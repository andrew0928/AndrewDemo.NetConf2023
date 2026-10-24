using System.Globalization;
using System.Text.RegularExpressions;
using AndrewDemo.NetConf2023.Core.AdminQueries;

namespace AndrewDemo.NetConf2023.API.Admin;

internal sealed class AdminQueryInvalidException : ArgumentException;

/// <summary>嚴格解析已定案條件，拒絕拼錯、重複及不支援的欄位。</summary>
internal static partial class AdminQueryParameters
{
    public static AdminQueryOptions Parse(IQueryCollection query, string resource)
    {
        var allowed = new HashSet<string>(["purpose", "page", "pageSize", "memberId"], StringComparer.Ordinal);
        if (resource == "members") allowed.Add("name");
        else { allowed.Add("from"); allowed.Add("to"); }
        if (resource == "checkouts") { allowed.Add("state"); allowed.Add("timeField"); }
        if (query.Any(x => !allowed.Contains(x.Key) || x.Value.Count != 1)) throw new AdminQueryInvalidException();

        string? Value(string key) => query.TryGetValue(key, out var values) ? values[0] : null;
        string Choice(string key, string fallback, params string[] valid)
        {
            var value = Value(key) ?? fallback;
            return valid.Contains(value, StringComparer.Ordinal) ? value : throw new AdminQueryInvalidException();
        }
        int Number(string key, int fallback, int max = int.MaxValue)
        {
            var text = Value(key);
            if (text == null) return fallback;
            if (!int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out var value) || value < 1 || value > max)
                throw new AdminQueryInvalidException();
            return value;
        }
        DateTime? Date(string key)
        {
            var value = Value(key);
            if (value == null) return null;
            if (!Timestamp().IsMatch(value) || !DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out var parsed)) throw new AdminQueryInvalidException();
            return parsed.UtcDateTime;
        }
        var from = Date("from");
        var to = Date("to");
        if (from != null && to != null && from >= to) throw new AdminQueryInvalidException();
        return new AdminQueryOptions
        {
            Purpose = Choice("purpose", "normal", "normal", "test", "all"),
            Page = Number("page", 1), PageSize = Number("pageSize", 50, 100),
            MemberId = Value("memberId") == null ? null : Number("memberId", 1), Name = Value("name"),
            From = from, To = to,
            State = Choice("state", "all", "all", "unknown", "created", "completed", "cancelled"),
            TimeField = Choice("timeField", "createdAt", "createdAt", "closedAt")
        };
    }

    public static object Filters(AdminQueryOptions options, string resource)
    {
        var filters = new Dictionary<string, object?>
        {
            ["purpose"] = options.Purpose, ["page"] = options.Page, ["pageSize"] = options.PageSize,
            ["memberId"] = options.MemberId
        };
        if (resource == "members") filters["name"] = options.Name;
        else { filters["from"] = options.From; filters["to"] = options.To; }
        if (resource == "checkouts") { filters["state"] = options.State; filters["timeField"] = options.TimeField; }
        return filters;
    }

    [GeneratedRegex(@"\A\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d{1,7})?(?:Z|[+-]\d{2}:\d{2})\z", RegexOptions.CultureInvariant)]
    private static partial Regex Timestamp();
}
