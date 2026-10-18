using System.Reflection;
using LiteDB;
using AndrewDemo.NetConf2023.Abstract.Contexts;
using AndrewDemo.NetConf2023.Abstract.Operations;
using AndrewDemo.NetConf2023.Abstract.Orders;
using AndrewDemo.NetConf2023.Abstract.Shops;
using AndrewDemo.NetConf2023.Core.AdminQueries;
using AndrewDemo.NetConf2023.Core.Checkouts;
using AndrewDemo.NetConf2023.Core.Contexts;
using AndrewDemo.NetConf2023.Core.Discounts;
using AndrewDemo.NetConf2023.Core.Effects;
using AndrewDemo.NetConf2023.Core.Products;
using Microsoft.Extensions.Logging;

namespace AndrewDemo.NetConf2023.Core.Tests;

public sealed class ProtectedWriteTests : ShopDatabaseTestBase
{
    private readonly IdentityContext _identity = new();
    private readonly TransactionContext _transaction = new();
    private readonly OperationEffects _effects = new();
    private readonly CaptureLogger _logger = new();
    private ProtectedWriteExecutor Writes => new(_identity, _transaction, _effects, _logger);
    private ProductMaintenanceService Maintenance(IShopDatabaseContext? database = null) => new(database ?? Context, Writes, _effects);
    private static void Allow(IdentitySnapshot identity) { }
    private string Snapshot(string collection) => string.Join("\n", Context.Database.GetCollection(collection).FindAll().Select(x => x.ToString()));

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public void ExplicitSource_DoesNotOrIdentityAndTransaction(bool identityTest, bool transactionTest)
    {
        _identity.Initialize(IdentitySnapshot.ForMember(1, identityTest));
        _transaction.Bind(new TransactionSnapshot(TransactionKind.Checkout, 12, transactionTest));
        var validated = 0; var written = 0;
        Writes.ForIdentity("product.update", "product", "p", 80m, _ => validated++, () => written++);
        Writes.ForCheckout(12, "inventory.deduct", "inventory", "s", 2, () => written++);
        Assert.Equal(1, validated);
        Assert.Equal((identityTest ? 0 : 1) + (transactionTest ? 0 : 1), written);
        Assert.Equal((identityTest ? 1 : 0) + (transactionTest ? 1 : 0), _effects.Skipped.Count);
    }

    [Theory]
    [InlineData("anonymous")]
    [InlineData("invalid-member")]
    [InlineData("missing")]
    [InlineData("wrong-id")]
    [InlineData("wrong-kind")]
    public void MissingOrMismatchedContext_StopsBeforeWrites(string kind)
    {
        if (kind == "invalid-member") _identity.Initialize(IdentitySnapshot.ForMember(0, false));
        if (kind == "wrong-id") _transaction.Bind(new TransactionSnapshot(TransactionKind.Checkout, 9, false));
        if (kind == "wrong-kind") _transaction.Bind(new TransactionSnapshot(TransactionKind.Order, 12, false));
        var invoked = false;
        Assert.Throws<ExecutionContextInvalidException>(() =>
        {
            if (kind is "anonymous" or "invalid-member")
                Writes.ForIdentity("product.update", "product", "p", 80m, _ => invoked = true, () => invoked = true);
            else Writes.ForCheckout(12, "inventory.deduct", "inventory", "s", 2, () => invoked = true);
        });
        Assert.False(invoked);
        Assert.Empty(_effects.Skipped);
        Assert.Contains(_logger.Entries, x => x.Level == LogLevel.Error);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Maintenance_ValidatesThenWritesOrSkips_AndPreservesOtherFields(bool isTest)
    {
        _identity.Initialize(IdentitySnapshot.ForMember(1, isTest));
        var (productId, skuId) = TestDataFactory.CreateStockTrackedProduct(Context, 100, 10);
        var sku = Context.Skus.FindById(skuId); sku.SpecificationSummary = "黑色／128GB"; Context.Skus.Update(sku);
        var products = Snapshot("products"); var skus = Snapshot("skus"); var inventory = Snapshot("inventory_records");
        // test 若先寫後還原，也會因觸及原始寫入入口而失敗。
        var database = Intercept(Context, (method, _) =>
        {
            if (isTest && method.Name == "get_Database") throw new InvalidOperationException("test 不可進入持久化寫入");
            return (false, null);
        });
        var service = Maintenance(database);
        var authorizations = 0;
        void Authorize(IdentitySnapshot identity) { Assert.Equal(isTest, identity.IsTest); authorizations++; }
        service.UpdatePrice(productId, 80, Authorize);
        var priceEffects = _effects.Skipped.ToArray();
        service.UpdateSpecification(skuId, "白色／256GB", Authorize);
        Assert.Equal(2, authorizations);
        Assert.Equal(isTest ? 100m : 80m, Context.Products.FindById(productId).Price);
        Assert.Equal(isTest ? "黑色／128GB" : "白色／256GB", Context.Skus.FindById(skuId).SpecificationSummary);
        Assert.Equal(skuId, Context.Products.FindById(productId).SkuId);
        Assert.Equal(inventory, Snapshot("inventory_records"));
        Assert.Null(_transaction.Current);
        if (isTest)
        {
            Assert.Equal(products, Snapshot("products")); Assert.Equal(skus, Snapshot("skus"));
            Assert.Equal("product.update", Assert.Single(priceEffects).EffectCode);
            Assert.Equal("sku.update", Assert.Single(_effects.Skipped).EffectCode);
            Assert.Contains(_logger.Entries, x => Equals(x.Fields["RequestedChange"], 80m) && Equals(x.Fields["TargetId"], productId));
            Assert.Contains(_logger.Entries, x => Equals(x.Fields["RequestedChange"], "白色／256GB") && Equals(x.Fields["TargetId"], skuId));
        }
        else { Assert.Empty(priceEffects); Assert.Empty(_effects.Skipped); Assert.Empty(_logger.Entries); }
        // 後續正式交易仍依實際保存的售價與 SKU 計價。
        var member = new Member { Name = "buyer" }; Context.Members.Insert(member);
        var cart = new Cart(); cart.AddProducts(productId, 2, FixedUtcNow); Context.Carts.Insert(cart);
        var checkout = Checkout();
        var id = checkout.Create(new() { CartId = cart.Id, RequestMember = member, PaymentId = 0 }).TransactionId;
        var result = await checkout.CompleteAsync(new() { TransactionId = id, RequestMember = member });
        Assert.Equal(isTest ? 200m : 160m, result.OrderDetail!.TotalPrice);
        Assert.Equal(8, Context.InventoryRecords.FindById(skuId).AvailableQuantity);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Maintenance_InvalidOrUnauthorizedRequests_DoNotWriteOrRecordSkips(bool isTest)
    {
        _identity.Initialize(IdentitySnapshot.ForMember(1, isTest));
        var (productId, skuId) = TestDataFactory.CreateStockTrackedProduct(Context, 100, 10);
        var before = Snapshot("products") + Snapshot("skus") + Snapshot("inventory_records");
        var service = Maintenance();
        Assert.Throws<UnauthorizedAccessException>(() => service.UpdatePrice(productId, 80, _ => throw new UnauthorizedAccessException()));
        Assert.Throws<UnauthorizedAccessException>(() => service.UpdateSpecification(skuId, "白色", _ => throw new UnauthorizedAccessException()));
        Assert.Throws<ArgumentOutOfRangeException>(() => service.UpdatePrice(productId, -1, Allow));
        Assert.Throws<ArgumentException>(() => service.UpdateSpecification(skuId, " ", Allow));
        Assert.Throws<KeyNotFoundException>(() => service.UpdatePrice("missing", 80, Allow));
        Assert.Throws<KeyNotFoundException>(() => service.UpdateSpecification("missing", "白色", Allow));
        Assert.Empty(_effects.Skipped); Assert.Empty(_logger.Entries);
        Assert.Equal(before, Snapshot("products") + Snapshot("skus") + Snapshot("inventory_records"));
    }

    [Fact]
    public void NextOperation_ClearsCollectorWithoutMutatingPriorResult()
    {
        _identity.Initialize(IdentitySnapshot.ForMember(1, true));
        var (productId, skuId) = TestDataFactory.CreateStockTrackedProduct(Context, 100, 10);
        var service = Maintenance();
        service.UpdatePrice(productId, 80, Allow);
        var snapshot = new OperationResult(true, OperationOutcome.Succeeded, _effects.Skipped);
        Assert.Throws<KeyNotFoundException>(() => service.UpdateSpecification("missing", "白色", Allow));
        Assert.Empty(_effects.Skipped);
        Assert.Single(snapshot.SkippedEffects);
    }

    [Fact]
    public void RealWriteFailure_IsNotReportedAsSkipped()
    {
        _identity.Initialize(IdentitySnapshot.ForMember(1, false));
        Assert.Throws<IOException>(() => Writes.ForIdentity("product.update", "product", "p", 80m, Allow, () => throw new IOException()));
        Assert.Empty(_effects.Skipped); Assert.Empty(_logger.Entries);
    }

    [Fact]
    public async Task TwoMembers_ConcurrentFormalAndTestCheckout_OnlyFormalAffectsStockAndTotals()
    {
        var (productId, skuId) = TestDataFactory.CreateStockTrackedProduct(Context, 100, 10);
        var normal = new Member { Name = "normal" }; var test = new Member { Name = "test", IsTest = true };
        Context.Members.Insert(normal); Context.Members.Insert(test);
        var normalEffects = new OperationEffects(); var testEffects = new OperationEffects();
        var normalTransaction = new TransactionContext(); var testTransaction = new TransactionContext();
        var normalService = Checkout(normalEffects, normalTransaction); var testService = Checkout(testEffects, testTransaction);
        int Create(Member member, int qty, CheckoutService checkout)
        {
            var cart = new Cart(); cart.AddProducts(productId, qty, FixedUtcNow); Context.Carts.Insert(cart);
            return checkout.Create(new() { CartId = cart.Id, RequestMember = member, PaymentId = 0 }).TransactionId;
        }
        var normalId = Create(normal, 3, normalService); var testId = Create(test, 2, testService);
        using var barrier = new Barrier(2);
        Task<CheckoutCompleteResult> Run(CheckoutService service, Member member, int id) => Task.Run(async () =>
        {
            if (!barrier.SignalAndWait(TimeSpan.FromSeconds(10))) throw new TimeoutException();
            return await service.CompleteAsync(new() { RequestMember = member, TransactionId = id });
        });
        var results = await Task.WhenAll(Run(normalService, normal, normalId), Run(testService, test, testId));
        Assert.All(results, x => Assert.Equal(CheckoutCompleteStatus.Succeeded, x.Status));
        Assert.Equal(300m, results[0].OrderDetail!.TotalPrice); Assert.False(results[0].OrderDetail!.IsTest);
        Assert.Equal(200m, results[1].OrderDetail!.TotalPrice); Assert.True(results[1].OrderDetail!.IsTest);
        Assert.Equal(7, Context.InventoryRecords.FindById(skuId).AvailableQuantity);
        Assert.Equal(normalId, normalTransaction.Current!.Id); Assert.False(normalTransaction.Current.IsTest);
        Assert.Equal(testId, testTransaction.Current!.Id); Assert.True(testTransaction.Current.IsTest);
        Assert.False(Context.ArchivedCheckoutTransactions.FindById(normalId).IsTest);
        Assert.True(Context.ArchivedCheckoutTransactions.FindById(testId).IsTest);
        Assert.Empty(normalEffects.Skipped);
        Assert.Equal(skuId, Assert.Single(testEffects.Skipped).TargetId);
        var report = new AdminQueryService(Context, FixedTimeProvider).GetOrders(new AdminQueryOptions());
        Assert.Equal(1, report.Page.TotalCount); Assert.Equal(300m, report.TotalAmount);
        Assert.Contains(_logger.Entries, x => Equals(x.Fields.GetValueOrDefault("TransactionId"), testId)
            && Equals(x.Fields.GetValueOrDefault("EffectCode"), "inventory.deduct"));
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task TestCheckout_ValidatesEverySkuBeforeSkipping_AndNeverCallsInventoryWrite(bool insufficient)
    {
        var (p1, s1) = TestDataFactory.CreateStockTrackedProduct(Context, 100, 10);
        var (p2, s2) = TestDataFactory.CreateStockTrackedProduct(Context, 100, insufficient ? 1 : 10);
        var member = new Member { Name = "test", IsTest = true }; Context.Members.Insert(member);
        var cart = new Cart(); cart.AddProducts(p1, 2, FixedUtcNow); cart.AddProducts(p2, 2, FixedUtcNow); Context.Carts.Insert(cart);
        var inventoryBefore = Snapshot("inventory_records");
        var productBefore = Snapshot("products"); var skuBefore = Snapshot("skus");
        var inventory = DispatchProxy.Create<ILiteCollection<InventoryRecord>, MemberServiceTests.CallInterceptor>();
        var interceptor = (MemberServiceTests.CallInterceptor)inventory;
        interceptor.Target = Context.InventoryRecords;
        interceptor.Before = (method, _) =>
        {
            if (method.Name.StartsWith("Update") || method.Name.StartsWith("Insert") || method.Name.StartsWith("Delete") || method.Name.StartsWith("Upsert"))
                throw new InvalidOperationException("test 不可先扣減再還原庫存");
            return (false, null);
        };
        var database = Intercept(Context, (method, _) => method.Name == "get_InventoryRecords" ? (true, inventory) : (false, null));
        var service = new CheckoutService(database, new DiscountEngine([]), new NullDispatcher(),
            new DefaultProductService(database), new ShopManifest { ShopId = "test" }, FixedTimeProvider,
            effects: _effects, logger: _logger);
        var id = service.Create(new() { CartId = cart.Id, RequestMember = member, PaymentId = 0 }).TransactionId;
        var result = await service.CompleteAsync(new() { TransactionId = id, RequestMember = member });
        Assert.Equal(inventoryBefore, Snapshot("inventory_records"));
        Assert.Equal(productBefore, Snapshot("products")); Assert.Equal(skuBefore, Snapshot("skus"));
        if (insufficient)
        {
            Assert.Equal(CheckoutCompleteStatus.InventoryInsufficient, result.Status);
            Assert.Empty(_effects.Skipped); Assert.Empty(Context.Orders.FindAll());
            Assert.NotNull(Context.CheckoutTransactions.FindById(id));
            Assert.Empty(Context.ArchivedCheckoutTransactions.FindAll());
            Assert.DoesNotContain(_logger.Entries, x => x.Fields.ContainsKey("EffectCode"));
        }
        else
        {
            Assert.Equal(CheckoutCompleteStatus.Succeeded, result.Status);
            Assert.Equal(new[] { s1, s2 }.Order(), _effects.Skipped.Select(x => x.TargetId).Order());
            Assert.All(_effects.Skipped, x => { Assert.Equal("inventory.deduct", x.EffectCode); Assert.Equal("test_behavior", x.ReasonCode); });
            Assert.Equal(2, _logger.Entries.Count(x => Equals(x.Fields.GetValueOrDefault("RequestedChange"), 2)
                && Equals(x.Fields.GetValueOrDefault("TransactionId"), id)));
        }
    }

    [Fact]
    public async Task TwoTestMembers_BuyerMismatchPreservesDataAndDoesNotDispatchOrSkip()
    {
        var (productId, _) = TestDataFactory.CreateStockTrackedProduct(Context, 100, 10);
        var buyer = new Member { Name = "buyer", IsTest = true };
        var other = new Member { Name = "other", IsTest = true };
        Context.Members.Insert(buyer); Context.Members.Insert(other);
        var cart = new Cart(); cart.AddProducts(productId, 2, FixedUtcNow); Context.Carts.Insert(cart);
        var dispatcher = new NullDispatcher();
        var service = new CheckoutService(Context, new DiscountEngine([]), dispatcher,
            new DefaultProductService(Context), new ShopManifest { ShopId = "test" }, FixedTimeProvider,
            effects: _effects, logger: _logger);
        var id = service.Create(new() { CartId = cart.Id, RequestMember = buyer, PaymentId = 0 }).TransactionId;
        string Data() => string.Join("\n", new[] { "carts", "products", "skus", "inventory_records", "checkout_transactions" }.Select(Snapshot));
        var before = Data();
        var result = await service.CompleteAsync(new() { TransactionId = id, RequestMember = other });
        Assert.Equal(CheckoutCompleteStatus.BuyerMismatch, result.Status);
        Assert.Equal(before, Data());
        Assert.Empty(Context.Orders.FindAll()); Assert.Empty(Context.ArchivedCheckoutTransactions.FindAll());
        Assert.Equal(0, dispatcher.CompletedCount); Assert.Empty(_effects.Skipped);
        Assert.Contains(_logger.Entries, x => Equals(x.Fields.GetValueOrDefault("Status"), CheckoutCompleteStatus.BuyerMismatch));
        Assert.DoesNotContain(_logger.Entries, x => x.Fields.ContainsKey("EffectCode"));
    }

    [Fact]
    public void LegacyFactory_RejectsProtectedCreationWithoutExecutionBoundary()
    {
#pragma warning disable CS0618 // 明確驗證舊靜態入口不可繞過用途判斷。
        Assert.Throws<InvalidOperationException>(() => ShopDatabase.Create(new AndrewDemo.NetConf2023.Abstract.Products.Product()));
        Assert.Throws<InvalidOperationException>(() => ShopDatabase.Create(new SkuRecord()));
        Assert.Throws<InvalidOperationException>(() => ShopDatabase.Create(new InventoryRecord()));
#pragma warning restore CS0618
    }

    private CheckoutService Checkout(OperationEffects? effects = null, TransactionContext? transaction = null) => new(Context,
        new DiscountEngine([]), new NullDispatcher(), new DefaultProductService(Context), new ShopManifest { ShopId = "test" },
        FixedTimeProvider, transactionContext: transaction, effects: effects, logger: _logger);

    private static IShopDatabaseContext Intercept(IShopDatabaseContext target,
        Func<MethodInfo, object?[]?, (bool Handled, object? Result)> before)
    {
        var proxy = DispatchProxy.Create<IShopDatabaseContext, MemberServiceTests.CallInterceptor>();
        ((MemberServiceTests.CallInterceptor)proxy).Target = target;
        ((MemberServiceTests.CallInterceptor)proxy).Before = before;
        return proxy;
    }
    private sealed class NullDispatcher : IOrderEventDispatcher
    {
        public int CompletedCount { get; private set; }
        public void Dispatch(OrderCompletedEvent orderEvent) => CompletedCount++;
        public void Dispatch(OrderCancelledEvent orderEvent) { }
    }
    private sealed class CaptureLogger : ILogger<CheckoutService>
    {
        public readonly System.Collections.Concurrent.ConcurrentBag<(LogLevel Level, Dictionary<string, object?> Fields)> Entries = new();
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel level) => true;
        public void Log<TState>(LogLevel level, EventId id, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => Entries.Add((level, ((IEnumerable<KeyValuePair<string, object?>>)(object)state!).ToDictionary(x => x.Key, x => x.Value)));
    }
}
