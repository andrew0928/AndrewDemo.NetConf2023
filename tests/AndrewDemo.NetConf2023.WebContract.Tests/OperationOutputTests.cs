using AndrewDemo.NetConf2023.Abstract.Products;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using AndrewDemo.NetConf2023.Abstract.Contexts;
using AndrewDemo.NetConf2023.Abstract.Operations;
using AndrewDemo.NetConf2023.API.Controllers;
using AndrewDemo.NetConf2023.Core;
using AndrewDemo.NetConf2023.Core.Contexts;
using AndrewDemo.NetConf2023.Core.Effects;
using AndrewDemo.NetConf2023.Core.Operations;
using AndrewDemo.NetConf2023.Core.Products;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AndrewDemo.NetConf2023.WebContract.Tests;

public sealed partial class ExecutionScopeTests
{
    private static void AssertHeaders(HttpResponseMessage response, string? behavior, string outcome,
        int? transactionId = null, bool? transactionIsTest = null, string? skipped = null)
    {
        Assert.Equal(outcome, response.Headers.GetValues("X-Shop-Outcome").Single());
        if (behavior == null) Assert.False(response.Headers.Contains("X-Shop-Behavior"));
        else Assert.Equal(behavior, response.Headers.GetValues("X-Shop-Behavior").Single());
        if (skipped == null) Assert.False(response.Headers.Contains("X-Shop-Skipped-Effects"));
        else Assert.Equal(skipped, response.Headers.GetValues("X-Shop-Skipped-Effects").Single());
        if (transactionId == null)
        {
            Assert.False(response.Headers.Contains("X-Shop-Transaction-Type"));
            Assert.False(response.Headers.Contains("X-Shop-Transaction-Id"));
            Assert.False(response.Headers.Contains("X-Shop-Transaction-Is-Test"));
        }
        else
        {
            Assert.Equal("checkout", response.Headers.GetValues("X-Shop-Transaction-Type").Single());
            Assert.Equal(transactionId.ToString(), response.Headers.GetValues("X-Shop-Transaction-Id").Single());
            Assert.Equal(transactionIsTest == true ? "true" : "false", response.Headers.GetValues("X-Shop-Transaction-Is-Test").Single());
        }
    }

    private Cart SeedInventoryCart(string prefix)
    {
        var cart = new Cart();
        foreach (var suffix in new[] { "a", "b" })
        {
            var id = prefix + suffix;
            _database.Products.Insert(new Product() { Id = id, Name = id, Price = 100m, SkuId = id });
            _database.Skus.Insert(new SkuRecord() { SkuId = id });
            _database.InventoryRecords.Insert(new InventoryRecord() { SkuId = id, AvailableQuantity = 10 });
            cart.AddProducts(id, 2, DateTime.UtcNow);
        }
        _database.Carts.Insert(cart);
        return cart;
    }

    private async Task<int> CreateCheckout(Cart cart, string token, int payment)
    {
        var response = await Send("/api/checkout/create", token, JsonSerializer.Serialize(new { cartId = cart.Id, paymentId = payment }));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(new[] { "consumerId", "consumerName", "transactionId", "transactionStartAt" }, json.EnumerateObject().Select(x => x.Name).Order().ToArray());
        var id = json.GetProperty("transactionId").GetInt32();
        var test = token == _testToken || payment == 990001;
        AssertHeaders(response, test ? "test" : "normal", "succeeded", id, test);
        return id;
    }

    [Theory]
    [InlineData(false, 0)]
    [InlineData(false, 990001)]
    [InlineData(true, 0)]
    [InlineData(true, 990001)]
    public async Task CheckoutOutputs_UsePersistedPurposeAndKeepEverySkippedTarget(bool testMember, int payment)
    {
        var cart = SeedInventoryCart("result-");
        var token = testMember ? _testToken : _normalToken;
        var test = testMember || payment == 990001;
        var id = await CreateCheckout(cart, token, payment);
        var response = await Send("/api/checkout/complete", token, $"{{\"transactionId\":{id}}}");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        AssertHeaders(response, test ? "test" : "normal", "succeeded", id, test, test ? "inventory.deduct" : null);
        Assert.Null(_database.Transactions.Checkout(id));
        Assert.Equal(test, _database.Transactions.Checkout(id, true)!.IsTest);
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(payment, body.GetProperty("paymentId").GetInt32());
        Assert.Equal(test, body.GetProperty("orderDetail").GetProperty("isTest").GetBoolean());
        Assert.All(_database.InventoryRecords.FindAll(), inventory => Assert.Equal(test ? 10 : 8, inventory.AvailableQuantity));
        var result = _executions.Last().Result!;
        Assert.Equal(test ? 2 : 0, result.SkippedEffects.Count);
        var logs = _logs.Entries.Where(x => x.Fields.ContainsKey("EffectCode")).ToArray();
        Assert.Equal(test ? 2 : 0, logs.Length);
        Assert.All(logs, entry =>
        {
            Assert.Equal(LogLevel.Information, entry.Level);
            Assert.Equal(_executions.Last().TraceId, entry.Fields["TraceId"]);
            Assert.Equal("test", entry.Fields["ShopId"]);
            Assert.Equal(IdentityKind.Member, entry.Fields["IdentityKind"]);
            Assert.Equal(testMember ? 2 : 1, entry.Fields["MemberId"]);
            Assert.Equal(TransactionKind.Checkout, entry.Fields["TransactionKind"]);
            Assert.Equal(id, entry.Fields["TransactionId"]); Assert.Equal(true, entry.Fields["TransactionIsTest"]);
            Assert.Equal(OperationOutcome.Succeeded, entry.Fields["Outcome"]);
            Assert.Equal("test_behavior", entry.Fields["ReasonCode"]);
        });
        Assert.Equal(test ? new[] { "result-a", "result-b" } : [], logs.Select(x => (string)x.Fields["TargetId"]!).Order().ToArray());
        var repeated = await Send("/api/checkout/complete", token, $"{{\"transactionId\":{id}}}");
        Assert.Equal(HttpStatusCode.BadRequest, repeated.StatusCode);
        AssertHeaders(repeated, testMember ? "test" : "normal", "rejected");
    }

    [Fact]
    public async Task InventoryRejection_AndMissingPayment_DoNotInventSkippedEffects()
    {
        var cart = SeedInventoryCart("reject-");
        var id = await CreateCheckout(cart, _testToken, 0);
        _database.InventoryRecords.Update(new InventoryRecord() { SkuId = "reject-b", AvailableQuantity = 1 });
        var response = await Send("/api/checkout/complete", _testToken, $"{{\"transactionId\":{id}}}");
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        AssertHeaders(response, "test", "rejected", id, true);
        Assert.Empty(_database.Transactions.Orders("all")); Assert.NotNull(_database.Transactions.Checkout(id));
        Assert.Empty(_executions.Last().Result!.SkippedEffects);
        Assert.DoesNotContain(_logs.Entries, x => x.Fields.ContainsKey("EffectCode"));
        var missing = await Send("/api/checkout/create", _testToken, $"{{\"cartId\":{cart.Id}}}");
        Assert.Equal(HttpStatusCode.BadRequest, missing.StatusCode); AssertHeaders(missing, "test", "rejected");
    }

    [Theory]
    [InlineData("ok", 200, "succeeded", "sku.update")]
    [InlineData("reject", 403, "rejected", null)]
    [InlineData("fail", 500, "failed", "sku.update")]
    [InlineData("bug", 500, "failed", null)]
    public async Task MaintenanceOutput_PreservesActualEffectsEvenWhenLaterStepFails(string mode, int status, string outcome, string? skipped)
    {
        _database.Products.Insert(new Product() { Id = "p", Price = 100m });
        _database.Skus.Insert(new SkuRecord() { SkuId = "s", SpecificationSummary = "原規格" });
        var response = await Send($"/api/output-probe/maintenance/{mode}", _testToken, "{}");
        Assert.Equal(status, (int)response.StatusCode);
        AssertHeaders(response, mode == "bug" ? null : "test", outcome, skipped: skipped);
        Assert.Equal(100m, _database.Products.FindById("p").Price);
        Assert.Equal("原規格", _database.Skus.FindById("s").SpecificationSummary);
        var effects = _executions.Last().Result?.SkippedEffects;
        Assert.Equal(skipped == null ? 0 : 3, effects?.Count ?? 0);
        var logs = _logs.Entries.Where(x => x.Fields.ContainsKey("EffectCode")).ToArray();
        Assert.Equal(skipped == null ? 0 : 3, logs.Length);
        Assert.All(logs, entry =>
        {
            Assert.Null(entry.Fields["TransactionKind"]); Assert.Null(entry.Fields["TransactionId"]); Assert.Null(entry.Fields["TransactionIsTest"]);
            Assert.Equal(outcome, entry.Fields["Outcome"]!.ToString()!.ToLowerInvariant());
            Assert.NotNull(entry.Fields["RequestedChange"]);
        });
    }

    [Fact]
    public async Task TestIdentityPriceChange_ReturnsExplicit403_AndLaterCheckoutKeepsOriginalPrice()
    {
        _database.Products.Insert(new Product { Id = "p", Price = 100m });
        var denied = await Send("/api/output-probe/maintenance/price", _testToken, "{}");
        Assert.Equal(HttpStatusCode.Forbidden, denied.StatusCode);
        Assert.Equal("test_identity_forbidden", (await denied.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("code").GetString());
        AssertHeaders(denied, "test", "rejected");
        Assert.Empty(_executions.Last().Result!.SkippedEffects);
        Assert.Equal(100m, _database.Products.FindById("p").Price);
        var denialLog = Assert.Single(_logs.Entries, x => x.Fields.TryGetValue("ReasonCode", out var reason) && Equals(reason, "test_identity_forbidden"));
        Assert.Equal("p", denialLog.Fields["TargetId"]);
        var cart = new Cart(); cart.AddProducts("p", 1, DateTime.UtcNow); _database.Carts.Insert(cart);
        var id = await CreateCheckout(cart, _testToken, 0);
        Assert.Equal(100m, _database.Transactions.Checkout(id)!.TotalPrice);
        var allowed = await Send("/api/output-probe/maintenance/price", _normalToken, "{}");
        Assert.Equal(HttpStatusCode.OK, allowed.StatusCode); AssertHeaders(allowed, "normal", "succeeded");
        Assert.Equal(80m, _database.Products.FindById("p").Price);
    }

    [Fact]
    public async Task NormalMaintenanceFailure_DoesNotPretendToSkipOrRollback()
    {
        _database.Products.Insert(new Product { Id = "p", Price = 100m });
        _database.Skus.Insert(new SkuRecord { SkuId = "s", SpecificationSummary = "原規格" });
        var response = await Send("/api/output-probe/maintenance/fail", _normalToken, "{}");
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        AssertHeaders(response, "normal", "failed");
        Assert.Equal(100m, _database.Products.FindById("p").Price);
        Assert.Equal("第三次規格", _database.Skus.FindById("s").SpecificationSummary);
        Assert.Empty(_executions.Last().Result!.SkippedEffects);
        Assert.DoesNotContain(_logs.Entries, x => x.Fields.ContainsKey("EffectCode"));
    }

    [Fact]
    public async Task AnonymousCartAndOrderCollection_HaveNoSingleTransaction()
    {
        AssertHeaders(await _client.GetAsync("/api/products"), "normal", "succeeded");
        AssertHeaders(await Send("/api/carts/create", _testToken, "{}"), "test", "succeeded");
        _database.Transactions.InsertOrder(new Order(50) { Buyer = new() { Id = 2 }, IsTest = true });
        var response = await Send("/api/member/orders", _testToken);
        AssertHeaders(response, "test", "succeeded");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(Assert.Single(body.GetProperty("orders").EnumerateArray()).GetProperty("isTest").GetBoolean());
    }

    [Fact]
    public async Task NormalMemberHistory_IncludesOwnBothPurposesAndExcludesOtherBuyers()
    {
        foreach (var (id, buyer, test) in new[] { (50, 1, false), (51, 1, true), (52, 2, false), (53, 2, true) })
            _database.Transactions.InsertOrder(new Order(id) { Buyer = new() { Id = buyer }, IsTest = test });
        var response = await Send("/api/member/orders", _normalToken);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode); AssertHeaders(response, "normal", "succeeded");
        var orders = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("orders").EnumerateArray().ToArray();
        Assert.Equal(new[] { 50, 51 }, orders.Select(x => x.GetProperty("id").GetInt32()).Order());
        Assert.Contains(orders, x => x.GetProperty("isTest").GetBoolean());
        Assert.Contains(orders, x => !x.GetProperty("isTest").GetBoolean());
    }

    [Fact]
    public async Task ParallelNormalAndTestOutputs_KeepResultAndLogsSeparate()
    {
        var normal = await CreateCheckout(SeedInventoryCart("normal-"), _normalToken, 0);
        var test = await CreateCheckout(SeedInventoryCart("test-"), _normalToken, 990001);
        var responses = await Task.WhenAll(Send("/api/checkout/complete", _normalToken, $"{{\"transactionId\":{normal}}}"),
            Send("/api/checkout/complete", _normalToken, $"{{\"transactionId\":{test}}}"));
        AssertHeaders(responses[0], "normal", "succeeded", normal, false);
        AssertHeaders(responses[1], "test", "succeeded", test, true, "inventory.deduct");
        var executions = _executions.Where(x => x.Transaction?.Id == normal || x.Transaction?.Id == test).ToArray();
        Assert.Equal(4, executions.Select(x => x.TraceId).Distinct().Count());
        Assert.All(_logs.Entries.Where(x => x.Fields.ContainsKey("EffectCode")), entry =>
        {
            Assert.Equal(test, entry.Fields["TransactionId"]);
            var execution = Assert.Single(executions, x => x.TraceId == (string)entry.Fields["TraceId"]!);
            Assert.True(execution.Result!.IsTestBehavior); Assert.Equal(2, execution.Result.SkippedEffects.Count);
        });
    }

    [Theory]
    [InlineData("success", OperationOutcome.Succeeded)]
    [InlineData("reject", OperationOutcome.Rejected)]
    [InlineData("fail", OperationOutcome.Failed)]
    public void CliResult_IsDirectImmutableSnapshotWithoutHttp(string mode, OperationOutcome outcome)
    {
        _database.Products.Insert(new Product() { Id = "p", Price = 100m });
        using var scope = ShopOperationScope.Open(_app.Services.GetRequiredService<IServiceScopeFactory>(), _testToken);
        var service = scope.Services.GetRequiredService<ProductMaintenanceService>();
        _database.Skus.Insert(new SkuRecord { SkuId = "cli-sku" });
        if (mode != "reject") service.UpdateSpecification("cli-sku", "新規格", _ => { });
        var result = mode == "success" ? scope.Complete() : scope.Fail(mode == "reject"
            ? new UnauthorizedAccessException("拒絕") : new IOException("失敗"));
        Assert.NotNull(result); Assert.True(result.IsTestBehavior); Assert.Equal(outcome, result.Outcome);
        Assert.Equal(mode == "reject" ? 0 : 1, result.SkippedEffects.Count); Assert.Null(scope.Execution.Transaction);
        scope.Services.GetRequiredService<IdentityContext>().Initialize(IdentitySnapshot.System);
        Assert.True(result.IsTestBehavior);
        Assert.Same(result, scope.Complete());
    }

    [Fact]
    public async Task OAuthAndAuthenticatedPaths_NeverLogTokensOrGrantCodes()
    {
        var original = Console.Out;
        using var output = new StringWriter();
        Console.SetOut(output);
        string token = "", code = "";
        try
        {
            using var direct = new HttpClient(new HttpClientHandler { AllowAutoRedirect = false }) { BaseAddress = _client.BaseAddress };
            var grant = await direct.PostAsync("/oauth/authorize", new FormUrlEncodedContent(new Dictionary<string, string>
            { ["name"] = "log-user", ["client_id"] = "test", ["redirect_uri"] = "http://localhost/callback" }));
            code = grant.Headers.GetValues("X-OAuth-Code").Single();
            var exchange = await direct.PostAsync("/oauth/token", new FormUrlEncodedContent(new Dictionary<string, string> { ["code"] = code }));
            Assert.Equal(HttpStatusCode.OK, exchange.StatusCode);
            token = _database.MemberTokens.FindAll().Single(x => x.MemberId == _database.Members.FindOne(x => x.Name == "log-user").Id).Token;
            var exchangedToken = (await exchange.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("access_token").GetString()!;
            Assert.Equal(HttpStatusCode.OK, (await Send("/api/member", exchangedToken)).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await direct.PostAsync("/oauth/token", new FormUrlEncodedContent(new Dictionary<string, string> { ["code"] = exchangedToken }))).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await direct.PostAsync("/oauth/token", new FormUrlEncodedContent(new Dictionary<string, string> { ["code"] = code }))).StatusCode);
            Assert.Equal(HttpStatusCode.Unauthorized, (await Send("/api/member", "recognizable-invalid-secret")).StatusCode);
            _database.MemberTokens.Insert(new MemberAccessTokenRecord() { Token = "recognizable-conflict-secret", MemberId = 2, IsTest = false, Expire = DateTime.MaxValue });
            Assert.Equal(HttpStatusCode.InternalServerError, (await Send("/api/member", "recognizable-conflict-secret")).StatusCode);
            _database.MemberTokens.Insert(new MemberAccessTokenRecord { Token = "recognizable-expired-secret", MemberId = 2, IsTest = true, Expire = DateTime.UtcNow.AddMinutes(-1) });
            Assert.Equal(HttpStatusCode.Unauthorized, (await Send("/api/member", "recognizable-expired-secret")).StatusCode);
            Assert.Equal(HttpStatusCode.BadRequest, (await direct.PostAsync("/oauth/authorize", new FormUrlEncodedContent(new Dictionary<string, string>
            { ["client_id"] = "test", ["redirect_uri"] = "http://localhost/callback" }))).StatusCode);
            var text = output + string.Join("\n", _logs.Entries.Select(x => x.Message + x.Exception + JsonSerializer.Serialize(x.Fields)));
            foreach (var secret in new[] { token, exchangedToken, code, "recognizable-invalid-secret", "recognizable-conflict-secret", "recognizable-expired-secret" })
                Assert.DoesNotContain(secret, text);
            Assert.Contains(_logs.Entries, x => x.Message.Contains("OAuth token exchange 完成"));
        }
        finally { Console.SetOut(original); }
    }
}

// 僅測試組件註冊的探針；production 沒有新增維護 API。
[ApiController]
[Route("api/output-probe")]
public sealed class OperationOutputProbeController(ProductMaintenanceService products) : ControllerBase
{
    [HttpPost("maintenance/{mode}")]
    public IActionResult Maintain(string mode)
    {
        if (mode == "price") { products.UpdatePrice("p", 80m, _ => { }); return Ok(); }
        if (mode == "bug") throw new ExecutionContextInvalidException("用途無效");
        products.UpdateSpecification("s", "新規格", _ => { if (mode == "reject") throw new UnauthorizedAccessException(); });
        products.UpdateSpecification("s", "第二次規格", _ => { });
        products.UpdateSpecification("s", "第三次規格", _ => { });
        if (mode == "fail") throw new IOException("稍後步驟失敗");
        return Ok(new { changed = true });
    }
}
