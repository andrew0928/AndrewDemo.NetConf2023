using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace AndrewDemo.NetConf2023.AdminCLI;

internal sealed class AdminClient(HttpClient client, CliConfiguration configuration)
{
    public static HttpClient CreateHttpClient() => new(new HttpClientHandler
    {
        AllowAutoRedirect = false,
        UseCookies = false
    }) { Timeout = TimeSpan.FromSeconds(30) };

    public async Task<JsonObject> QueryAsync(CliOptions options, CancellationToken cancellation)
    {
        var observations = new JsonArray();
        var rows = new JsonArray();
        QueryPage? first = null;
        int? previousId = null;
        var page = options.AllPages || options.Command == "stats" ? 1 : options.Page;
        do
        {
            cancellation.ThrowIfCancellationRequested();
            using var request = new HttpRequestMessage(HttpMethod.Get, new Uri(configuration.BaseUrl, options.Query(page)));
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", configuration.AccessToken);
            using var response = await client.SendAsync(request, cancellation);
            var body = await response.Content.ReadAsStringAsync(cancellation);
            if (response.StatusCode != HttpStatusCode.OK)
                throw new CliException(5, $"HTTP {(int)response.StatusCode}：{SafeError(body)}");
            // 回應若意外包含憑證，拒絕輸出，不讓伺服器反射到證據檔。
            if (body.Contains(configuration.AccessToken, StringComparison.Ordinal)) throw ResponseSchema.Invalid();
            var current = ResponseSchema.Parse(body, options, page);
            first ??= current;
            if (!JsonNode.DeepEquals(first.Filters, current.Filters)) throw ResponseSchema.Invalid();
            foreach (var (key, value) in first.Observation)
                if (key is not ("queriedAt" or "page") && !JsonNode.DeepEquals(value, current.Observation[key])) throw ResponseSchema.Invalid();
            foreach (var row in current.Rows)
            {
                var id = row![options.Source == "checkout" ? "transactionId" : "id"]!.GetValue<int>();
                if (previousId.HasValue && id >= previousId.Value) throw ResponseSchema.Invalid();
                previousId = id;
                if (options.Command == "list") rows.Add(row.DeepClone());
            }
            observations.Add(current.Observation.DeepClone());
            var total = first.Observation["totalCount"]!.GetValue<int>();
            if (!options.AllPages || rows.Count >= total) break;
            page++;
        } while (true);
        return new JsonObject
        {
            ["schemaVersion"] = 1, ["command"] = options.Command, ["source"] = options.Source,
            ["filters"] = first.Filters.DeepClone(),
            ["pagination"] = new JsonObject
            {
                ["mode"] = options.Command == "stats" ? "stats" : options.AllPages ? "all" : "page",
                ["page"] = options.Command == "stats" || options.AllPages ? null : JsonValue.Create(options.Page),
                ["pageSize"] = options.Command == "stats" ? null : JsonValue.Create(options.PageSize)
            },
            ["observations"] = observations, ["rows"] = rows
        };
    }

    private static string SafeError(string body)
    {
        try
        {
            using var document = JsonDocument.Parse(body);
            if (document.RootElement.TryGetProperty("code", out var code) && code.ValueKind == JsonValueKind.String)
                return code.GetString() switch
                {
                    "admin_disabled" => "admin_disabled，服務端未啟用管理查核。",
                    "admin_unauthorized" => "admin_unauthorized，管理認證無效。",
                    "admin_query_invalid" => "admin_query_invalid，查詢條件無效。",
                    "admin_data_invalid" => "admin_data_invalid，服務端資料無法判讀。",
                    "admin_query_failed" => "admin_query_failed，服務端查詢失敗。",
                    _ => "管理查詢失敗。"
                };
        }
        catch (Exception error) when (error is JsonException or InvalidOperationException) { }
        return "管理查詢失敗。";
    }
}
