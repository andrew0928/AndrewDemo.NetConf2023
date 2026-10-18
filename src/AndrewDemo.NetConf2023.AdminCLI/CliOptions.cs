using System.Globalization;
using System.Text.RegularExpressions;

namespace AndrewDemo.NetConf2023.AdminCLI;

internal sealed class CliException(int exitCode, string message) : Exception(message)
{
    public int ExitCode { get; } = exitCode;
}

internal sealed record CliOptions(string Command, string Source, Dictionary<string, string> Values)
{
    public string Type => Get("type") ?? "normal";
    public string Format => Get("format") ?? "txt";
    public bool AllPages => Values.ContainsKey("all-pages");
    public int Page => int.Parse(Get("page") ?? "1", CultureInfo.InvariantCulture);
    public int PageSize => Command == "stats" ? 1 : int.Parse(Get("page-size") ?? "50", CultureInfo.InvariantCulture);
    public string? Get(string key) => Values.GetValueOrDefault(key);
    public string Resource => Source switch { "member" => "members", "checkout" => "checkouts", _ => "orders" };

    public static CliOptions Parse(string[] args)
    {
        void Invalid() => throw new CliException(2, "命令或選項無效；請執行 shop-admin --help。");
        if (args.Length == 0 || args[0] is not ("stats" or "list")) Invalid();
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        string[] allowed = ["source", "type", "from", "to", "member-id", "name", "state", "time-field",
            "page", "page-size", "all-pages", "format", "output", "env-file"];
        for (var i = 1; i < args.Length; i++)
        {
            if (!args[i].StartsWith("--", StringComparison.Ordinal)) Invalid();
            var key = args[i][2..];
            if (!allowed.Contains(key) || values.ContainsKey(key)) Invalid();
            if (key == "all-pages") { values.Add(key, "true"); continue; }
            if (++i == args.Length || args[i].StartsWith("--", StringComparison.Ordinal)) Invalid();
            values.Add(key, args[i]);
        }
        var source = values.GetValueOrDefault("source", "");
        if (source is not ("member" or "checkout" or "order")) Invalid();
        var result = new CliOptions(args[0], source, values);
        if (result.Type is not ("all" or "normal" or "test") || result.Format is not ("txt" or "csv" or "json")) Invalid();
        foreach (var key in new[] { "member-id", "page", "page-size" })
            if (values.TryGetValue(key, out var value) && (!int.TryParse(value, NumberStyles.None,
                CultureInfo.InvariantCulture, out var number) || number < 1 || (key == "page-size" && number > 100))) Invalid();
        if (result.Command == "stats" && values.Keys.Any(k => k is "page" or "page-size" or "all-pages")) Invalid();
        if (result.AllPages && values.ContainsKey("page")) Invalid();
        if (source == "member" && values.Keys.Any(k => k is "from" or "to")) Invalid();
        if (source != "member" && values.ContainsKey("name")) Invalid();
        if (source != "checkout" && values.Keys.Any(k => k is "state" or "time-field")) Invalid();
        if (result.Get("state") is { } state && state is not ("all" or "unknown" or "created" or "completed" or "cancelled")) Invalid();
        if (result.Get("time-field") is { } field && field is not ("createdAt" or "closedAt")) Invalid();
        foreach (var key in new[] { "from", "to" })
            if (values.TryGetValue(key, out var date))
            {
                if (!TryTimestamp(date, out var parsed)) Invalid();
                values[key] = parsed.UtcDateTime.ToString("O", CultureInfo.InvariantCulture);
            }
        if (result.Get("from") is { } from && result.Get("to") is { } to &&
            DateTimeOffset.Parse(from, CultureInfo.InvariantCulture) >= DateTimeOffset.Parse(to, CultureInfo.InvariantCulture)) Invalid();
        foreach (var key in new[] { "output", "env-file" })
            if (values.TryGetValue(key, out var path) && string.IsNullOrWhiteSpace(path)) Invalid();
        if (result.Get("output") is { } output)
        {
            var extension = Path.GetExtension(output).ToLowerInvariant();
            if (extension is ".txt" or ".csv" or ".json" && extension != "." + result.Format) Invalid();
        }
        return result;
    }

    internal static bool TryTimestamp(string value, out DateTimeOffset parsed)
    {
        parsed = default;
        return Regex.IsMatch(value, @"\A\d{4}-\d{2}-\d{2}T\d{2}:\d{2}:\d{2}(?:\.\d{1,7})?(?:Z|[+-]\d{2}:\d{2})\z") &&
            DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed);
    }

    public Dictionary<string, string?> Filters() => Source switch
    {
        "member" => new() { ["purpose"] = Type, ["memberId"] = Get("member-id"), ["name"] = Get("name") },
        "checkout" => new() { ["purpose"] = Type, ["memberId"] = Get("member-id"), ["from"] = Get("from"), ["to"] = Get("to"),
            ["state"] = Get("state") ?? "all", ["timeField"] = Get("time-field") ?? "createdAt" },
        _ => new() { ["purpose"] = Type, ["memberId"] = Get("member-id"), ["from"] = Get("from"), ["to"] = Get("to") }
    };

    public string Query(int page) => $"api/admin/{Resource}?" + string.Join("&", Filters()
        .Where(p => p.Value != null).Select(p => $"{p.Key}={Uri.EscapeDataString(p.Value!)}")
        .Append($"page={page.ToString(CultureInfo.InvariantCulture)}").Append($"pageSize={PageSize.ToString(CultureInfo.InvariantCulture)}"));
}
