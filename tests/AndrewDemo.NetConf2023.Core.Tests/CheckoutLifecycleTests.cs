using System.Reflection;
using AndrewDemo.NetConf2023.Abstract.Carts;
using AndrewDemo.NetConf2023.Abstract.Contexts;
using AndrewDemo.NetConf2023.Abstract.Discounts;
using AndrewDemo.NetConf2023.Abstract.Orders;
using AndrewDemo.NetConf2023.Abstract.Shops;
using AndrewDemo.NetConf2023.Core.Checkouts;
using AndrewDemo.NetConf2023.Core.Contexts;
using AndrewDemo.NetConf2023.Core.Discounts;
using AndrewDemo.NetConf2023.Core.Products;
using AndrewDemo.NetConf2023.Core.Testing;
using LiteDB;
using Microsoft.Extensions.Logging;

namespace AndrewDemo.NetConf2023.Core.Tests;

public sealed class CheckoutLifecycleTests : ShopDatabaseTestBase
{
    private readonly RecordingDispatcher _events = new();
    private readonly QuoteRule _rule = new();
    private readonly RecordingLogger _logger = new();

    private CheckoutService Service(IShopDatabaseContext? database = null, TransactionContext? transaction = null,
        CheckoutEffects? effects = null, TestAllowlist? allowlist = null) => new(database ?? Context,
            new DiscountEngine([_rule]), _events, new DefaultProductService(database ?? Context),
            new ShopManifest { ShopId = "test" }, FixedTimeProvider, allowlist, transaction, effects, _logger);

    private (Member Member, Cart Cart, string ProductId, string SkuId) Seed(bool test = false, int stock = 20)
    {
        var member = new Member { Name = Guid.NewGuid().ToString(), IsTest = test };
        Context.Members.Insert(member);
        var (productId, skuId) = TestDataFactory.CreateStockTrackedProduct(Context, 100m, stock);
        var cart = new Cart();
        cart.AddProducts(productId, 2, FixedUtcNow);
        Context.Carts.Insert(cart);
        return (member, cart, productId, skuId);
    }

    private static CheckoutCreateCommand CreateCommand(Member member, Cart cart, int payment = 0) =>
        new() { CartId = cart.Id, RequestMember = member, PaymentId = payment };
    private static CheckoutCompleteCommand CompleteCommand(Member member, int id) =>
        new() { TransactionId = id, RequestMember = member };
    private BsonDocument Raw(int id, string collection = "checkout_transactions") => Context.Database.GetCollection(collection).FindById(id);

    [Theory]
    [InlineData(false, 0, false)]
    [InlineData(false, 990001, true)]
    [InlineData(true, 0, true)]
    [InlineData(true, 990001, true)]
    public async Task QuoteAndCompletion_FixPurposeAmountsAndContext(bool memberTest, int payment, bool expectedTest)
    {
        var (member, cart, productId, skuId) = Seed(memberTest);
        var context = new TransactionContext();
        var effects = new CheckoutEffects();
        var service = Service(transaction: context, effects: effects);
        var created = service.Create(CreateCommand(member, cart, payment));
        var quote = Context.CheckoutTransactions.FindById(created.TransactionId);
        Assert.Equal(CheckoutState.Created, quote.State);
        Assert.Equal(180m, quote.TotalPrice);
        Assert.Equal(expectedTest, quote.IsTest);
        Assert.Null(quote.ClosedAt);
        Assert.Equal(20, Context.InventoryRecords.FindById(skuId).AvailableQuantity);
        Assert.Equal(expectedTest, context.Current!.IsTest);
        Assert.Equal(TransactionKind.Checkout, context.Current.Kind);
        Assert.Equal(cart.LineItems.Single().LineId, quote.ProductLines.Single().LineId);
        Assert.Equal(quote.ProductLines.Single().LineId, quote.DiscountLines.Single().RelatedLineIds.Single());
        Assert.Empty(effects.Skipped);
        var original = Raw(created.TransactionId);
        // 額外欄位也必須原封保存；此為資料完整性測試的基礎層注入。
        original["ExtensionField"] = "保留";
        Context.Database.GetCollection("checkout_transactions").Update(original);
        Context.Carts.Delete(cart.Id);
        var product = Context.Products.FindById(productId);
        product.Price = 999m;
        product.Name = "完成時顯示名稱";
        product.IsPublished = false; // hidden 仍可透過 ID 購買。
        Context.Products.Update(product);
        _rule.Fail = true; // complete 若重新計價，測試必定失敗。
        var result = await service.CompleteAsync(CompleteCommand(member, created.TransactionId));
        Assert.Equal(CheckoutCompleteStatus.Succeeded, result.Status);
        var order = result.OrderDetail!;
        Assert.Equal(180m, order.TotalPrice);
        Assert.Equal(expectedTest, order.IsTest);
        Assert.Equal(memberTest, order.Buyer.IsTest);
        Assert.Equal(created.TransactionId, order.CheckoutTransactionId);
        Assert.Equal(FixedUtcNow, order.CreatedAt);
        Assert.Equal(FixedUtcNow, result.TransactionCompleteAt);
        Assert.Equal(100m, order.ProductLines.Single().UnitPrice);
        Assert.Equal("完成時顯示名稱", order.ProductLines.Single().ProductName);
        Assert.Equal(expectedTest ? 20 : 18, Context.InventoryRecords.FindById(skuId).AvailableQuantity);
        Assert.Equal(expectedTest ? 1 : 0, effects.Skipped.Count);
        Assert.Equal(created.TransactionId, context.Current!.Id);
        Assert.Equal(TransactionKind.Checkout, context.Current.Kind);
        original["State"] = "Completed";
        original["ClosedAt"] = FixedUtcNow;
        Assert.Equal(original.ToString(), Raw(created.TransactionId, "archived_checkout_transactions").ToString());
        Assert.Null(Raw(created.TransactionId));
        order.ProductLines[0].Quantity = 99;
        Assert.Equal(2, Context.ArchivedCheckoutTransactions.FindById(created.TransactionId).ProductLines.Single().Quantity);
        Assert.Single(_events.Completed);
    }

    [Theory]
    [InlineData("product")]
    [InlineData("sku-link")]
    [InlineData("sku-missing")]
    [InlineData("inventory")]
    [InlineData("inventory-missing")]
    public async Task NecessaryValidation_RejectsWithoutAnyEffects(string change)
    {
        var (member, cart, productId, skuId) = Seed(true);
        var effects = new CheckoutEffects();
        var service = Service(effects: effects);
        var id = service.Create(CreateCommand(member, cart)).TransactionId;
        var before = Raw(id).ToString();
        switch (change)
        {
            case "product": Context.Products.Delete(productId); break;
            case "sku-link":
                var product = Context.Products.FindById(productId); product.SkuId = null; Context.Products.Update(product); break;
            case "sku-missing": Context.Skus.Delete(skuId); break;
            case "inventory":
                var inventory = Context.InventoryRecords.FindById(skuId); inventory.AvailableQuantity = 1; Context.InventoryRecords.Update(inventory); break;
            case "inventory-missing": Context.InventoryRecords.Delete(skuId); break;
        }
        var result = await service.CompleteAsync(CompleteCommand(member, id));
        Assert.NotEqual(CheckoutCompleteStatus.Succeeded, result.Status);
        Assert.Equal(before, Raw(id).ToString());
        Assert.Empty(Context.Orders.FindAll());
        Assert.Empty(Context.ArchivedCheckoutTransactions.FindAll());
        Assert.Empty(_events.Completed);
        Assert.Empty(effects.Skipped);
        Assert.DoesNotContain(_logger.Entries, x => x.Message.Contains("TiP 略過"));
    }

    [Fact]
    public void CreateFailure_LeavesNoPartialQuoteOrTransactionContext()
    {
        var (member, cart, productId, _) = Seed();
        var context = new TransactionContext();
        context.Bind(new TransactionSnapshot(TransactionKind.Order, 123, false));
        var service = Service(transaction: context);
        Context.Products.Delete(productId);
        Assert.Throws<CheckoutRejectedException>(() => service.Create(CreateCommand(member, cart)));
        Assert.Null(context.Current);
        Assert.Empty(Context.CheckoutTransactions.FindAll());
        Context.Products.Insert(new AndrewDemo.NetConf2023.Abstract.Products.Product() { Id = productId, Price = 100m });
        _rule.Fail = true;
        Assert.Throws<InvalidOperationException>(() => service.Create(CreateCommand(member, cart)));
        Assert.Empty(Context.CheckoutTransactions.FindAll());
        Assert.Null(context.Current);
    }

    [Theory]
    [InlineData("State")]
    [InlineData("TotalPrice")]
    [InlineData("ProductLines")]
    [InlineData("DiscountLines")]
    [InlineData("line-price")]
    [InlineData("unknown")]
    public async Task LegacyMissingQuote_RequiresRecreationWithoutMutation(string field)
    {
        var (member, cart, _, skuId) = Seed();
        var service = Service();
        var id = service.Create(CreateCommand(member, cart)).TransactionId;
        var raw = Raw(id);
        if (field == "line-price") raw["ProductLines"][0].AsDocument.Remove("UnitPrice");
        else if (field == "unknown") raw["State"] = "Unknown";
        else raw.Remove(field);
        Context.Database.GetCollection("checkout_transactions").Update(raw);
        var result = await service.CompleteAsync(CompleteCommand(member, id));
        Assert.Equal(CheckoutCompleteStatus.RecreateRequired, result.Status);
        Assert.Equal(raw.ToString(), Raw(id).ToString());
        Assert.Equal(20, Context.InventoryRecords.FindById(skuId).AvailableQuantity);
        Assert.Empty(Context.Orders.FindAll());
    }

    [Fact]
    public async Task CorruptTotal_FailsAndLogsRatherThanRepricing()
    {
        var (member, cart, _, skuId) = Seed();
        var service = Service();
        var id = service.Create(CreateCommand(member, cart)).TransactionId;
        var raw = Raw(id); raw["TotalPrice"] = 1m;
        Context.Database.GetCollection("checkout_transactions").Update(raw);
        await Assert.ThrowsAsync<InvalidOperationException>(() => service.CompleteAsync(CompleteCommand(member, id)));
        Assert.Equal(raw.ToString(), Raw(id).ToString());
        Assert.Equal(20, Context.InventoryRecords.FindById(skuId).AvailableQuantity);
        Assert.Empty(Context.Orders.FindAll());
        Assert.Contains(_logger.Entries, x => x.Level == LogLevel.Error);
    }

    [Theory]
    [InlineData("inventory")]
    [InlineData("order")]
    [InlineData("archive")]
    [InlineData("delete")]
    [InlineData("commit")]
    public async Task FailureAtEveryWrite_RollsBackAndAllowsRetry(string stage)
    {
        var (member, cart, _, skuId) = Seed();
        var id = Service().Create(CreateCommand(member, cart)).TransactionId;
        var before = Raw(id).ToString();
        var orders = AfterFailure(Context.Orders, "Insert", stage == "order");
        var inventory = AfterFailure(Context.InventoryRecords, "Update", stage == "inventory");
        var active = AfterFailure(Context.CheckoutTransactions, "Delete", stage == "delete");
        var archive = AfterFailure(Context.Database.GetCollection("archived_checkout_transactions"), "Insert", stage == "archive");
        var database = Intercept<ILiteDatabase>(Context.Database, (method, args) =>
        {
            if (method.Name == "Commit" && stage == "commit") throw new IOException("提交前故障");
            if (method.Name == "GetCollection" && args![0] as string == "archived_checkout_transactions") return (true, archive);
            return (false, null);
        });
        var context = Intercept<IShopDatabaseContext>(Context, (method, _) => method.Name switch
        {
            "get_Database" => (true, database), "get_Orders" => (true, orders),
            "get_InventoryRecords" => (true, inventory), "get_CheckoutTransactions" => (true, active), _ => (false, null)
        });
        await Assert.ThrowsAsync<IOException>(() => Service(database: context).CompleteAsync(CompleteCommand(member, id)));
        Assert.Equal(before, Raw(id).ToString());
        Assert.Equal(20, Context.InventoryRecords.FindById(skuId).AvailableQuantity);
        Assert.Empty(Context.Orders.FindAll());
        Assert.Empty(Context.ArchivedCheckoutTransactions.FindAll());
        Assert.Empty(_events.Completed);
        Assert.Equal(CheckoutCompleteStatus.Succeeded, (await Service().CompleteAsync(CompleteCommand(member, id))).Status);
        Assert.Single(_events.Completed);
    }

    [Fact]
    public async Task ConcurrentCompletionAndCreate_UseUniqueIdsAndDoNotOverwriteUnrelatedOrders()
    {
        var (member, cart, _, skuId) = Seed();
        Context.Orders.Insert(new Order(70) { Buyer = member, TotalPrice = 777m });
        var id = Service().Create(CreateCommand(member, cart)).TransactionId;
        var results = await Task.WhenAll(Enumerable.Range(0, 12).Select(_ => Service().CompleteAsync(CompleteCommand(member, id))));
        var success = Assert.Single(results, x => x.Status == CheckoutCompleteStatus.Succeeded);
        Assert.All(results.Where(x => x != success), x => Assert.Equal(CheckoutCompleteStatus.TransactionNotFound, x.Status));
        Assert.Equal(71, success.OrderDetail!.Id);
        Assert.Equal(777m, Context.Orders.FindById(70).TotalPrice);
        Assert.Equal(18, Context.InventoryRecords.FindById(skuId).AvailableQuantity);
        Assert.Single(_events.Completed);
        Assert.Single(Context.ArchivedCheckoutTransactions.FindAll());
        var ids = await Task.WhenAll(Enumerable.Range(0, 16).Select(_ => Task.Run(() => Service().Create(CreateCommand(member, cart)).TransactionId)));
        Assert.Equal(16, ids.Distinct().Count());
        Assert.DoesNotContain(id, ids);
    }

    [Fact]
    public async Task FormalAndTestConcurrentCompletion_OnlyFormalDeducts()
    {
        var (member, cart, _, skuId) = Seed();
        var normal = Service().Create(CreateCommand(member, cart)).TransactionId;
        var test = Service().Create(CreateCommand(member, cart, 990001)).TransactionId;
        var results = await Task.WhenAll(Service().CompleteAsync(CompleteCommand(member, normal)), Service().CompleteAsync(CompleteCommand(member, test)));
        Assert.All(results, x => Assert.Equal(CheckoutCompleteStatus.Succeeded, x.Status));
        Assert.False(results[0].OrderDetail!.IsTest);
        Assert.True(results[1].OrderDetail!.IsTest);
        Assert.Equal(18, Context.InventoryRecords.FindById(skuId).AvailableQuantity);
        Assert.Equal(2, Context.ArchivedCheckoutTransactions.Count());
    }

    [Fact]
    public async Task ArchiveAndActiveGuards_RejectMutationAndReactivation()
    {
        var (member, cart, _, _) = Seed();
        var service = Service();
        var id = service.Create(CreateCommand(member, cart)).TransactionId;
        var active = Context.CheckoutTransactions.FindById(id);
        Assert.Throws<InvalidOperationException>(() => Context.CheckoutTransactions.Update(active));
        Assert.Throws<InvalidOperationException>(() => Context.CheckoutTransactions.Upsert(active));
        await service.CompleteAsync(CompleteCommand(member, id));
        var original = Raw(id, "archived_checkout_transactions").ToString();
        var archived = Context.ArchivedCheckoutTransactions.FindById(id);
        archived.State = CheckoutState.Created;
        archived.ClosedAt = null;
        archived.ProductLines.Clear();
        archived.DiscountLines.Clear();
        Assert.Throws<InvalidOperationException>(() => Context.ArchivedCheckoutTransactions.Update(archived));
        Assert.Throws<InvalidOperationException>(() => Context.ArchivedCheckoutTransactions.Upsert(archived));
        Assert.Throws<InvalidOperationException>(() => Context.ArchivedCheckoutTransactions.Delete(id));
        Assert.Throws<InvalidOperationException>(() => Context.ArchivedCheckoutTransactions.DeleteAll());
        Assert.Throws<InvalidOperationException>(() => Context.ArchivedCheckoutTransactions.DeleteMany(x => true));
        Assert.Throws<InvalidOperationException>(() => Context.ArchivedCheckoutTransactions.UpdateMany("{State: 'Created'}", "true"));
        Assert.Throws<InvalidOperationException>(() => Context.CheckoutTransactions.Insert(archived));
        Assert.Throws<InvalidOperationException>(() => Context.CheckoutTransactions.Insert(new CheckoutTransactionRecord()));
        Assert.Throws<LiteException>(() => Context.ArchivedCheckoutTransactions.Insert(archived));
        Assert.Equal(original, Raw(id, "archived_checkout_transactions").ToString());
        Assert.Equal(CheckoutCompleteStatus.TransactionNotFound, (await service.CompleteAsync(CompleteCommand(member, id))).Status);
        Assert.Single(_events.Completed);
    }

    [Fact]
    public async Task FulfillmentFailure_LeavesCompleteArchiveAndOnlyUpdatesOrderStatus()
    {
        var (member, cart, _, _) = Seed();
        var service = Service();
        var id = service.Create(CreateCommand(member, cart)).TransactionId;
        var expected = Raw(id); expected["State"] = "Completed"; expected["ClosedAt"] = FixedUtcNow;
        _events.Fail = true;
        var result = await service.CompleteAsync(CompleteCommand(member, id));
        Assert.Equal(CheckoutCompleteStatus.Succeeded, result.Status);
        Assert.Equal(OrderFulfillmentStatus.Failed, Context.Orders.FindById(result.OrderDetail!.Id).FulfillmentStatus);
        Assert.Equal(expected.ToString(), Raw(id, "archived_checkout_transactions").ToString());
    }

    [Fact]
    public async Task CompletionIgnoresChangedAllowlistAndLegacyMissingPurposeStaysFormal()
    {
        var (member, cart, _, skuId) = Seed();
        var id = Service().Create(CreateCommand(member, cart)).TransactionId;
        var raw = Raw(id); raw.Remove("IsTest");
        Context.Database.GetCollection("checkout_transactions").Update(raw);
        var result = await Service(allowlist: new TestAllowlist([member.Name], [0])).CompleteAsync(CompleteCommand(member, id));
        Assert.False(result.OrderDetail!.IsTest);
        Assert.Equal(18, Context.InventoryRecords.FindById(skuId).AvailableQuantity);
        Assert.False(Raw(id, "archived_checkout_transactions").ContainsKey("IsTest"));
    }

    [Fact]
    public async Task SharedConnections_SerializeCreationAndCompletion()
    {
        var path = Path.Combine(Path.GetTempPath(), $"tip23-shared-{Guid.NewGuid():N}.db");
        try
        {
            using var first = new ShopDatabaseContext(new ShopDatabaseOptions { ConnectionString = $"Filename={path};Connection=Shared" });
            using var second = new ShopDatabaseContext(new ShopDatabaseOptions { ConnectionString = $"Filename={path};Connection=Shared" });
            var member = new Member { Name = "shared" }; first.Members.Insert(member);
            var productId = TestDataFactory.CreateProduct(first, 100m);
            var cart = new Cart(); cart.AddProducts(productId, 1, FixedUtcNow); first.Carts.Insert(cart);
            var ids = await Task.WhenAll(Enumerable.Range(0, 12).Select(i => Task.Run(() =>
                Service(database: i % 2 == 0 ? first : second).Create(CreateCommand(member, cart)).TransactionId)));
            Assert.Equal(12, ids.Distinct().Count());
            var results = await Task.WhenAll(Enumerable.Range(0, 8).Select(i =>
                Service(database: i % 2 == 0 ? first : second).CompleteAsync(CompleteCommand(member, ids[0]))));
            Assert.Single(results, x => x.Status == CheckoutCompleteStatus.Succeeded);
            Assert.Equal(1, first.Orders.Count());
            Assert.Equal(1, first.ArchivedCheckoutTransactions.Count());
            Assert.Single(_events.Completed);
        }
        finally { File.Delete(path); }
    }

    [Fact]
    public void FailedCreateCommit_RollsBackAndDoesNotBindContext()
    {
        var (member, cart, _, _) = Seed(true);
        var transaction = new TransactionContext();
        var database = Intercept<ILiteDatabase>(Context.Database, (method, _) =>
        {
            if (method.Name == "Commit") throw new IOException("建立提交前故障");
            return (false, null);
        });
        var context = Intercept<IShopDatabaseContext>(Context, (method, _) =>
            method.Name == "get_Database" ? (true, database) : (false, null));
        Assert.Throws<IOException>(() => Service(database: context, transaction: transaction).Create(CreateCommand(member, cart)));
        Assert.Empty(Context.CheckoutTransactions.FindAll());
        Assert.Null(transaction.Current);
        var created = Service(transaction: transaction).Create(CreateCommand(member, cart));
        Assert.Equal(created.TransactionId, transaction.Current!.Id);
        Assert.True(Context.CheckoutTransactions.FindById(created.TransactionId).IsTest);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task PersistedPurposes_CannotBeReclassifiedThroughCollectionWrites(bool isTest)
    {
        var (member, cart, _, _) = Seed(isTest);
        var service = Service();
        var id = service.Create(CreateCommand(member, cart)).TransactionId;
        var activeBefore = Raw(id).ToString();
        var altered = new CheckoutTransactionRecord { TransactionId = id, MemberId = member.Id,
            IsTest = !isTest, PaymentId = 123, TotalPrice = 999m };
        Assert.Throws<InvalidOperationException>(() => Context.CheckoutTransactions.Upsert(altered));
        Assert.Equal(activeBefore, Raw(id).ToString());
        var result = await service.CompleteAsync(CompleteCommand(member, id));
        var order = result.OrderDetail!;
        var orderBefore = Context.Database.GetCollection("orders").FindById(order.Id).ToString();
        var replacement = new Order(order.Id) { Buyer = member, IsTest = !isTest, TotalPrice = 1m };
        Assert.Throws<InvalidOperationException>(() => Context.Orders.Update(replacement));
        Assert.Throws<InvalidOperationException>(() => Context.Orders.Upsert(replacement));
        Assert.Throws<InvalidOperationException>(() => Context.Orders.UpdateMany("{IsTest: true}", "true"));
        Assert.Throws<InvalidOperationException>(() => Context.Orders.Delete(order.Id));
        Assert.Equal(orderBefore, Context.Database.GetCollection("orders").FindById(order.Id).ToString());
        var archiveBefore = Raw(id, "archived_checkout_transactions").ToString();
        Assert.Throws<InvalidOperationException>(() => Context.ArchivedCheckoutTransactions.Update(altered));
        Assert.Throws<InvalidOperationException>(() => Context.ArchivedCheckoutTransactions.Upsert(altered));
        Assert.Equal(archiveBefore, Raw(id, "archived_checkout_transactions").ToString());
        Assert.Equal(isTest, Context.Orders.FindById(order.Id).IsTest);
        Assert.Equal(OrderFulfillmentStatus.Succeeded, Context.Orders.FindById(order.Id).FulfillmentStatus);
    }

    [Fact]
    public async Task MultipleLines_PreserveRelationsAndValidateCombinedSkuQuantity()
    {
        var (member, cart, productId, skuId) = Seed(true, stock: 3);
        cart.AddProducts(productId, 2, FixedUtcNow, cart.LineItems[0].LineId);
        Context.Carts.Update(cart);
        var effects = new CheckoutEffects();
        var service = Service(effects: effects);
        var id = service.Create(CreateCommand(member, cart)).TransactionId;
        var quote = Context.CheckoutTransactions.FindById(id);
        Assert.Equal(2, quote.ProductLines.Count);
        Assert.Equal(quote.ProductLines[0].LineId, quote.ProductLines[1].ParentLineId);
        Assert.Equal(2, quote.DiscountLines.Single().RelatedLineIds.Count);
        Assert.Equal(380m, quote.TotalPrice);
        var result = await service.CompleteAsync(CompleteCommand(member, id));
        Assert.Equal(CheckoutCompleteStatus.InventoryInsufficient, result.Status);
        Assert.Equal(3, Context.InventoryRecords.FindById(skuId).AvailableQuantity);
        Assert.Empty(effects.Skipped);
        Assert.Empty(Context.Orders.FindAll());
    }

    [Fact]
    public async Task FailureAfterActualSkip_RetainsEffectEvidenceWithoutDatabaseWrites()
    {
        var (member, cart, _, skuId) = Seed(true);
        var id = Service().Create(CreateCommand(member, cart)).TransactionId;
        var orders = AfterFailure(Context.Orders, "Insert", true);
        var database = Intercept<IShopDatabaseContext>(Context, (method, _) =>
            method.Name == "get_Orders" ? (true, orders) : (false, null));
        var effects = new CheckoutEffects();
        await Assert.ThrowsAsync<IOException>(() => Service(database: database, effects: effects).CompleteAsync(CompleteCommand(member, id)));
        var effect = Assert.Single(effects.Skipped);
        Assert.Equal("inventory.deduct", effect.EffectCode);
        Assert.Equal(skuId, effect.TargetId);
        Assert.Equal(20, Context.InventoryRecords.FindById(skuId).AvailableQuantity);
        Assert.Empty(Context.Orders.FindAll());
        Assert.Empty(Context.ArchivedCheckoutTransactions.FindAll());
        Assert.Equal(CheckoutState.Created, Context.CheckoutTransactions.FindById(id).State);
    }

    private static T AfterFailure<T>(T target, string name, bool enabled) where T : class => Intercept(target, (method, args) =>
    {
        if (enabled && method.Name == name) { method.Invoke(target, args); throw new IOException("寫入後注入故障"); }
        return (false, null);
    });
    private static T Intercept<T>(T target, Func<MethodInfo, object?[]?, (bool Handled, object? Result)> before) where T : class
    {
        var proxy = DispatchProxy.Create<T, MemberServiceTests.CallInterceptor>();
        var interceptor = (MemberServiceTests.CallInterceptor)(object)proxy;
        interceptor.Target = target;
        interceptor.Before = before;
        return proxy;
    }
    private sealed class QuoteRule : IDiscountRule
    {
        public bool Fail;
        public string RuleId => "quote";
        public int Priority => 0;
        public IReadOnlyList<DiscountRecord> Evaluate(CartContext context)
        {
            if (Fail) throw new InvalidOperationException("不得重新計價");
            return [new DiscountRecord { RuleId = RuleId, Name = "折扣", Description = "折抵", Amount = -20m,
                Kind = DiscountRecordKind.Discount, RelatedLineIds = context.LineItems.Select(x => x.LineId).ToList() }];
        }
    }
    private sealed class RecordingDispatcher : IOrderEventDispatcher
    {
        public readonly System.Collections.Concurrent.ConcurrentBag<OrderCompletedEvent> Completed = new();
        public bool Fail;
        public void Dispatch(OrderCompletedEvent orderEvent) { Completed.Add(orderEvent); if (Fail) throw new IOException("履約故障"); }
        public void Dispatch(OrderCancelledEvent orderEvent) { }
    }
    private sealed class RecordingLogger : ILogger<CheckoutService>
    {
        public readonly System.Collections.Concurrent.ConcurrentBag<(LogLevel Level, string Message)> Entries = new();
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => true;
        public void Log<TState>(LogLevel level, EventId id, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => Entries.Add((level, formatter(state, exception)));
    }
}
