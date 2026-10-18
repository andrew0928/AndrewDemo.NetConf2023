using System.Globalization;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace AndrewDemo.NetConf2023.AdminCLI;

internal static class OutputRenderer
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
    private static readonly Dictionary<string, string> Labels = new()
    {
        ["id"] = "識別碼", ["name"] = "名稱", ["isTest"] = "用途", ["memberId"] = "會員 ID", ["memberName"] = "買方名稱",
        ["transactionId"] = "結帳 ID", ["cartId"] = "購物車 ID", ["paymentId"] = "付款 ID", ["state"] = "狀態",
        ["createdAt"] = "建立時間", ["closedAt"] = "結束時間", ["totalPrice"] = "保存金額", ["storage"] = "保存分區",
        ["fulfillmentStatus"] = "履約狀態", ["checkoutTransactionId"] = "關聯結帳 ID", ["totalCount"] = "符合條件總筆數",
        ["excludedUnknownTimeCount"] = "因未知時間排除筆數", ["totalAmount"] = "訂單總額", ["knownTotalAmount"] = "已知訂單金額",
        ["unknownAmountCount"] = "未知金額筆數"
    };
    public static string Render(JsonObject result, CliOptions options) => options.Format switch
    {
        "json" => result.ToJsonString(JsonOptions) + "\n",
        "csv" => Csv(result, options),
        _ => Txt(result, options)
    };

    private static string Txt(JsonObject result, CliOptions options)
    {
        var text = new StringBuilder();
        text.AppendLine($"命令：{options.Command}　來源：{options.Source}");
        text.AppendLine("有效條件：" + string.Join("，", result["filters"]!.AsObject().Select(p => $"{p.Key}={Visible(Value(p.Value, true))}")));
        foreach (var observation in result["observations"]!.AsArray())
        {
            text.AppendLine($"查詢時間：{Value(observation!["queriedAt"])}　請求頁次：{Value(observation["page"])}");
            foreach (var (key, value) in observation.AsObject().Where(p => p.Key is not ("page" or "queriedAt")))
                text.AppendLine($"  {Labels[key]} ({key})：{Value(value, true)}");
        }
        if (options.Source == "order") text.AppendLine("金額涵蓋全部符合條件的訂單，為保存金額，非當頁合計或實收；all 含正式與測試。");
        if (options.AllPages) text.AppendLine("跨請求讀取，非原子快照；漂移檢查不能偵測所有並行變更。");
        if (options.Command == "list")
        {
            var rows = result["rows"]!.AsArray();
            var total = result["observations"]![0]!["totalCount"]!.GetValue<int>();
            text.AppendLine($"頁次：{(options.AllPages ? "全部" : options.Page.ToString(CultureInfo.InvariantCulture))}　每頁上限：{options.PageSize}　已讀筆數：{rows.Count}／{total}");
            if (rows.Count == 0) text.AppendLine(total > 0 ? "本頁無資料。" : "符合條件的集合為空。");
            foreach (var row in rows)
            {
                text.AppendLine("---");
                foreach (var key in ResponseSchema.Columns(options.Source))
                {
                    var value = row![key];
                    var displayed = key == "isTest"
                        ? $"{(value!.GetValue<bool>() ? "test" : "normal")} (IsTest={Value(value)})"
                        : Visible(Value(value, true));
                    text.AppendLine($"{Labels[key]} ({key})：{displayed}");
                }
            }
        }
        return text.ToString();
    }

    private static string Csv(JsonObject result, CliOptions options)
    {
        var text = new StringBuilder("\uFEFF");
        if (options.Command == "stats")
        {
            string[] columns = ["source", "type", "memberId", "name", "state", "timeField", "from", "to", "queriedAt",
                "totalCount", "excludedUnknownTimeCount", "totalAmount", "knownTotalAmount", "unknownAmountCount"];
            text.AppendJoin(',', columns).Append("\r\n");
            var filters = result["filters"]!.AsObject();
            var observation = result["observations"]![0]!.AsObject();
            var values = columns.Select(key => key switch
            {
                "source" => Cell(JsonValue.Create(options.Source), true),
                "type" => Cell(filters["purpose"], true),
                _ => Cell(filters.ContainsKey(key) ? filters[key] : observation[key], IsText(key))
            });
            text.AppendJoin(',', values).Append("\r\n");
        }
        else
        {
            var columns = ResponseSchema.Columns(options.Source);
            text.AppendJoin(',', columns).Append("\r\n");
            foreach (var row in result["rows"]!.AsArray())
                text.AppendJoin(',', columns.Select(key => Cell(row![key], IsText(key)))).Append("\r\n");
        }
        return text.ToString();
    }

    private static bool IsText(string key) => key is "name" or "memberName" or "state" or "timeField" or "storage" or "fulfillmentStatus";
    private static string Cell(JsonNode? value, bool protect)
    {
        var text = Value(value);
        // 只處理文字欄位，避免改寫負數金額。前導空白也不應繞過公式保護。
        var first = text.TrimStart();
        if (protect && text.Length > 0 && (char.IsControl(text[0]) || first.Length > 0 && "=+-@".Contains(first[0]))) text = "'" + text;
        return text.IndexOfAny([',', '"', '\r', '\n']) >= 0 ? "\"" + text.Replace("\"", "\"\"") + "\"" : text;
    }

    internal static string Value(JsonNode? value, bool showNull = false)
    {
        if (value == null) return showNull ? "未知 (null)" : "";
        return value.GetValueKind() == JsonValueKind.String ? value.GetValue<string>() : value.ToJsonString();
    }
    private static string Visible(string value) => string.Concat(value.Select(c =>
        char.IsControl(c) || CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.Format ? $"\\u{(int)c:x4}" : c.ToString()));
}
