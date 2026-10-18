using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AndrewDemo.NetConf2023.API.Admin;
using AndrewDemo.NetConf2023.API.Controllers;
using AndrewDemo.NetConf2023.API.Execution;
using AndrewDemo.NetConf2023.Core;
using AndrewDemo.NetConf2023.Core.AdminQueries;
using AndrewDemo.NetConf2023.Core.Contexts;
using AndrewDemo.NetConf2023.Core.Testing;
using LiteDB;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AndrewDemo.NetConf2023.WebContract.Tests;

public sealed class AdminQueryHttpTests
{
    private const string AdminToken = "admin_httpfixture123";

    [Theory]
    [InlineData("orders")]
    [InlineData("checkouts")]
    [InlineData("members")]
    public async Task AuthorizedEndpoints_AreFreshNoStoreAndDoNotExposeTokens(string resource)
    {
        await using var host = await Host.Start(AdminToken);
        var response = await host.Send("/api/admin/" + resource, AdminToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.CacheControl!.NoStore);
        Assert.DoesNotContain(response.Headers, h => h.Key.StartsWith("X-Shop-Transaction"));
        var text = await response.Content.ReadAsStringAsync();
        Assert.DoesNotContain(AdminToken, text); Assert.DoesNotContain(host.MemberToken, text); Assert.DoesNotContain("private-note", text);
        var result = JsonDocument.Parse(text).RootElement;
        Assert.Equal("normal", result.GetProperty("filters").GetProperty("purpose").GetString());
        Assert.Equal(50, result.GetProperty("pageSize").GetInt32());
        Assert.Equal(1, host.Database.MemberTokens.Count());
        if (resource == "members")
        {
            host.Database.Members.Insert(new Member { Id = 20, Name = "newly-committed", IsTest = true });
            var refreshed = await host.Send("/api/admin/members?purpose=test&name=newly-committed", AdminToken);
            var body = await refreshed.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(20, body.GetProperty("items")[0].GetProperty("id").GetInt32());
            Assert.Equal(new[] { "id", "isTest", "name" }, body.GetProperty("items")[0].EnumerateObject().Select(p => p.Name).Order());
        }
    }

    [Theory]
    [InlineData(null)]
    [InlineData("admin_wrong")]
    [InlineData("admin_")]
    [InlineData("ADMIN_httpfixture123")]
    [InlineData("admin_http-fixture123")]
    [InlineData("admin_httpfixture 123")]
    public async Task InvalidAuthorization_IsRejectedBeforeQuery(string? token)
    {
        await using var host = await Host.Start(AdminToken);
        host.Database.Dispose(); // 若有執行查詢就會失敗；仍應回認證拒絕。
        var response = await host.Send("/api/admin/orders", token);
        await AssertError(response, HttpStatusCode.Unauthorized, "admin_unauthorized");
    }

    [Fact]
    public async Task MemberAndAdminTokens_AreNotInterchangeable()
    {
        await using var host = await Host.Start(AdminToken);
        await AssertError(await host.Send("/api/admin/members", host.MemberToken), HttpStatusCode.Unauthorized, "admin_unauthorized");
        var testMember = host.Database.Members.Insert(new Member { Id = 2, Name = "test", IsTest = true });
        var members = host.App.Services.GetRequiredService<MemberService>();
        var testToken = members.IssueToken(2, DateTime.UtcNow.AddHours(1)).Token;
        await AssertError(await host.Send("/api/admin/orders", testToken), HttpStatusCode.Unauthorized, "admin_unauthorized");
        Assert.Equal(HttpStatusCode.OK, (await host.Send("/api/member", host.MemberToken)).StatusCode);
        // 即使資料被外部工具污染而出現相同會員 token，也不可把 admin 當成 Member。
        host.Database.MemberTokens.Insert(new MemberAccessTokenRecord { Token = AdminToken, MemberId = 1, Expire = DateTime.UtcNow.AddHours(1) });
        Assert.Equal(HttpStatusCode.Unauthorized, (await host.Send("/api/member", AdminToken)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await host.Send("/api/checkout/complete", AdminToken, HttpMethod.Post)).StatusCode);
    }

    [Fact]
    public async Task OtherTokenLocationsAndMultipleHeaders_AreRejected()
    {
        await using var host = await Host.Start(AdminToken);
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/admin/orders?access_token=" + AdminToken);
        request.Headers.Add("Cookie", "access-token=" + AdminToken);
        request.Content = JsonContent.Create(new { token = AdminToken });
        await AssertError(await host.Client.SendAsync(request), HttpStatusCode.Unauthorized, "admin_unauthorized");
        using var multiple = new HttpRequestMessage(HttpMethod.Get, "/api/admin/orders");
        multiple.Headers.TryAddWithoutValidation("Authorization", new[] { "Bearer " + AdminToken, "Bearer " + AdminToken });
        await AssertError(await host.Client.SendAsync(multiple), HttpStatusCode.Unauthorized, "admin_unauthorized");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task MissingConfiguration_DisablesAdminAndKeepsMembersWorking(string? configured)
    {
        await using var host = await Host.Start(configured);
        await AssertError(await host.Send("/api/admin/orders", AdminToken), HttpStatusCode.NotFound, "admin_disabled");
        Assert.Equal(HttpStatusCode.OK, (await host.Send("/api/member", host.MemberToken)).StatusCode);
    }

    [Theory]
    [InlineData("admin_")]
    [InlineData("ADMIN_secret")]
    [InlineData("admin_a-b")]
    [InlineData(" admin_secret")]
    [InlineData("admin_secret\n")]
    [InlineData("admin_中文")]
    public void InvalidConfiguredToken_FailsWithoutEchoingSecret(string token)
    {
        var error = Assert.Throws<InvalidOperationException>(() => new AdminAccessToken(token));
        Assert.DoesNotContain(token, error.ToString(), StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("admin_httpfixture123 ")]
    [InlineData(" admin_httpfixture123")]
    [InlineData("admin_HTTPfixture123")]
    public void TokenComparison_DoesNotNormalizeSuppliedSecret(string supplied)
    {
        Assert.False(new AdminAccessToken(AdminToken).Matches(supplied));
    }

    [Fact]
    public async Task RestartWithNewToken_RevokesOldToken()
    {
        await using (var old = await Host.Start(AdminToken)) Assert.Equal(HttpStatusCode.OK, (await old.Send("/api/admin/orders", AdminToken)).StatusCode);
        await using var renewed = await Host.Start("admin_replacement456");
        await AssertError(await renewed.Send("/api/admin/orders", AdminToken), HttpStatusCode.Unauthorized, "admin_unauthorized");
        Assert.Equal(HttpStatusCode.OK, (await renewed.Send("/api/admin/orders", "admin_replacement456")).StatusCode);
    }

    [Theory]
    [InlineData("orders?purpose=TEST")]
    [InlineData("orders?purpose=")]
    [InlineData("orders?page=0")]
    [InlineData("orders?page=-1")]
    [InlineData("orders?page=2147483648")]
    [InlineData("orders?pageSize=101")]
    [InlineData("orders?page=1&page=2")]
    [InlineData("orders?memberId=abc")]
    [InlineData("orders?memberId=0")]
    [InlineData("orders?purpose=normal&Purpose=test")]
    [InlineData("orders?name=test")]
    [InlineData("orders?from=2026-10-18T00:00:00")]
    [InlineData("orders?from=2026-02-30T00:00:00Z")]
    [InlineData("orders?from=2026-10-18T00:00:00Z&to=2026-10-18T00:00:00Z")]
    [InlineData("orders?from=2026-10-19T00:00:00Z&to=2026-10-18T00:00:00Z")]
    [InlineData("checkouts?state=failed")]
    [InlineData("checkouts?timeField=paidAt")]
    [InlineData("members?from=2026-10-18T00:00:00Z")]
    [InlineData("members?names=test")]
    public async Task InvalidQuery_Is400WithStableEnvelope(string query)
    {
        await using var host = await Host.Start(AdminToken);
        await AssertError(await host.Send("/api/admin/" + query, AdminToken), HttpStatusCode.BadRequest, "admin_query_invalid");
    }

    [Fact]
    public async Task OrderTotalsAndUtcRange_AreSharedAcrossPages()
    {
        await using var host = await Host.Start(AdminToken);
        var now = new DateTime(2026, 10, 18, 0, 0, 0, DateTimeKind.Utc);
        foreach (var (id, amount, isTest, date) in new[] { (1, 100, false, now), (2, 200, false, now), (3, 900, true, now), (4, 800, false, now.AddDays(1)) })
            host.Database.Orders.Insert(new Order(id) { Buyer = new Member { Id = 1, Name = "saved" }, TotalPrice = amount, IsTest = isTest, CreatedAt = date });
        var response = await host.Send("/api/admin/orders?pageSize=1&from=2026-10-18T08:00:00%2B08:00&to=2026-10-19T00:00:00Z", AdminToken);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(300, body.GetProperty("totalAmount").GetDecimal()); Assert.Equal(2, body.GetProperty("totalCount").GetInt32());
        Assert.Single(body.GetProperty("items").EnumerateArray()); Assert.Equal(2, body.GetProperty("items")[0].GetProperty("id").GetInt32());
        Assert.Equal("2026-10-18T00:00:00Z", body.GetProperty("filters").GetProperty("from").GetString());
        Assert.Equal(1, body.GetProperty("items")[0].GetProperty("memberId").GetInt32());
        Assert.Equal("pending", body.GetProperty("items")[0].GetProperty("fulfillmentStatus").GetString());
    }

    [Fact]
    public async Task StoredDataAndInfrastructureErrors_AreDistinctAndDoNotLeakDetails()
    {
        await using var host = await Host.Start(AdminToken);
        host.Database.Database.GetCollection("orders").Insert(new BsonDocument { ["_id"] = 1, ["IsTest"] = AdminToken });
        var invalid = await host.Send("/api/admin/orders", AdminToken);
        await AssertError(invalid, HttpStatusCode.InternalServerError, "admin_data_invalid");
        Assert.DoesNotContain(AdminToken, await invalid.Content.ReadAsStringAsync());
        host.Database.Dispose();
        await AssertError(await host.Send("/api/admin/orders", AdminToken), HttpStatusCode.InternalServerError, "admin_query_failed");
    }

    private static async Task AssertError(HttpResponseMessage response, HttpStatusCode status, string code)
    {
        Assert.Equal(status, response.StatusCode);
        Assert.True(response.Headers.CacheControl!.NoStore);
        Assert.Equal(code, (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
    }

    private sealed class Host : IAsyncDisposable
    {
        private readonly string _path = Path.Combine(Path.GetTempPath(), $"admin-http-{Guid.NewGuid():N}.db");
        public WebApplication App = null!;
        public HttpClient Client = null!;
        public ShopDatabaseContext Database = null!;
        public string MemberToken = null!;
        public static async Task<Host> Start(string? configured)
        {
            var host = new Host();
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseUrls("http://127.0.0.1:0");
            builder.Logging.ClearProviders();
            host.Database = new ShopDatabaseContext(new() { ConnectionString = $"Filename={host._path};Connection=Direct" });
            builder.Services.AddSingleton<IShopDatabaseContext>(host.Database);
            builder.Services.AddSingleton(TimeProvider.System);
            builder.Services.AddSingleton(new TestAllowlist());
            builder.Services.AddSingleton<MemberService>();
            builder.Services.AddShopExecutionContexts();
            builder.Services.AddSingleton(new AdminAccessToken(configured));
            builder.Services.AddSingleton<AdminQueryService>();
            builder.Services.AddControllers().AddApplicationPart(typeof(AdminQueryController).Assembly);
            host.App = builder.Build();
            var members = host.App.Services.GetRequiredService<MemberService>();
            var member = members.GetOrCreate("normal"); member.ShopNotes = "private-note"; members.UpdateMember(member);
            host.MemberToken = members.IssueToken(member.Id, DateTime.UtcNow.AddHours(1)).Token;
            host.App.UseRouting();
            host.App.UseMiddleware<AdminQueryMiddleware>();
            host.App.UseWhen(http => !http.Request.Path.StartsWithSegments("/api/admin"), branch => branch.UseMiddleware<ShopExecutionMiddleware>());
            host.App.MapControllers();
            await host.App.StartAsync();
            host.Client = new HttpClient { BaseAddress = new Uri(host.App.Urls.Single()) };
            return host;
        }
        public Task<HttpResponseMessage> Send(string path, string? token, HttpMethod? method = null)
        {
            var request = new HttpRequestMessage(method ?? HttpMethod.Get, path);
            if (token != null) request.Headers.TryAddWithoutValidation("Authorization", "Bearer " + token);
            return Client.SendAsync(request);
        }
        public async ValueTask DisposeAsync()
        {
            Client.Dispose(); await App.DisposeAsync(); Database.Dispose(); File.Delete(_path);
        }
    }
}
