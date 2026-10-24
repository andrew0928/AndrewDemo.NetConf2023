using AndrewDemo.NetConf2023.Abstract.Contexts;
using AndrewDemo.NetConf2023.Abstract.Orders;
using AndrewDemo.NetConf2023.Abstract.Shops;
using AndrewDemo.NetConf2023.Core.Checkouts;
using AndrewDemo.NetConf2023.Core.Contexts;
using AndrewDemo.NetConf2023.Core.Discounts;
using AndrewDemo.NetConf2023.Core.Effects;
using AndrewDemo.NetConf2023.Core.Products;
using LiteDB;
using AndrewDemo.NetConf2023.Core.Storage;
using AndrewDemo.NetConf2023.Core.AdminQueries;
using System.Reflection;

namespace AndrewDemo.NetConf2023.Core.Tests;

public sealed class TransactionStorageTests : ShopDatabaseTestBase
{
    [Theory]
    [InlineData(false, 0, false)]
    [InlineData(false, 990001, true)]
    [InlineData(true, 0, true)]
    [InlineData(true, 990001, true)]
    public async Task Lifecycle_UsesOnlyItsPersistedPartition(bool memberTest, int payment, bool expectedTest)
    {
        var member = new Member { Name = "buyer", IsTest = memberTest };
        Context.Members.Insert(member);
        var (product, sku) = TestDataFactory.CreateStockTrackedProduct(Context, 100, 10);
        var cart = new Cart(); cart.AddProducts(product, 2, FixedUtcNow); Context.Carts.Insert(cart);
        var service = new CheckoutService(Context, new DiscountEngine([]), new NoEvents(),
            new DefaultProductService(Context), new ShopManifest { ShopId = "test" }, FixedTimeProvider);
        var created = service.Create(new() { CartId = cart.Id, PaymentId = payment, RequestMember = member });
        var prefix = expectedTest ? "test_" : "";
        var other = expectedTest ? "" : "test_";
        Assert.NotNull(Context.Database.GetCollection(prefix + "checkout_transactions").FindById(created.TransactionId));
        Assert.Empty(Context.Database.GetCollection(other + "checkout_transactions").FindAll());
        var result = await service.CompleteAsync(new() { TransactionId = created.TransactionId, RequestMember = member });
        Assert.Equal(CheckoutCompleteStatus.Succeeded, result.Status);
        Assert.NotNull(Context.Database.GetCollection(prefix + "orders").FindById(result.OrderDetail!.Id));
        Assert.NotNull(Context.Database.GetCollection(prefix + "archived_checkout_transactions").FindById(created.TransactionId));
        Assert.Empty(Context.Database.GetCollection(prefix + "checkout_transactions").FindAll());
        Assert.Empty(Context.Database.GetCollection(other + "orders").FindAll());
        Assert.Empty(Context.Database.GetCollection(other + "archived_checkout_transactions").FindAll());
        Assert.Equal(expectedTest ? 10 : 8, Context.InventoryRecords.FindById(sku).AvailableQuantity);
    }

    [Theory]
    [InlineData(null)]
    [InlineData(false)]
    [InlineData(true)]
    public void TestIdentity_PriceChangeIsRejectedInsteadOfSkipped(bool? boundPurpose)
    {
        var id = TestDataFactory.CreateProduct(Context, 100);
        var identity = new IdentityContext(); identity.Initialize(IdentitySnapshot.ForMember(1, true));
        var effects = new OperationEffects();
        var transaction = new TransactionContext();
        if (boundPurpose is { } purpose) transaction.Bind(new(TransactionKind.Checkout, 1, purpose));
        var writes = new ProtectedWriteExecutor(identity, transaction, effects);
        var maintenance = new ProductMaintenanceService(Context, writes, effects);
        Assert.Throws<TestIdentityForbiddenException>(() => maintenance.UpdatePrice(id, 80, _ => { }));
        Assert.Empty(effects.Skipped);
        Assert.Equal(100, Context.Products.FindById(id).Price);
    }

    [Fact]
    public async Task Identifiers_AreUniqueAcrossAllFourCheckoutPartitions()
    {
        var id = 10;
        foreach (var test in new[] { false, true })
            foreach (var archived in new[] { false, true })
            {
                Context.Database.GetCollection(TransactionCollections.Checkouts(test, archived))
                    .Insert(new BsonDocument { ["_id"] = id, ["IsTest"] = test });
                id += 10;
            }
        var records = Enumerable.Range(0, 20).Select(i => NewCheckout(i % 2 == 0)).ToArray();
        await Task.WhenAll(records.Select(record => Task.Run(() => Context.Transactions.InsertCheckout(record))));
        Assert.Equal(20, records.Select(x => x.TransactionId).Distinct().Count());
        Assert.All(records, x => Assert.InRange(x.TransactionId, 41, 60));
        Assert.Equal(24, Context.Transactions.ReadCheckouts("all").Length);
    }

    [Theory]
    [InlineData("duplicate-order")]
    [InlineData("duplicate-checkout")]
    [InlineData("wrong-partition")]
    [InlineData("missing-test-purpose")]
    [InlineData("null-purpose")]
    [InlineData("invalid-purpose")]
    [InlineData("opposite-link")]
    public void CorruptData_IsRejectedByLocatorAndAdmin(string kind)
    {
        var checkout = new BsonDocument { ["_id"] = 1, ["IsTest"] = true };
        var order = new BsonDocument { ["_id"] = 1, ["IsTest"] = true };
        if (kind == "missing-test-purpose") checkout.Remove("IsTest");
        if (kind == "null-purpose") checkout["IsTest"] = BsonValue.Null;
        if (kind == "invalid-purpose") checkout["IsTest"] = "true";
        if (kind == "wrong-partition") checkout["IsTest"] = false;
        Context.Database.GetCollection("test_checkout_transactions").Insert(checkout);
        if (kind == "duplicate-checkout") Context.Database.GetCollection("checkout_transactions").Insert(new BsonDocument { ["_id"] = 1 });
        if (kind == "duplicate-order")
        {
            Context.Database.GetCollection("orders").Insert(new BsonDocument { ["_id"] = 1 });
            Context.Database.GetCollection("test_orders").Insert(order);
        }
        if (kind == "opposite-link")
        {
            order["IsTest"] = false; order["CheckoutTransactionId"] = 1;
            Context.Database.GetCollection("orders").Insert(order);
        }
        var orders = kind is "duplicate-order" or "opposite-link";
        Assert.Throws<ExecutionContextInvalidException>(() =>
        {
            if (orders) Context.Transactions.FindOrder(1); else Context.Transactions.FindActiveCheckout(1);
        });
        var admin = new AdminQueryService(Context, FixedTimeProvider);
        Assert.Throws<AdminDataInvalidException>(() =>
        {
            if (orders) admin.GetOrders(new() { Purpose = "all" }); else admin.GetCheckouts(new() { Purpose = "all" });
        });
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ReinsertCannotChangePurposeOrOverwrite(bool test)
    {
        var first = NewCheckout(test, 7); Context.Transactions.InsertCheckout(first);
        var replacement = NewCheckout(!test, 7);
        Assert.Throws<ExecutionContextInvalidException>(() => Context.Transactions.InsertCheckout(replacement));
        Context.Transactions.InsertOrder(new Order(12) { IsTest = test });
        Assert.Throws<ExecutionContextInvalidException>(() => Context.Transactions.InsertOrder(new Order(12) { IsTest = !test }));
        Assert.Equal(test, Context.Transactions.Checkout(7)!.IsTest);
        Assert.Equal(test, Context.Transactions.Order(12)!.IsTest);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void SystemCannotEscapeTestTransactionThroughMaintenance(bool clear)
    {
        var product = TestDataFactory.CreateProduct(Context, 100);
        var identity = new IdentityContext(); identity.Initialize(IdentitySnapshot.System);
        var transaction = new TransactionContext(); transaction.Bind(new(TransactionKind.Order, 1, true));
        if (clear) transaction.Clear();
        var effects = new OperationEffects();
        var maintenance = new ProductMaintenanceService(Context, new ProtectedWriteExecutor(identity, transaction, effects), effects);
        Assert.Throws<ExecutionContextInvalidException>(() => maintenance.UpdatePrice(product, 80, _ => { }));
        Assert.Equal(100, Context.Products.FindById(product).Price);
        Assert.Empty(effects.Skipped);
    }

    [Fact]
    public void AdminMergesAllPartitionsBeforePaging_AndDefaultReadsOnlyNormal()
    {
        foreach (var test in new[] { false, true })
        {
            Context.Transactions.InsertOrder(new Order(test ? 20 : 10) { IsTest = test, TotalPrice = test ? 900 : 100,
                Buyer = new Member { Id = 1, IsTest = false } });
            foreach (var archived in new[] { false, true })
                Context.Database.GetCollection(TransactionCollections.Checkouts(test, archived)).Insert(new BsonDocument
                { ["_id"] = (test ? 20 : 10) + (archived ? 1 : 0), ["IsTest"] = test, ["State"] = archived ? "Completed" : "Created" });
        }
        var admin = new AdminQueryService(Context, FixedTimeProvider);
        Assert.Equal(100, admin.GetOrders(new()).TotalAmount);
        Assert.Equal(900, admin.GetOrders(new() { Purpose = "test" }).TotalAmount);
        var all = admin.GetOrders(new() { Purpose = "all", PageSize = 1 });
        Assert.Equal(1000, all.TotalAmount); Assert.Equal(2, all.Page.TotalCount); Assert.Equal(20, Assert.Single(all.Page.Items).Id);
        var checkouts = admin.GetCheckouts(new() { Purpose = "all", PageSize = 2 });
        Assert.Equal(4, checkouts.TotalCount);
        Assert.Equal(new[] { 21, 20 }, checkouts.Items.Select(x => x.TransactionId));
        Assert.Equal(new[] { "archived", "active" }, checkouts.Items.Select(x => x.Storage));
        Assert.Equal(2, admin.GetCheckouts(new()).TotalCount);
    }

    private static CheckoutTransactionRecord NewCheckout(bool test, int id = 0) => new()
    { TransactionId = id, IsTest = test, State = CheckoutState.Created, TotalPrice = 0 };

    [Fact]
    public async Task NestedCheckoutSteps_DoNotBeginAnotherDatabaseTransaction()
    {
        var member = new Member { Name = "owner" }; Context.Members.Insert(member);
        var cart = new Cart(); Context.Carts.Insert(cart);
        var begins = 0;
        var db = DispatchProxy.Create<ILiteDatabase, MemberServiceTests.CallInterceptor>();
        var dbProxy = (MemberServiceTests.CallInterceptor)(object)db;
        dbProxy.Target = Context.Database;
        dbProxy.Before = (method, _) => { if (method.Name == "BeginTrans") begins++; return (false, null); };
        var context = DispatchProxy.Create<IShopDatabaseContext, MemberServiceTests.CallInterceptor>();
        var contextProxy = (MemberServiceTests.CallInterceptor)(object)context;
        contextProxy.Target = Context;
        contextProxy.Before = (method, _) => method.Name == "get_Database" ? (true, db) : (false, null);
        var service = new CheckoutService(context, new DiscountEngine([]), new NoEvents(),
            new DefaultProductService(context), new ShopManifest { ShopId = "test" }, FixedTimeProvider);
        var created = service.Create(new() { CartId = cart.Id, PaymentId = 990001, RequestMember = member });
        Assert.Equal(1, begins);
        var complete = await service.CompleteAsync(new() { TransactionId = created.TransactionId, RequestMember = member });
        Assert.Equal(CheckoutCompleteStatus.Succeeded, complete.Status);
        // 完成交易一筆，提交後履約狀態另開一筆；locator／Order insert／archive 不重入。
        Assert.Equal(3, begins);
        new AdminQueryService(context, FixedTimeProvider).GetCheckouts(new() { Purpose = "all" });
        Assert.Equal(4, begins);
    }

    private sealed class NoEvents : IOrderEventDispatcher
    {
        public void Dispatch(OrderCompletedEvent orderEvent) { }
        public void Dispatch(OrderCancelledEvent orderEvent) { }
    }
}
