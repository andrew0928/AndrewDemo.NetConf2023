using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using AndrewDemo.NetConf2023.AdminCLI;

namespace AndrewDemo.NetConf2023.AdminCLI.Tests;

public sealed class CliTests : IDisposable
{
    private readonly string directory = Path.Combine(Path.GetTempPath(), "admin-cli-tests-" + Guid.NewGuid().ToString("N"));
    private const string Token = "admin_fixturesecret";
    public CliTests() { Directory.CreateDirectory(directory); File.WriteAllText(Path.Combine(directory, ".env"), ""); }
    public void Dispose() => Directory.Delete(directory, true);
    private string FilePath(string name) => Path.Combine(directory, name);

    private async Task<(int Code, string Out, string Error, int Calls)> Run(string[] args,
        Func<HttpRequestMessage, int, HttpResponseMessage>? respond = null, Func<string, string?>? env = null,
        CancellationToken cancellation = default, TextWriter? output = null)
    {
        var handler = new Handler(respond ?? ((_, _) => throw new Exception("不應呼叫 HTTP")));
        using var client = new HttpClient(handler);
        var stdout = output ?? new StringWriter();
        var stderr = new StringWriter();
        if (args.Length > 1 && args[0] is "stats" or "list" && args[1] != "--help" && !args.Contains("--env-file"))
            args = [.. args, "--env-file", FilePath(".env")];
        var code = await CliApplication.RunAsync(args, env ?? (key => key == "SHOP_ADMIN_ACCESS_TOKEN" ? Token : null), stdout, stderr, cancellation, client);
        return (code, stdout.ToString()!, stderr.ToString(), handler.Calls);
    }

    private sealed class Handler(Func<HttpRequestMessage, int, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public int Calls { get; private set; }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(respond(request, ++Calls));
    }
    private static HttpResponseMessage Response(JsonObject body) => new(HttpStatusCode.OK) { Content = new StringContent(body.ToJsonString()) };
    private static Dictionary<string, string> Query(HttpRequestMessage request) => request.RequestUri!.Query[1..].Split('&')
        .Select(x => x.Split('=', 2)).ToDictionary(x => x[0], x => Uri.UnescapeDataString(x[1]));

    private static JsonObject Fixture(HttpRequestMessage request, int count = 3)
    {
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal("Bearer", request.Headers.Authorization!.Scheme);
        Assert.Equal(Token, request.Headers.Authorization.Parameter);
        Assert.Null(request.Content);
        var query = Query(request);
        var source = request.RequestUri!.AbsolutePath.Split('/')[^1];
        Assert.Contains(source, new[] { "members", "orders", "checkouts" });
        var page = int.Parse(query["page"]);
        var size = int.Parse(query["pageSize"]);
        var filters = new JsonObject { ["purpose"] = query["purpose"], ["memberId"] = query.TryGetValue("memberId", out var member) ? int.Parse(member) : null,
            ["page"] = page, ["pageSize"] = size };
        if (source == "members") filters["name"] = query.GetValueOrDefault("name");
        else { filters["from"] = query.GetValueOrDefault("from"); filters["to"] = query.GetValueOrDefault("to"); }
        if (source == "checkouts") { filters["state"] = query.GetValueOrDefault("state", "all"); filters["timeField"] = query.GetValueOrDefault("timeField", "createdAt"); }
        var rows = new JsonArray();
        foreach (var id in Enumerable.Range(1, count).Reverse().Skip((page - 1) * size).Take(size))
        {
            var row = new JsonObject { [source == "checkouts" ? "transactionId" : "id"] = id, ["isTest"] = query["purpose"] == "test" };
            if (source == "members") row["name"] = "測試會員";
            else
            {
                row["memberId"] = 4; row["createdAt"] = "2026-10-18T00:00:00Z"; row["totalPrice"] = 100.1234567890123456789m;
                if (source == "orders") { row["memberName"] = "一般買方"; row["fulfillmentStatus"] = "failed"; row["checkoutTransactionId"] = null; }
                else { row["cartId"] = 1; row["paymentId"] = 900001; row["state"] = "completed"; row["closedAt"] = null; row["storage"] = "archived"; }
            }
            rows.Add(row);
        }
        var body = new JsonObject { ["queriedAt"] = "2026-10-18T00:00:00Z", ["filters"] = filters, ["page"] = page, ["pageSize"] = size,
            ["totalCount"] = count, ["excludedUnknownTimeCount"] = 0, ["items"] = rows };
        if (source == "orders") { body["totalAmount"] = 300.1234567890123456789m; body["knownTotalAmount"] = 300.1234567890123456789m; body["unknownAmountCount"] = 0; }
        return body;
    }

    [Theory]
    [InlineData("member", "stats")][InlineData("checkout", "stats")][InlineData("order", "stats")]
    [InlineData("member", "list")][InlineData("checkout", "list")][InlineData("order", "list")]
    public async Task SourcesCommandsAndFormatsUseOnlyGetAndSameProjection(string source, string command)
    {
        foreach (var format in new[] { "txt", "csv", "json" })
        {
            var result = await Run([command, "--source", source, "--format", format], (request, _) =>
            {
                Assert.Equal("normal", Query(request)["purpose"]);
                Assert.Equal(command == "stats" ? "1" : "50", Query(request)["pageSize"]);
                return Response(Fixture(request));
            });
            Assert.Equal(0, result.Code); Assert.Equal(1, result.Calls); Assert.Empty(result.Error);
            Assert.DoesNotContain(Token, result.Out);
            if (format == "json")
            {
                var json = JsonNode.Parse(result.Out)!;
                Assert.Equal(1, json["schemaVersion"]!.GetValue<int>());
                Assert.Equal(command == "stats" ? 0 : 3, json["rows"]!.AsArray().Count);
                Assert.False(json["filters"]!.AsObject().ContainsKey("page"));
                Assert.Equal(source == "order", json["observations"]![0]!.AsObject().ContainsKey("totalAmount"));
            }
            if (format == "csv") { Assert.StartsWith("\uFEFF", result.Out); Assert.Contains("\r\n", result.Out); }
            if (format == "txt") Assert.Contains("符合條件總筆數", result.Out);
        }
    }

    public static IEnumerable<object[]> InvalidArguments => new[]
    {
        new[] { "list" }, new[] { "list", "--source", "member", "--from", "2026-10-18T00:00:00Z" },
        new[] { "list", "--source", "member", "--to", "2026-10-18T00:00:00Z" },
        new[] { "list", "--source", "order", "--state", "all" }, new[] { "list", "--source", "checkout", "--name", "test" },
        new[] { "list", "--source", "order", "--time-field", "createdAt" },
        new[] { "list", "--source", "order", "--type", "NORMAL" }, new[] { "list", "--source", "order", "--page", "0" },
        new[] { "list", "--source", "order", "--page-size", "101" }, new[] { "list", "--source", "order", "--page", "999999999999" },
        new[] { "list", "--source", "order", "--all-pages", "--page", "1" }, new[] { "stats", "--source", "order", "--all-pages" },
        new[] { "stats", "--source", "order", "--page-size", "1" }, new[] { "list", "--source", "member", "--source", "order" },
        new[] { "list", "--source=order" }, new[] { "list", "--source", "order", "--token", Token },
        new[] { "list", "--source", "order", "--from", "2026-10-18T00:00:00" },
        new[] { "list", "--source", "order", "--from", "2026-10-18T00:00:00Z", "--to", "2026-10-18T00:00:00Z" },
        new[] { "list", "--source", "order", "--from", "2026-02-30T00:00:00Z" },
        new[] { "list", "--source", "order", "--format", "csv", "--output", "x.json" },
        new[] { "--help", "--env-file", "secret" }, new[] { "--skill", "configure", "--output", "x" }
    }.Select(x => new object[] { x });
    [Theory, MemberData(nameof(InvalidArguments))]
    public async Task InvalidOptionsFailBeforeHttp(string[] args)
    { var result = await Run(args); Assert.Equal(2, result.Code); Assert.Equal(0, result.Calls); Assert.Empty(result.Out); Assert.DoesNotContain(Token, result.Error); }

    [Theory]
    [InlineData("900001")][InlineData("中文 + &= \" $(touch /tmp/nope)")]
    public async Task NamesRemainEncodedSingleValues(string name)
    {
        var result = await Run(["list", "--source", "member", "--type", "all", "--name", name], (request, _) =>
        { Assert.Equal(name, Query(request)["name"]); Assert.Equal(4, Query(request).Count); return Response(Fixture(request)); });
        Assert.Equal(0, result.Code);
    }
    [Fact]
    public async Task OffsetNormalizesToUtcAndSingleSidedRangeWorks()
    {
        var result = await Run(["stats", "--source", "order", "--from", "2026-10-18T08:00:00+08:00"], (request, _) =>
        { Assert.Equal("2026-10-18T00:00:00.0000000Z", Query(request)["from"]); return Response(Fixture(request)); });
        Assert.Equal(0, result.Code);
    }

    [Theory]
    [InlineData("http://example.com")][InlineData("http://localhost.evil")][InlineData("https://u:p@example.com")]
    [InlineData("https://example.com/path")][InlineData("https://example.com/?x=1")][InlineData("https://example.com/#x")]
    [InlineData("")][InlineData("file:///tmp")]
    public async Task InvalidUrlNeverSendsToken(string url)
    {
        var result = await Run(["stats", "--source", "order"], env: key => key == "SHOP_ADMIN_BASE_URL" ? url : Token);
        Assert.Equal(3, result.Code); Assert.Equal(0, result.Calls); Assert.DoesNotContain(Token, result.Error);
    }
    [Theory]
    [InlineData("http://localhost:5128/")][InlineData("http://127.0.0.1:5128")][InlineData("http://[::1]:5128")][InlineData("https://example.com")]
    public async Task AllowedUrlsUseRoot(string url)
    {
        var result = await Run(["stats", "--source", "order"], (request, _) => Response(Fixture(request)), key => key == "SHOP_ADMIN_BASE_URL" ? url : Token);
        Assert.Equal(0, result.Code);
    }
    [Theory]
    [InlineData(null)][InlineData("")][InlineData("admin_A")][InlineData("admin_a-b")][InlineData(" admin_a")][InlineData("admin_a\n")]
    public async Task InvalidTokensArePrivate(string? token)
    {
        var result = await Run(["list", "--source", "member"], env: _ => token);
        Assert.Equal(3, result.Code); Assert.Equal(0, result.Calls);
    }

    [Fact]
    public async Task DotenvBomQuotesAndEnvironmentPrecedence()
    {
        File.WriteAllText(FilePath(".env"), "# 設定\n\nSHOP_ADMIN_ACCESS_TOKEN = 'admin_file'\nSHOP_ADMIN_BASE_URL=\"https://example.com\"\nIGNORED=$(echo nope)\n", new UTF8Encoding(true));
        var result = await Run(["stats", "--source", "member"], (request, _) =>
        { Assert.Equal("example.com", request.RequestUri!.Host); return Response(Fixture(request)); });
        Assert.Equal(0, result.Code);
        var empty = await Run(["stats", "--source", "member"], env: _ => "");
        Assert.Equal(3, empty.Code); Assert.Equal(0, empty.Calls);
    }
    [Theory]
    [InlineData("SHOP_ADMIN_ACCESS_TOKEN=admin_secret\nSHOP_ADMIN_ACCESS_TOKEN=admin_other")]
    [InlineData("export SHOP_ADMIN_ACCESS_TOKEN=admin_secret")]
    [InlineData("SHOP_ADMIN_ACCESS_TOKEN=\"admin_secret")]
    [InlineData("SHOP_ADMIN_ACCESS_TOKEN='admin_secret\nrest'")]
    [InlineData("bad line admin_secret")]
    public async Task InvalidDotenvNeverEchoesInput(string contents)
    {
        File.WriteAllText(FilePath(".env"), contents);
        var result = await Run(["list", "--source", "member"]);
        Assert.Equal(3, result.Code); Assert.DoesNotContain("admin_secret", result.Error); Assert.Empty(result.Out); Assert.Equal(0, result.Calls);
    }
    [Fact]
    public async Task MissingExplicitDotenvFailsEvenWithEnvironment()
    {
        var result = await Run(["stats", "--source", "order", "--env-file", FilePath("missing")]);
        Assert.Equal(3, result.Code); Assert.Equal(0, result.Calls);
    }

    [Theory]
    [InlineData(0)][InlineData(1)][InlineData(5)]
    public async Task AllPagesPreservesObservationsAndDoesNotSumAmounts(int count)
    {
        var result = await Run(["list", "--source", "order", "--all-pages", "--page-size", "2", "--format", "json"], (request, _) => Response(Fixture(request, count)));
        Assert.Equal(0, result.Code);
        var json = JsonNode.Parse(result.Out)!;
        Assert.Equal(count, json["rows"]!.AsArray().Count);
        Assert.Equal(Math.Max(1, (count + 1) / 2), result.Calls);
        Assert.Equal(result.Calls, json["observations"]!.AsArray().Count);
        Assert.Equal("all", json["pagination"]!["mode"]!.GetValue<string>());
        Assert.Null(json["pagination"]!["page"]);
        Assert.Equal(300.1234567890123456789m, json["observations"]![0]!["totalAmount"]!.GetValue<decimal>());
    }
    [Theory]
    [InlineData("count")][InlineData("money")][InlineData("duplicate")][InlineData("empty")][InlineData("filters")][InlineData("order")]
    public async Task DetectableDriftPublishesNothing(string change)
    {
        var result = await Run(["list", "--source", "order", "--all-pages", "--page-size", "1", "--output", FilePath("result.txt")], (request, call) =>
        {
            var fixture = Fixture(request);
            if (call == 2) switch (change)
            {
                case "count": fixture["totalCount"] = 4; break;
                case "money": fixture["totalAmount"] = 400; fixture["knownTotalAmount"] = 400; break;
                case "duplicate": fixture["items"]![0]!["id"] = 3; break;
                case "order": fixture["items"]![0]!["id"] = 4; break;
                case "empty": fixture["items"] = new JsonArray(); break;
                case "filters": fixture["filters"]!["purpose"] = "test"; break;
            }
            return Response(fixture);
        });
        Assert.Equal(6, result.Code); Assert.Equal(2, result.Calls); Assert.Empty(result.Out); Assert.False(File.Exists(FilePath("result.txt")));
        Assert.Empty(Directory.GetFiles(directory, "*.tmp"));
    }

    [Theory]
    [InlineData("missing")][InlineData("bool")][InlineData("enum")][InlineData("time")][InlineData("decimal")][InlineData("null")]
    public async Task InvalidSchemaFailsClosed(string change)
    {
        var result = await Run(["list", "--source", "order"], (request, _) =>
        {
            var fixture = Fixture(request); var row = fixture["items"]![0]!.AsObject();
            switch (change)
            {
                case "missing": row.Remove("isTest"); break;
                case "bool": row["isTest"] = "false"; break;
                case "enum": row["fulfillmentStatus"] = "bogus"; break;
                case "time": row["createdAt"] = "2026-10-18"; break;
                case "decimal": row["totalPrice"] = "100"; break;
                case "null": row["id"] = null; break;
            }
            return Response(fixture);
        });
        Assert.Equal(6, result.Code); Assert.Empty(result.Out);
    }
    [Theory]
    [InlineData("")][InlineData("[]")][InlineData("null")][InlineData("{bad}")][InlineData("{\"queriedAt\":null}")]
    public async Task InvalidJsonHasSafeFailure(string body)
    {
        var result = await Run(["stats", "--source", "member"], (_, _) => new(HttpStatusCode.OK) { Content = new StringContent(body) });
        Assert.Equal(6, result.Code); Assert.Empty(result.Out);
    }
    [Fact]
    public async Task DuplicateKeysRejectedAndUnknownFieldsAccepted()
    {
        var duplicate = await Run(["stats", "--source", "member"], (request, _) =>
            new(HttpStatusCode.OK) { Content = new StringContent(Fixture(request).ToJsonString().Replace("\"totalCount\":3", "\"totalCount\":3,\"totalCount\":3")) });
        Assert.Equal(6, duplicate.Code);
        var extra = await Run(["stats", "--source", "member"], (request, _) => { var f = Fixture(request); f["extra"] = true; return Response(f); });
        Assert.Equal(0, extra.Code);
    }

    [Theory]
    [InlineData(302)][InlineData(400)][InlineData(401)][InlineData(404)][InlineData(500)]
    public async Task HttpFailuresDoNotRetryOrEchoBody(int status)
    {
        var result = await Run(["stats", "--source", "order"], (_, _) => new((HttpStatusCode)status) { Content = new StringContent(Token) });
        Assert.Equal(5, result.Code); Assert.Equal(1, result.Calls); Assert.Empty(result.Out); Assert.Contains(status.ToString(), result.Error); Assert.DoesNotContain(Token, result.Error);
    }
    [Fact]
    public async Task DisabledIsDistinctFromUnknownRoute()
    {
        var result = await Run(["stats", "--source", "order"], (_, _) => new(HttpStatusCode.NotFound) { Content = new StringContent("{\"code\":\"admin_disabled\"}") });
        Assert.Contains("未啟用", result.Error);
    }
    [Fact]
    public async Task TransportTimeoutAndUserCancellationHaveDistinctCodes()
    {
        Assert.Equal(4, (await Run(["stats", "--source", "order"], (_, _) => throw new HttpRequestException(Token))).Code);
        Assert.Equal(4, (await Run(["stats", "--source", "order"], (_, _) => throw new TaskCanceledException(Token))).Code);
        using var cancellation = new CancellationTokenSource(); cancellation.Cancel();
        var result = await Run(["stats", "--source", "order"], cancellation: cancellation.Token);
        Assert.Equal(130, result.Code); Assert.Equal(0, result.Calls);
    }

    [Fact]
    public async Task UnknownAmountsAndOwnPurposeSurviveProjection()
    {
        var result = await Run(["list", "--source", "order", "--type", "all", "--format", "json"], (request, _) =>
        {
            var f = Fixture(request); f["totalAmount"] = null; f["unknownAmountCount"] = 1;
            var row = f["items"]![0]!; row["isTest"] = true; row["totalPrice"] = null; row["createdAt"] = null;
            f["items"]![1]!["totalPrice"] = 0;
            return Response(f);
        });
        Assert.Equal(0, result.Code);
        var json = JsonNode.Parse(result.Out)!;
        Assert.Null(json["observations"]![0]!["totalAmount"]);
        Assert.True(json["rows"]![0]!["isTest"]!.GetValue<bool>());
        Assert.Null(json["rows"]![0]!["totalPrice"]);
        Assert.Equal(0, json["rows"]![1]!["totalPrice"]!.GetValue<decimal>());
    }
    [Fact]
    public async Task TxtShowsEmptyPageAndEscapesControlCharacters()
    {
        var empty = await Run(["list", "--source", "member", "--page", "9"], (request, _) => Response(Fixture(request)));
        Assert.Equal(0, empty.Code); Assert.Contains("本頁無資料", empty.Out); Assert.Contains("3", empty.Out);
        var longName = new string('長', 300) + "\u001b[31m\n";
        var result = await Run(["list", "--source", "member"], (request, _) => { var f = Fixture(request); f["items"]![0]!["name"] = longName; return Response(f); });
        Assert.Contains(new string('長', 300), result.Out); Assert.Contains("\\u001b", result.Out); Assert.DoesNotContain('\u001b', result.Out);
    }
    [Fact]
    public async Task CsvQuotesProtectsTextAndKeepsNegativeDecimal()
    {
        var result = await Run(["list", "--source", "order", "--format", "csv"], (request, _) =>
        { var f = Fixture(request, 1); f["items"]![0]!["memberName"] = "=SUM(1,2)\"\n中文"; f["items"]![0]!["totalPrice"] = -10.1234567890123456789m; return Response(f); });
        Assert.Equal(0, result.Code); Assert.StartsWith("\uFEFFid,memberId,memberName,isTest,createdAt,totalPrice,fulfillmentStatus,checkoutTransactionId\r\n", result.Out);
        Assert.Contains("\"'=SUM(1,2)\"\"\n中文\"", result.Out); Assert.Contains(",-10.1234567890123456789,", result.Out);
    }

    [Fact]
    public async Task OutputIsAtomicPrivateAndNeverOverwrites()
    {
        var args = new[] { "list", "--source", "order", "--format", "json", "--output", FilePath("orders.json") };
        var result = await Run(args, (request, _) => Response(Fixture(request)));
        Assert.Equal(0, result.Code); Assert.Empty(result.Out);
        var original = File.ReadAllText(FilePath("orders.json"));
        Assert.NotNull(JsonNode.Parse(original));
        if (!OperatingSystem.IsWindows()) Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite, File.GetUnixFileMode(FilePath("orders.json")));
        var retry = await Run(args, (request, _) => Response(Fixture(request)));
        Assert.Equal(7, retry.Code); Assert.Equal(original, File.ReadAllText(FilePath("orders.json"))); Assert.Empty(Directory.GetFiles(directory, "*.tmp"));
        var missing = await Run(["stats", "--source", "member", "--output", FilePath("missing/a.txt")], (request, _) => Response(Fixture(request)));
        Assert.Equal(7, missing.Code);
    }
    private sealed class BrokenWriter : StringWriter
    { public override Task WriteAsync(ReadOnlyMemory<char> buffer, CancellationToken cancellationToken = default) => throw new IOException("磁碟滿"); }
    [Fact]
    public async Task StdoutFailureIsNonzero()
    { Assert.Equal(7, (await Run(["stats", "--source", "member"], (request, _) => Response(Fixture(request)), output: new BrokenWriter())).Code); }

    [Fact]
    public async Task OfflineHelpVersionAndEmbeddedTopicsNeedNoConfiguration()
    {
        File.WriteAllText(FilePath(".env"), "invalid secret");
        foreach (var args in new[] { new[] { "--help" }, new[] { "stats", "--help" }, new[] { "list", "--help" }, new[] { "--version" }, new[] { "--skill" } }
            .Concat(SelfDescription.Topics.Select(topic => new[] { "--skill", topic })))
        {
            var result = await Run(args, env: _ => throw new Exception("不可讀組態"));
            Assert.Equal(0, result.Code); Assert.Equal(0, result.Calls); Assert.NotEmpty(result.Out);
        }
        var unknown = await Run(["--skill", "unknown"]);
        Assert.Equal(2, unknown.Code); foreach (var topic in SelfDescription.Topics) Assert.Contains(topic, unknown.Error);
    }

    [Theory]
    [InlineData("normal", 2, 300)][InlineData("test", 1, 900)][InlineData("all", 3, 1200)]
    public async Task StatsUsesApiTotalsInsteadOfPageRowAmounts(string purpose, int count, int amount)
    {
        var result = await Run(["stats", "--source", "order", "--type", purpose, "--format", "json"], (request, _) =>
        {
            Assert.Equal(purpose, Query(request)["purpose"]);
            Assert.Equal("1", Query(request)["pageSize"]);
            var fixture = Fixture(request, count); fixture["totalAmount"] = amount; fixture["knownTotalAmount"] = amount;
            fixture["items"]![0]!["totalPrice"] = 99;
            return Response(fixture);
        });
        Assert.Equal(0, result.Code); Assert.Equal(1, result.Calls);
        var json = JsonNode.Parse(result.Out)!;
        Assert.Equal(amount, json["observations"]![0]!["totalAmount"]!.GetValue<decimal>());
        Assert.Empty(json["rows"]!.AsArray());
    }

    [Theory]
    [InlineData("http", 5)][InlineData("json", 6)][InlineData("cancel", 130)]
    public async Task MidwayFailuresDoNotPublishOrLeaveTemporaryFiles(string failure, int expected)
    {
        using var cancellation = new CancellationTokenSource();
        var result = await Run(["list", "--source", "member", "--all-pages", "--page-size", "1", "--output", FilePath("partial.txt")], (request, call) =>
        {
            if (call == 2)
            {
                if (failure == "http") return new(HttpStatusCode.InternalServerError) { Content = new StringContent(Token) };
                if (failure == "json") return new(HttpStatusCode.OK) { Content = new StringContent("invalid") };
                cancellation.Cancel(); throw new OperationCanceledException(cancellation.Token);
            }
            return Response(Fixture(request));
        }, cancellation: cancellation.Token);
        Assert.Equal(expected, result.Code); Assert.Equal(2, result.Calls); Assert.Empty(result.Out);
        Assert.False(File.Exists(FilePath("partial.txt"))); Assert.Empty(Directory.GetFiles(directory, "*.tmp"));
    }

    [Fact]
    public async Task IdenticalCountUpdatesRemainExplicitlyNonAtomic()
    {
        var result = await Run(["list", "--source", "member", "--all-pages", "--page-size", "1"], (request, call) =>
        { var fixture = Fixture(request); fixture["items"]![0]!["name"] = "第 " + call + " 次讀取"; return Response(fixture); });
        Assert.Equal(0, result.Code); Assert.Contains("非原子快照", result.Out);
    }

    [Fact]
    public async Task TokenReflectedInSuccessfulResponseIsNotPublished()
    {
        var result = await Run(["list", "--source", "member", "--format", "json"], (request, _) =>
        { var fixture = Fixture(request); fixture["items"]![0]!["name"] = Token; return Response(fixture); });
        Assert.Equal(6, result.Code); Assert.Empty(result.Out); Assert.DoesNotContain(Token, result.Error);
    }

    [Fact]
    public async Task UnreadableDotenvIsConfigurationFailure()
    {
        var result = await Run(["stats", "--source", "member", "--env-file", directory]);
        Assert.Equal(3, result.Code); Assert.Equal(0, result.Calls);
    }

    [Theory]
    [InlineData("0.123456789012345678901234567891", 6)]
    [InlineData("1e-100", 6)]
    [InlineData("100.1234567890123456789", 0)]
    [InlineData("1.25e2", 0)]
    public async Task DecimalMustRemainExact(string number, int expected)
    {
        var result = await Run(["list", "--source", "order", "--format", "json"], (request, _) =>
        { var fixture = Fixture(request); fixture["items"]![0]!["totalPrice"] = JsonNode.Parse(number); return Response(fixture); });
        Assert.Equal(expected, result.Code);
        if (expected != 0) Assert.Empty(result.Out);
    }
}
