using System.Collections.Concurrent;
using AndrewDemo.NetConf2023.Abstract.Operations;
using AndrewDemo.NetConf2023.Core.Operations;
using AndrewDemo.NetConf2023.Core.Effects;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using AndrewDemo.NetConf2023.Abstract.Contexts;
using AndrewDemo.NetConf2023.Abstract.Orders;
using AndrewDemo.NetConf2023.Abstract.Products;
using AndrewDemo.NetConf2023.Abstract.Shops;
using AndrewDemo.NetConf2023.API.Controllers;
using AndrewDemo.NetConf2023.API.Execution;
using AndrewDemo.NetConf2023.Core;
using AndrewDemo.NetConf2023.Core.Checkouts;
using AndrewDemo.NetConf2023.Core.Contexts;
using AndrewDemo.NetConf2023.Core.Discounts;
using AndrewDemo.NetConf2023.Core.Orders;
using AndrewDemo.NetConf2023.Core.Products;
using AndrewDemo.NetConf2023.Core.Testing;
using LiteDB;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;

namespace AndrewDemo.NetConf2023.WebContract.Tests;

public sealed partial class ExecutionScopeTests : IAsyncLifetime
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"tip22-{Guid.NewGuid():N}.db");
    private WebApplication _app = null!;
    private HttpClient _client = null!;
    private ShopDatabaseContext _database = null!;
    private readonly OperationLogCapture _logs = new();
    private readonly ConcurrentQueue<OperationExecution> _executions = new();
    private string _normalToken = null!;
    private string _testToken = null!;

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions { EnvironmentName = "Development" });
        builder.WebHost.UseUrls("http://127.0.0.1:0");
        builder.Logging.ClearProviders();
        builder.Logging.AddProvider(_logs);
        builder.Logging.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);
        builder.Host.UseDefaultServiceProvider(options => { options.ValidateScopes = true; options.ValidateOnBuild = true; });
        _database = new ShopDatabaseContext(new ShopDatabaseOptions { ConnectionString = $"Filename={_path};Connection=Direct" });
        builder.Services.AddSingleton<IShopDatabaseContext>(_database);
        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddSingleton(new TestAllowlist(["test"], [990001]));
        builder.Services.AddSingleton<MemberService>();
        builder.Services.AddSingleton(new ShopManifest { ShopId = "test" });
        builder.Services.AddSingleton(new DiscountEngine([]));
        builder.Services.AddSingleton<IProductService, DefaultProductService>();
        builder.Services.AddSingleton<IOrderEventDispatcher, DefaultOrderEventDispatcher>();
        builder.Services.AddShopExecutionContexts();
        builder.Services.AddControllers(options => options.Filters.Add<CheckoutTransactionFilter>())
            .AddApplicationPart(typeof(CheckoutController).Assembly)
            .AddApplicationPart(typeof(ContextProbeController).Assembly);
        _app = builder.Build();
        var members = _app.Services.GetRequiredService<MemberService>();
        _normalToken = members.IssueToken(members.GetOrCreate("normal").Id, DateTime.MaxValue).Token;
        _testToken = members.IssueToken(members.GetOrCreate("test").Id, DateTime.MaxValue).Token;
        _database.Database.GetCollection<CheckoutTransactionRecord>("checkout_transactions").Insert(new CheckoutTransactionRecord { TransactionId = 10, MemberId = 1, IsTest = false });
        _database.Database.GetCollection<CheckoutTransactionRecord>("test_checkout_transactions").Insert(new CheckoutTransactionRecord { TransactionId = 20, MemberId = 2, IsTest = true });
        _app.UseRouting();
        _app.Use(async (http, next) =>
        {
            // 在正式出口完成後擷取結果；測試專用 header 保留前階段的 context 驗證。
            http.Response.OnStarting(() =>
            {
                _executions.Enqueue(http.RequestServices.GetRequiredService<OperationExecution>());
                http.Response.Headers["X-Test-Skipped-Count"] = http.RequestServices.GetRequiredService<OperationEffects>().Skipped.Count.ToString();
                var snapshot = http.RequestServices.GetRequiredService<TransactionContext>().Current;
                if (snapshot != null)
                {
                    http.Response.Headers["X-Test-Context-Id"] = snapshot.Id.ToString();
                    http.Response.Headers["X-Test-Context-Kind"] = snapshot.Kind.ToString();
                    http.Response.Headers["X-Test-Context-IsTest"] = snapshot.IsTest.ToString();
                }
                return Task.CompletedTask;
            });
            await next();
        });
        _app.UseMiddleware<ShopExecutionMiddleware>();
        _app.MapControllers();
        await _app.StartAsync();
        _client = new HttpClient { BaseAddress = new Uri(_app.Urls.Single()) };
    }

    [Fact]
    public async Task RealCheckoutEndpoints_BindCreateAndRetainCheckoutSnapshotAfterCompletion()
    {
        _database.Products.Insert(new Product { Id = "quote", Name = "商品", Price = 100m });
        var cart = new Cart();
        cart.AddProducts("quote", 2, DateTime.UtcNow);
        _database.Carts.Insert(cart);
        var created = await Send("/api/checkout/create", _testToken,
            System.Text.Json.JsonSerializer.Serialize(new { cartId = cart.Id, paymentId = 0 }));
        Assert.Equal(HttpStatusCode.OK, created.StatusCode);
        var body = await created.Content.ReadFromJsonAsync<CheckoutController.CheckoutCreateResponse>();
        Assert.Equal(body!.TransactionId.ToString(), created.Headers.GetValues("X-Test-Context-Id").Single());
        Assert.Equal("True", created.Headers.GetValues("X-Test-Context-IsTest").Single());
        _database.Carts.Delete(cart.Id);
        var completed = await Send("/api/checkout/complete", _testToken,
            System.Text.Json.JsonSerializer.Serialize(new { transactionId = body.TransactionId, paymentId = 999 }));
        Assert.Equal(HttpStatusCode.OK, completed.StatusCode);
        Assert.Equal(body.TransactionId.ToString(), completed.Headers.GetValues("X-Test-Context-Id").Single());
        Assert.Equal("Checkout", completed.Headers.GetValues("X-Test-Context-Kind").Single());
        using var result = System.Text.Json.JsonDocument.Parse(await completed.Content.ReadAsStringAsync());
        Assert.Equal(0, result.RootElement.GetProperty("paymentId").GetInt32());
        var order = result.RootElement.GetProperty("orderDetail");
        Assert.Equal(200m, order.GetProperty("totalPrice").GetDecimal());
        Assert.True(order.GetProperty("isTest").GetBoolean());
        Assert.Equal(body.TransactionId, order.GetProperty("checkoutTransactionId").GetInt32());
        Assert.Equal(CheckoutState.Completed, _database.Transactions.Checkout(body.TransactionId, true)!.State);
        Assert.Equal(result.RootElement.GetProperty("transactionCompleteAt").GetDateTime(),
            _database.Transactions.Checkout(body.TransactionId, true)!.ClosedAt!.Value.ToUniversalTime());
        Assert.Equal(result.RootElement.GetProperty("transactionCompleteAt").GetDateTime(),
            _database.Transactions.Order(order.GetProperty("id").GetInt32())!.CreatedAt!.Value.ToUniversalTime());
        var repeated = await Send("/api/checkout/complete", _testToken,
            System.Text.Json.JsonSerializer.Serialize(new { transactionId = body.TransactionId }));
        Assert.Equal(HttpStatusCode.BadRequest, repeated.StatusCode);
        Assert.Single(_database.Transactions.Orders("all"));
    }

    [Theory]
    [InlineData("State")]
    [InlineData("TotalPrice")]
    [InlineData("ProductLines")]
    public async Task LegacyQuoteViaHttp_Returns409AndKeepsOriginal(string missing)
    {
        var raw = new BsonDocument
        {
            ["_id"] = 30, ["MemberId"] = 2, ["PaymentId"] = 0, ["IsTest"] = true,
            ["State"] = "Created", ["TotalPrice"] = 0m,
            ["ProductLines"] = new BsonArray(), ["DiscountLines"] = new BsonArray()
        };
        raw.Remove(missing);
        _database.Database.GetCollection("test_checkout_transactions").Insert(raw);
        var response = await Send("/api/checkout/complete", _testToken, "{\"transactionId\":30}");
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("checkout_recreate_required", await response.Content.ReadAsStringAsync());
        Assert.Equal("rejected", response.Headers.GetValues("X-Shop-Outcome").Single());
        Assert.Equal("test", response.Headers.GetValues("X-Shop-Behavior").Single());
        Assert.False(response.Headers.Contains("X-Shop-Skipped-Effects"));
        Assert.Equal(raw.ToString(), _database.Database.GetCollection("test_checkout_transactions").FindById(30).ToString());
        Assert.Empty(_database.Transactions.Orders("all"));
    }

    [Fact]
    public async Task CreateMissingProductViaHttp_Returns400WithoutBinding()
    {
        var cart = new Cart();
        cart.AddProducts("missing", 1, DateTime.UtcNow);
        _database.Carts.Insert(cart);
        var response = await Send("/api/checkout/create", _normalToken,
            System.Text.Json.JsonSerializer.Serialize(new { cartId = cart.Id, paymentId = 0 }));
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.False(response.Headers.Contains("X-Test-Context-Id"));
        Assert.Equal(2, _database.Transactions.Checkouts("all").Length);
    }

    [Fact]
    public async Task AnonymousProductsAndAuthenticatedNontransactionEndpoints_KeepTransactionNull()
    {
        Assert.Equal(HttpStatusCode.OK, (await _client.GetAsync("/api/products")).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await _client.GetAsync("/api/member")).StatusCode);
        var response = await Send("/api/context-probe/20", _testToken);
        var probe = await response.Content.ReadFromJsonAsync<ContextProbe>();
        Assert.Equal(2, probe!.MemberId);
        Assert.True(probe.IdentityIsTest);
        Assert.Null(probe.TransactionId);
        Assert.True(probe.SameInstances);
        Assert.False(response.Headers.Contains("X-Shop-Transaction-Id"));
        Assert.Equal(HttpStatusCode.OK, (await Send("/api/member/orders", _testToken)).StatusCode);
    }

    [Theory]
    [InlineData("{\"transactionId\":20}")]
    [InlineData("{\"TransactionId\":\"20\"}")]
    [InlineData("{\"transactionId\":10,\"transactionId\":20}")]
    public async Task CompleteMetadata_UsesExactlyTheMvcBoundId(string json)
    {
        var response = await Send("/api/context-probe/complete", _testToken, json);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var probe = await response.Content.ReadFromJsonAsync<ContextProbe>();
        Assert.Equal(20, probe!.TransactionId);
        Assert.Equal(probe.BoundRequestId, probe.TransactionId);
        AssertHeaders(response, "test", "succeeded", 20, true);
        Assert.True(probe.TransactionIsTest);
        Assert.True(probe.SameInstances);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("null")]
    [InlineData("{")]
    [InlineData("{\"transactionId\":null}")]
    [InlineData("{\"transactionId\":\"invalid\"}")]
    [InlineData("{\"transactionId\":2147483648}")]
    public async Task InvalidPayload_IsRejectedBeforeBindingOrEffects(string json)
    {
        var response = await Send("/api/context-probe/complete", _testToken, json);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        AssertHeaders(response, "test", "rejected");
        Assert.Empty(_database.Transactions.Orders("all"));
    }

    [Fact]
    public async Task OversizedAndUnsupportedPayloads_AreRejected()
    {
        var response = await Send("/api/context-probe/complete", _testToken,
            "{\"transactionId\":20,\"shopComments\":\"" + new string('x', 70 * 1024) + "\"}");
        Assert.Contains(response.StatusCode, new[] { HttpStatusCode.BadRequest, HttpStatusCode.RequestEntityTooLarge });
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/context-probe/complete");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _testToken);
        request.Content = new StringContent("transactionId=20", Encoding.UTF8, "text/plain");
        Assert.Equal(HttpStatusCode.UnsupportedMediaType, (await _client.SendAsync(request)).StatusCode);
    }

    [Theory]
    [InlineData("Basic secret")]
    [InlineData("Bearer")]
    [InlineData("Bearer unknown")]
    [InlineData("x")]
    public async Task InvalidAuthorization_Returns401WithoutExecutingAction(string authorization)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/context-probe/complete");
        request.Headers.TryAddWithoutValidation("Authorization", authorization);
        request.Content = JsonContent.Create(new { transactionId = 20 });
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        AssertHeaders(response, null, "rejected");
    }

    [Fact]
    public async Task UnauthorizedMissingAndContradictoryTransactions_DoNotLeakOrExecute()
    {
        Assert.Equal(HttpStatusCode.Forbidden, (await Send("/api/context-probe/complete", _normalToken, "{\"transactionId\":20}")).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await Send("/api/context-probe/complete", _testToken, "{\"transactionId\":999}")).StatusCode);
        _database.MemberTokens.Insert(new MemberAccessTokenRecord { Token = "bad-purpose", MemberId = 2, IsTest = false, Expire = DateTime.MaxValue });
        var response = await Send("/api/context-probe/complete", "bad-purpose", "{\"transactionId\":20}");
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Contains("execution_context_invalid", await response.Content.ReadAsStringAsync());
        AssertHeaders(response, null, "failed");
        Assert.False(response.Headers.Contains("X-Shop-Behavior"));
        Assert.False(response.Headers.Contains("X-Shop-Transaction-Id"));
        Assert.Empty(_database.Transactions.Orders("all"));
    }

    [Fact]
    public async Task LegacyCheckoutWithNullPayment_ReachesExisting409Mapping()
    {
        var raw = _database.Database.GetCollection("test_checkout_transactions");
        var checkout = raw.FindById(20);
        checkout["PaymentId"] = BsonValue.Null;
        raw.Update(checkout);
        var response = await Send("/api/checkout/complete", _testToken, "{\"transactionId\":20}");
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Contains("checkout_recreate_required", await response.Content.ReadAsStringAsync());
        Assert.Equal("rejected", response.Headers.GetValues("X-Shop-Outcome").Single());
        Assert.Equal("test", response.Headers.GetValues("X-Shop-Behavior").Single());
        Assert.False(response.Headers.Contains("X-Shop-Skipped-Effects"));
        Assert.NotNull(raw.FindById(20));
        Assert.Empty(_database.Transactions.Orders("all"));
    }

    [Fact]
    public async Task Utf16Json_UsesMvcEncodingAndTransactionBinding()
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/context-probe/complete");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _testToken);
        request.Content = new StringContent("{\"transactionId\":20}", Encoding.Unicode, "application/json");
        var response = await _client.SendAsync(request);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var probe = await response.Content.ReadFromJsonAsync<ContextProbe>();
        Assert.Equal(20, probe!.BoundRequestId);
        Assert.Equal(20, probe.TransactionId);
    }

    [Fact]
    public async Task CorruptStoredPurpose_ReturnsContextBugWithoutEffects()
    {
        var raw = _database.Database.GetCollection("test_checkout_transactions");
        var checkout = raw.FindById(20);
        checkout["IsTest"] = "unknown";
        raw.Update(checkout);
        var response = await Send("/api/context-probe/complete", _testToken, "{\"transactionId\":20}");
        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.Contains("execution_context_invalid", await response.Content.ReadAsStringAsync());
        AssertHeaders(response, null, "failed");
        Assert.Empty(_database.Transactions.Orders("all"));
    }

    [Fact]
    public async Task ParallelCliOperations_DoNotShareContexts()
    {
        var factory = _app.Services.GetRequiredService<IServiceScopeFactory>();
        await Task.WhenAll(Enumerable.Range(0, 20).Select(async i =>
        {
            var test = i % 2 == 0;
            using var operation = ShopOperationScope.Open(factory, test ? _testToken : _normalToken);
            var id = test ? 20 : 10;
            Assert.Equal(CheckoutContextStatus.Bound, operation.Services.GetRequiredService<CheckoutContextLoader>().Bind(id));
            await Task.Delay(20);
            Assert.Equal(test, operation.Services.GetRequiredService<IIdentityContext>().Current.IsTest);
            Assert.Equal(id, operation.Services.GetRequiredService<ITransactionContext>().Current!.Id);
        }));
    }

    [Fact]
    public async Task ConcurrentAndSubsequentRequests_HaveIndependentContexts()
    {
        await Task.WhenAll(Enumerable.Range(0, 20).Select(async i =>
        {
            var test = i % 2 == 0;
            var id = test ? 20 : 10;
            var response = await Send("/api/context-probe/complete", test ? _testToken : _normalToken, $"{{\"transactionId\":{id}}}");
            var probe = await response.Content.ReadFromJsonAsync<ContextProbe>();
            Assert.Equal(id, probe!.TransactionId);
            Assert.Equal(test, probe.IdentityIsTest);
            Assert.Equal(test, probe.TransactionIsTest);
            AssertHeaders(response, test ? "test" : "normal", "succeeded", id, test);
        }));
        var next = await (await Send("/api/context-probe/20", _normalToken)).Content.ReadFromJsonAsync<ContextProbe>();
        Assert.Null(next!.TransactionId);
        Assert.False(next.IdentityIsTest);
    }

    [Fact]
    public void CliScopes_AreIndependentAndCheckoutIsScoped()
    {
        var factory = _app.Services.GetRequiredService<IServiceScopeFactory>();
        using var outer = ShopOperationScope.Open(factory, _testToken);
        var identity = outer.Services.GetRequiredService<IIdentityContext>();
        var transaction = outer.Services.GetRequiredService<ITransactionContext>();
        Assert.Same(identity, outer.Services.GetRequiredService<IdentityContext>());
        Assert.Same(transaction, outer.Services.GetRequiredService<TransactionContext>());
        Assert.Equal(CheckoutContextStatus.Bound, outer.Services.GetRequiredService<CheckoutContextLoader>().Bind(20));
        var checkout = outer.Services.GetRequiredService<CheckoutService>();
        Assert.Same(checkout, outer.Services.GetRequiredService<CheckoutService>());
        using (var inner = ShopOperationScope.Open(factory, _normalToken))
        {
            Assert.Null(inner.Services.GetRequiredService<ITransactionContext>().Current);
            Assert.NotSame(checkout, inner.Services.GetRequiredService<CheckoutService>());
            Assert.False(inner.Services.GetRequiredService<IIdentityContext>().Current.IsTest);
        }
        Assert.True(identity.Current.IsTest);
        Assert.Equal(20, transaction.Current!.Id);
        Assert.Throws<UnauthorizedAccessException>(() => ShopOperationScope.Open(factory, "unknown"));
        using var anonymous = ShopOperationScope.Open(factory, null);
        Assert.Equal(IdentityKind.Anonymous, anonymous.Services.GetRequiredService<IIdentityContext>().Current.Kind);
        Assert.Null(anonymous.Services.GetRequiredService<ITransactionContext>().Current);
        Assert.Throws<ExecutionContextInvalidException>(() => anonymous.Services.GetRequiredService<CheckoutContextLoader>().Bind(20));
        Assert.Throws<InvalidOperationException>(() => _app.Services.GetRequiredService<CheckoutService>());
    }

    [Fact]
    public async Task TestMember_CartAddAndDeletePersistWithoutProtectedChangesOrSkips()
    {
        _database.Products.Insert(new Product { Id = "cart-product", Name = "商品", Price = 100m, SkuId = "cart-sku" });
        _database.Skus.Insert(new SkuRecord { SkuId = "cart-sku", SpecificationSummary = "黑色／128GB" });
        _database.InventoryRecords.Insert(new InventoryRecord { SkuId = "cart-sku", AvailableQuantity = 10 });
        var before = ProtectedDocuments();
        var created = await Send("/api/carts/create", _testToken, "{}");
        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        var cart = (await created.Content.ReadFromJsonAsync<Cart>())!;
        var added = await Send($"/api/carts/{cart.Id}/items", _testToken, "{\"productId\":\"cart-product\",\"qty\":2}");
        Assert.Equal(HttpStatusCode.Created, added.StatusCode);
        var read = await Send($"/api/carts/{cart.Id}", _testToken);
        var line = Assert.Single((await read.Content.ReadFromJsonAsync<Cart>())!.LineItems);
        Assert.Equal(2, line.Quantity);
        Assert.Equal(2, Assert.Single(_database.Carts.FindById(cart.Id).LineItems).Quantity);
        using var deleteRequest = new HttpRequestMessage(HttpMethod.Delete, $"/api/carts/{cart.Id}/lines/{line.LineId}");
        deleteRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _testToken);
        var deleted = await _client.SendAsync(deleteRequest);
        Assert.Equal(HttpStatusCode.OK, deleted.StatusCode);
        var reread = await Send($"/api/carts/{cart.Id}", _testToken);
        Assert.Empty((await reread.Content.ReadFromJsonAsync<Cart>())!.LineItems);
        Assert.Empty(_database.Carts.FindById(cart.Id).LineItems);
        var raw = _database.Database.GetCollection("carts").FindById(cart.Id);
        Assert.False(raw.ContainsKey("IsTest")); Assert.False(raw.ContainsKey("MemberId"));
        Assert.Equal(before, ProtectedDocuments());
        foreach (var response in new[] { created, added, read, deleted, reread })
        {
            Assert.False(response.Headers.Contains("X-Test-Context-Id"));
            Assert.Equal("0", response.Headers.GetValues("X-Test-Skipped-Count").Single());
        }
    }

    [Fact]
    public async Task TwoTestMembers_CannotCompleteEachOthersCheckout_AndNoEffectsOccur()
    {
        var members = _app.Services.GetRequiredService<MemberService>();
        var other = new Member { Name = "other-test", IsTest = true };
        _database.Members.Insert(other);
        var token = members.IssueToken(other.Id, DateTime.MaxValue).Token;
        _database.Products.Insert(new Product { Id = "buyer-product", Price = 100m, SkuId = "buyer-sku" });
        _database.Skus.Insert(new SkuRecord { SkuId = "buyer-sku" });
        _database.InventoryRecords.Insert(new InventoryRecord { SkuId = "buyer-sku", AvailableQuantity = 10 });
        var cart = new Cart(); cart.AddProducts("buyer-product", 2, DateTime.UtcNow); _database.Carts.Insert(cart);
        var created = await Send("/api/checkout/create", _testToken,
            System.Text.Json.JsonSerializer.Serialize(new { cartId = cart.Id, paymentId = 0 }));
        Assert.Equal(HttpStatusCode.OK, created.StatusCode);
        var checkoutId = (await created.Content.ReadFromJsonAsync<CheckoutController.CheckoutCreateResponse>())!.TransactionId;
        var checkoutBefore = _database.Database.GetCollection("test_checkout_transactions").FindById(checkoutId).ToString();
        var cartBefore = _database.Database.GetCollection("carts").FindById(cart.Id).ToString();
        var protectedBefore = ProtectedDocuments();
        var response = await Send("/api/checkout/complete", token, $"{{\"transactionId\":{checkoutId}}}");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        AssertHeaders(response, "test", "rejected");
        Assert.False(response.Headers.Contains("X-Test-Context-Id"));
        Assert.Equal("0", response.Headers.GetValues("X-Test-Skipped-Count").Single());
        Assert.Empty(_database.Transactions.Orders("all")); Assert.Empty(_database.Transactions.Checkouts("all", true));
        Assert.Equal(checkoutBefore, _database.Database.GetCollection("test_checkout_transactions").FindById(checkoutId).ToString());
        Assert.Equal(cartBefore, _database.Database.GetCollection("carts").FindById(cart.Id).ToString());
        Assert.Equal(protectedBefore, ProtectedDocuments());
    }

    [Fact]
    public async Task MaintenanceScopes_KeepPurposeAndEffectsSeparateDuringParallelCalls()
    {
        _database.Products.Insert(new Product { Id = "maintenance", Price = 100m });
        var factory = _app.Services.GetRequiredService<IServiceScopeFactory>();
        var results = await Task.WhenAll(new[] { _normalToken, _testToken }.Select(token => Task.Run(() =>
        {
            using var operation = ShopOperationScope.Open(factory, token);
            var effects = operation.Services.GetRequiredService<OperationEffects>();
            var service = operation.Services.GetRequiredService<ProductMaintenanceService>();
            Action update = () => service.UpdatePrice("maintenance", token == _testToken ? 80m : 90m, identity =>
                Assert.Equal(token == _testToken, identity.IsTest));
            if (token == _testToken) Assert.Throws<TestIdentityForbiddenException>(update);
            else update();
            Assert.Null(operation.Services.GetRequiredService<ITransactionContext>().Current);
            return effects;
        })));
        Assert.NotSame(results[0], results[1]);
        Assert.Empty(results[0].Skipped); Assert.Empty(results[1].Skipped);
        Assert.Equal(90m, _database.Products.FindById("maintenance").Price);
        using var next = ShopOperationScope.Open(factory, _testToken);
        Assert.Empty(next.Services.GetRequiredService<OperationEffects>().Skipped);
        Assert.Throws<InvalidOperationException>(() => _app.Services.GetRequiredService<ProductMaintenanceService>());
    }

    private string ProtectedDocuments() => string.Join("\n", new[] { "products", "skus", "inventory_records" }
        .SelectMany(name => _database.Database.GetCollection(name).FindAll()).Select(x => x.ToString()));

    private Task<HttpResponseMessage> Send(string path, string token, string? json = null)
    {
        var request = new HttpRequestMessage(json == null ? HttpMethod.Get : HttpMethod.Post, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        if (json != null) request.Content = new StringContent(json, Encoding.UTF8, "application/json");
        return _client.SendAsync(request);
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _app.DisposeAsync();
        _database.Dispose();
        File.Delete(_path);
    }
}

public sealed record ContextProbe(int? MemberId, bool IdentityIsTest, int? TransactionId, bool? TransactionIsTest, int? BoundRequestId, bool SameInstances);

[ApiController]
[Route("api/context-probe")]
public sealed class ContextProbeController(IIdentityContext identity, ITransactionContext transaction) : ControllerBase
{
    [HttpGet("{id}")]
    public ContextProbe Get(int id) => Snapshot(null);

    [HttpPost("complete")]
    [CheckoutTransaction]
    [RequestSizeLimit(64 * 1024)]
    public async Task<ContextProbe> Complete(CheckoutController.CheckoutCompleteRequest request)
    {
        await Task.Delay(20);
        return Snapshot(request.TransactionId);
    }

    private ContextProbe Snapshot(int? requestId) => new(identity.Current.MemberId, identity.Current.IsTest,
        transaction.Current?.Id, transaction.Current?.IsTest, requestId,
        ReferenceEquals(identity, HttpContext.Items[typeof(IdentityContext)]) && ReferenceEquals(transaction, HttpContext.Items[typeof(TransactionContext)]));
}
