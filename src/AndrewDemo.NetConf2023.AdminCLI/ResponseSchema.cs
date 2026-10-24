using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;

namespace AndrewDemo.NetConf2023.AdminCLI;

internal sealed record QueryPage(JsonObject Filters, JsonObject Observation, JsonArray Rows);

internal static class ResponseSchema
{
    public static string[] Columns(string source) => source switch
    {
        "member" => ["id", "name", "isTest"],
        "checkout" => ["transactionId", "memberId", "cartId", "paymentId", "isTest", "state", "createdAt", "closedAt", "totalPrice", "storage"],
        _ => ["id", "memberId", "memberName", "isTest", "createdAt", "totalPrice", "fulfillmentStatus", "checkoutTransactionId"]
    };

    public static CliException Invalid() => new(6, "API 回應不符合管理查核 schema，或查詢期間資料改變；未發布結果。");

    public static QueryPage Parse(string json, CliOptions options, int page)
    {
        try
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            RejectDuplicates(root);
            var filters = root.GetProperty("filters");
            if (root.GetProperty("page").GetInt32() != page || root.GetProperty("pageSize").GetInt32() != options.PageSize ||
                filters.GetProperty("page").GetInt32() != page || filters.GetProperty("pageSize").GetInt32() != options.PageSize) throw Invalid();
            var effective = new JsonObject();
            foreach (var (key, expected) in options.Filters())
            {
                var value = filters.GetProperty(key);
                if (expected == null) { if (value.ValueKind != JsonValueKind.Null) throw Invalid(); }
                else if (key == "memberId") { if (value.GetInt32() != int.Parse(expected, CultureInfo.InvariantCulture)) throw Invalid(); }
                else if (key is "from" or "to")
                {
                    Utc(value);
                    if (DateTimeOffset.Parse(value.GetString()!, CultureInfo.InvariantCulture) != DateTimeOffset.Parse(expected, CultureInfo.InvariantCulture)) throw Invalid();
                }
                else if (value.GetString() != expected) throw Invalid();
                effective[key] = JsonNode.Parse(value.GetRawText());
            }
            var observation = new JsonObject { ["page"] = page };
            Utc(root.GetProperty("queriedAt"));
            observation["queriedAt"] = root.GetProperty("queriedAt").GetString();
            foreach (var key in new[] { "totalCount", "excludedUnknownTimeCount" })
            {
                var number = root.GetProperty(key).GetInt32();
                if (number < 0) throw Invalid();
                observation[key] = number;
            }
            if (options.Source == "order")
            {
                var total = root.GetProperty("totalAmount");
                var known = Money(root.GetProperty("knownTotalAmount"));
                var unknown = root.GetProperty("unknownAmountCount").GetInt32();
                if (unknown < 0 || unknown > observation["totalCount"]!.GetValue<int>() ||
                    (unknown == 0 ? total.ValueKind == JsonValueKind.Null || Money(total) != known : total.ValueKind != JsonValueKind.Null)) throw Invalid();
                observation["totalAmount"] = total.ValueKind == JsonValueKind.Null ? null : JsonValue.Create(Money(total));
                observation["knownTotalAmount"] = known;
                observation["unknownAmountCount"] = unknown;
            }
            var rows = new JsonArray();
            foreach (var item in root.GetProperty("items").EnumerateArray())
            {
                var row = new JsonObject();
                foreach (var key in Columns(options.Source))
                {
                    var value = item.GetProperty(key);
                    var nullable = key is "memberId" or "cartId" or "paymentId" or "checkoutTransactionId" or "name" or "memberName" or "createdAt" or "closedAt" or "totalPrice";
                    if (value.ValueKind == JsonValueKind.Null)
                    {
                        if (!nullable) throw Invalid();
                        row[key] = null;
                        continue;
                    }
                    switch (key)
                    {
                        case "id": case "transactionId": case "memberId": case "cartId": case "paymentId": case "checkoutTransactionId":
                            row[key] = value.GetInt32(); break;
                        case "isTest": row[key] = value.GetBoolean(); break;
                        case "totalPrice": row[key] = Money(value); break;
                        case "createdAt": case "closedAt": Utc(value); row[key] = value.GetString(); break;
                        default:
                            var text = value.GetString()!;
                            if (key == "state" && text is not ("unknown" or "created" or "completed" or "cancelled") ||
                                key == "storage" && text is not ("active" or "archived") ||
                                key == "fulfillmentStatus" && text is not ("pending" or "succeeded" or "failed")) throw Invalid();
                            row[key] = text; break;
                    }
                }
                rows.Add(row);
            }
            var count = observation["totalCount"]!.GetValue<int>();
            var expectedRows = (int)Math.Min(options.PageSize, Math.Max(0L, count - ((long)page - 1) * options.PageSize));
            if (rows.Count != expectedRows) throw Invalid();
            return new(effective, observation, rows);
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException or KeyNotFoundException or FormatException or OverflowException)
        { throw Invalid(); }
    }

    private static void Utc(JsonElement value)
    {
        if (value.ValueKind != JsonValueKind.String || !CliOptions.TryTimestamp(value.GetString()!, out var time) || time.Offset != TimeSpan.Zero) throw Invalid();
    }

    private static decimal Money(JsonElement value)
    {
        var number = value.GetDecimal();
        // GetDecimal 對超長小數可能四捨五入；查核不得悄悄損失 API 金額精度。
        static string Normalize(string text)
        {
            var match = Regex.Match(text, @"\A(-?)(\d+)(?:\.(\d+))?(?:[eE]([+-]?\d+))?\z");
            if (!match.Success) throw Invalid();
            var digits = (match.Groups[2].Value + match.Groups[3].Value).TrimStart('0');
            if (digits.Length == 0) return "0";
            var significant = digits.TrimEnd('0');
            var exponent = match.Groups[4].Success ? int.Parse(match.Groups[4].Value, CultureInfo.InvariantCulture) : 0;
            var scale = checked(exponent - match.Groups[3].Length + digits.Length - significant.Length);
            return match.Groups[1].Value + significant + "e" + scale.ToString(CultureInfo.InvariantCulture);
        }
        if (Normalize(value.GetRawText()) != Normalize(number.ToString(CultureInfo.InvariantCulture))) throw Invalid();
        return number;
    }

    private static void RejectDuplicates(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var property in element.EnumerateObject())
            {
                if (!names.Add(property.Name)) throw Invalid();
                RejectDuplicates(property.Value);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
            foreach (var item in element.EnumerateArray()) RejectDuplicates(item);
    }
}
