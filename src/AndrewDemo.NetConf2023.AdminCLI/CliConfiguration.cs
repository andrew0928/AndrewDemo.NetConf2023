using System.Text;
using System.Text.RegularExpressions;

namespace AndrewDemo.NetConf2023.AdminCLI;

internal sealed record CliConfiguration(Uri BaseUrl, string AccessToken)
{
    public static CliConfiguration Load(CliOptions options, Func<string, string?> environment)
    {
        var file = options.Get("env-file") ?? Path.Combine(Directory.GetCurrentDirectory(), ".env");
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        try
        {
            // 只略過不存在的預設檔案；其他讀取錯誤與明確指定路徑都必須回報。
            string[] lines;
            try { lines = File.ReadAllLines(file, new UTF8Encoding(false, true)); }
            catch (FileNotFoundException) when (options.Get("env-file") == null) { lines = []; }
            for (var i = 0; i < lines.Length; i++)
            {
                var line = lines[i].Trim();
                if (line.Length == 0 || line.StartsWith('#')) continue;
                var equal = line.IndexOf('=');
                var key = equal < 0 ? "" : line[..equal].Trim();
                if (!Regex.IsMatch(key, @"\A[A-Za-z_][A-Za-z0-9_]*\z") || values.ContainsKey(key))
                    throw new CliException(3, $"dotenv 第 {i + 1} 行格式無效。");
                var value = line[(equal + 1)..].Trim();
                if (value.StartsWith('\'') || value.StartsWith('"'))
                {
                    if (value.Length < 2 || value[^1] != value[0] || value[1..^1].Contains(value[0]))
                        throw new CliException(3, $"dotenv 第 {i + 1} 行引號無效。");
                    value = value[1..^1];
                }
                else if (value.Contains('\'') || value.Contains('"'))
                    throw new CliException(3, $"dotenv 第 {i + 1} 行引號無效。");
                values.Add(key, value);
            }
        }
        catch (Exception e) when (e is IOException or UnauthorizedAccessException or ArgumentException or DecoderFallbackException)
        { throw new CliException(3, "無法讀取 dotenv 設定檔。"); }
        string? Get(string name) => environment(name) ?? values.GetValueOrDefault(name);
        var token = Get("SHOP_ADMIN_ACCESS_TOKEN");
        if (token == null || !Regex.IsMatch(token, @"\Aadmin_[a-z0-9]+\z"))
            throw new CliException(3, "SHOP_ADMIN_ACCESS_TOKEN 未設定或格式無效，需為 admin_ 加小寫英數字。");
        var baseUrl = Get("SHOP_ADMIN_BASE_URL") ?? "http://localhost:5128";
        var literalHost = Regex.Match(baseUrl, @"\Ahttps?://(\[[^\]]+\]|[^/:?#]+)(?::[0-9]+)?/?\z", RegexOptions.IgnoreCase).Groups[1].Value;
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri) || uri.Scheme is not ("http" or "https") ||
            uri.UserInfo.Length != 0 || uri.Query.Length != 0 || uri.Fragment.Length != 0 || uri.AbsolutePath != "/" ||
            baseUrl != baseUrl.Trim() || baseUrl.Contains('\\') || literalHost.Length == 0 ||
            (uri.Scheme == "http" && literalHost.ToLowerInvariant() is not ("localhost" or "127.0.0.1" or "[::1]")))
            throw new CliException(3, "SHOP_ADMIN_BASE_URL 無效；僅允許根網址，非本機端點須使用 HTTPS。");
        return new CliConfiguration(uri, token);
    }
}
