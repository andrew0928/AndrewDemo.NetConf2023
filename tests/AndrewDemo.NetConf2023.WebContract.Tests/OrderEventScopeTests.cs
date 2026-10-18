using AndrewDemo.NetConf2023.Core.Operations;
using System.Collections.Concurrent;
using AndrewDemo.NetConf2023.Abstract.Contexts;
using AndrewDemo.NetConf2023.Abstract.Operations;
using AndrewDemo.NetConf2023.Abstract.Orders;
using AndrewDemo.NetConf2023.Abstract.Products;
using AndrewDemo.NetConf2023.Abstract.Shops;
using AndrewDemo.NetConf2023.Core;
using AndrewDemo.NetConf2023.Core.Checkouts;
using AndrewDemo.NetConf2023.Core.Contexts;
using AndrewDemo.NetConf2023.Core.Discounts;
using AndrewDemo.NetConf2023.Core.Effects;
using AndrewDemo.NetConf2023.Core.Orders;
using AndrewDemo.NetConf2023.Core.Products;
using AndrewDemo.NetConf2023.Core.Testing;
using LiteDB;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AndrewDemo.NetConf2023.WebContract.Tests;

/// <summary>使用正式 dispatcher、DI scope 與 LiteDB；商業 adapter 僅為可計數的測試替身。</summary>
public sealed class OrderEventScopeTests : IDisposable
{
    private readonly string _path = Path.Combine(Path.GetTempPath(), $"tip25-{Guid.NewGuid():N}.db");
    private readonly ShopDatabaseContext _database;
    private readonly ServiceProvider _provider;
    private readonly State _state = new();
    private readonly CaptureLogs _logs = new();
    private IOrderEventDispatcher Dispatcher => _provider.GetRequiredService<IOrderEventDispatcher>();

    public OrderEventScopeTests()
    {
        _database = new ShopDatabaseContext(new() { ConnectionString = $"Filename={_path};Connection=Direct" });
        var services = Services();
        services.AddScoped<IOrderEventHandler, Handler>();
        _provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
    }

    private ServiceCollection Services()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IShopDatabaseContext>(_database);
        services.AddSingleton(_state);
        services.AddLogging(x => x.AddProvider(_logs));
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton(new ShopManifest { ShopId = "test" });
        services.AddSingleton(new TestAllowlist());
        services.AddSingleton<MemberService>();
        services.AddSingleton(new DiscountEngine([]));
        services.AddSingleton<IProductService, DefaultProductService>();
        services.AddSingleton<IOrderEventDispatcher, DefaultOrderEventDispatcher>();
        services.AddShopExecutionContexts();
        return services;
    }

    private void Seed(int id, bool isTest) => _database.Orders.Insert(new Order(id)
    {
        IsTest = isTest, Buyer = new Member { Id = 91, Name = "opposite", IsTest = !isTest },
        TotalPrice = 100m, CheckoutTransactionId = null
    });
    private string StoredOrders() => string.Join("\n", _database.Database.GetCollection("orders").FindAll().Select(x => x.ToString()));
    private void Send(int id, bool cancelled = false)
    {
        if (cancelled) Dispatcher.Dispatch(new OrderCancelledEvent { OrderId = id, ShopId = "test" });
        else Dispatcher.Dispatch(new OrderCompletedEvent { OrderId = id, ShopId = "test" });
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, false)]
    [InlineData(false, true)]
    [InlineData(true, true)]
    public void ReplayAfterCallerScopeDisposed_RestoresPersistedOrderPurpose(bool isTest, bool cancelled)
    {
        Seed(1, isTest);
        var before = StoredOrders();
        IIdentityContext callerIdentity; ITransactionContext callerTransaction;
        using (var caller = _provider.CreateScope())
        {
            var identity = caller.ServiceProvider.GetRequiredService<IdentityContext>();
            var transaction = caller.ServiceProvider.GetRequiredService<TransactionContext>();
            identity.Initialize(IdentitySnapshot.ForMember(91, !isTest));
            transaction.Bind(new(TransactionKind.Checkout, 700, !isTest));
            callerIdentity = identity; callerTransaction = transaction;
        }
        Send(1, cancelled); Send(1, cancelled);
        Assert.Equal(2, _state.Handlers.Count);
        var handlers = _state.Handlers.ToArray();
        Assert.NotSame(handlers[0].Identity, handlers[1].Identity);
        Assert.NotSame(handlers[0].Transaction, handlers[1].Transaction);
        Assert.NotSame(handlers[0].Effects, handlers[1].Effects);
        Assert.All(handlers, h =>
        {
            Assert.NotSame(callerIdentity, h.Identity); Assert.NotSame(callerTransaction, h.Transaction);
            Assert.Equal(IdentityKind.System, h.Identity.Current.Kind); Assert.Null(h.Identity.Current.MemberId);
            Assert.False(h.Identity.Current.IsTest);
            Assert.Equal(TransactionKind.Order, h.Transaction.Current!.Kind);
            Assert.Equal(1, h.Transaction.Current.Id); Assert.Equal(isTest, h.Transaction.Current.IsTest);
            Assert.Equal(cancelled, h.Cancelled); Assert.True(h.Disposed);
            Assert.Equal(isTest ? 2 : 0, h.Effects.Skipped.Count);
            Assert.Equal(isTest, h.Execution.Result!.IsTestBehavior);
            Assert.Equal(OperationOutcome.Succeeded, h.Execution.Result.Outcome);
            Assert.Equal(isTest ? 2 : 0, h.Execution.Result.SkippedEffects.Count);
        });
        Assert.Equal(isTest ? 0 : 4, _state.RealEffects);
        Assert.Equal(4, _state.Validations);
        Assert.Equal(before, StoredOrders());
        Assert.Empty(_database.ArchivedCheckoutTransactions.FindAll());
        Assert.Equal(isTest ? 4 : 0, _logs.Entries.Count(x => x.Fields.ContainsKey("EffectCode")));
        Assert.All(_logs.Entries.Where(x => x.Fields.ContainsKey("EffectCode")), entry =>
        {
            Assert.Equal(IdentityKind.System, entry.Fields["IdentityKind"]);
            Assert.Null(entry.Fields["MemberId"]);
            Assert.Equal(TransactionKind.Order, entry.Fields["TransactionKind"]);
            Assert.Equal(1, entry.Fields["TransactionId"]); Assert.Equal(true, entry.Fields["TransactionIsTest"]);
            Assert.Equal("test_behavior", entry.Fields["ReasonCode"]);
            Assert.Equal("test", entry.Fields["ShopId"]);
            Assert.Equal(OperationOutcome.Succeeded, entry.Fields["Outcome"]);
            Assert.Contains(handlers, h => h.Execution.TraceId == (string)entry.Fields["TraceId"]!);
        });
    }

    [Fact]
    public void SyncDispatch_DoesNotOverwriteCallerContextsOrEffects()
    {
        Seed(1, true);
        using var caller = _provider.CreateScope();
        var identity = caller.ServiceProvider.GetRequiredService<IdentityContext>();
        var transaction = caller.ServiceProvider.GetRequiredService<TransactionContext>();
        identity.Initialize(IdentitySnapshot.ForMember(7, false)); transaction.Bind(new(TransactionKind.Checkout, 12, true));
        var identityBefore = identity.Current; var transactionBefore = transaction.Current;
        var effects = caller.ServiceProvider.GetRequiredService<OperationEffects>();
        caller.ServiceProvider.GetRequiredService<ProtectedWriteExecutor>().ForCheckout(12, "inventory.deduct", "inventory", "sku", 2, () => throw new Exception());
        Send(1);
        Assert.Same(identityBefore, identity.Current); Assert.Same(transactionBefore, transaction.Current);
        Assert.Equal("inventory.deduct", Assert.Single(effects.Skipped).EffectCode);
        var handler = Assert.Single(_state.Handlers);
        Assert.NotSame(effects, handler.Effects); Assert.True(handler.Disposed);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void NestedDispatch_IncludingFailure_RetainsOuterOrderScope(bool innerFails)
    {
        Seed(1, true); Seed(2, false);
        _state.Process = (h, id) =>
        {
            if (id == 2 && innerFails) throw new IOException("內層處理失敗");
            if (id == 1)
            {
                var identity = h.Identity.Current; var transaction = h.Transaction.Current;
                if (innerFails) Assert.Throws<IOException>(() => Send(2)); else Send(2);
                Assert.Same(identity, h.Identity.Current); Assert.Same(transaction, h.Transaction.Current);
                Assert.Empty(h.Effects.Skipped);
            }
            h.RunEffects(id);
        };
        Send(1);
        Assert.Equal(2, _state.Handlers.Count); Assert.All(_state.Handlers, h => Assert.True(h.Disposed));
        Assert.Equal(2, _state.Handlers.Single(h => h.Transaction.Current!.Id == 1).Effects.Skipped.Count);
        Assert.Empty(_state.Handlers.Single(h => h.Transaction.Current!.Id == 2).Effects.Skipped);
        Assert.Equal(innerFails ? 0 : 2, _state.RealEffects);
    }

    [Fact]
    public async Task ParallelDispatch_KeepsSystemOrderAndEffectsIsolated()
    {
        Seed(1, true); Seed(2, false);
        using var barrier = new Barrier(2);
        _state.Process = (h, id) =>
        {
            if (!barrier.SignalAndWait(TimeSpan.FromSeconds(10))) throw new TimeoutException();
            Assert.Equal(id, h.Transaction.Current!.Id);
            h.RunEffects(id);
        };
        await Task.WhenAll(Task.Run(() => Send(1)), Task.Run(() => Send(2)));
        Assert.Equal(2, _state.Handlers.Count); Assert.Equal(2, _state.RealEffects);
        Assert.All(_state.Handlers, h => Assert.True(h.Disposed));
        Assert.Equal(2, _state.Handlers.Single(h => h.Transaction.Current!.IsTest).Effects.Skipped.Count);
        Assert.Empty(_state.Handlers.Single(h => !h.Transaction.Current!.IsTest).Effects.Skipped);
    }

    [Theory]
    [InlineData("missing-order")]
    [InlineData("null")]
    [InlineData("string")]
    [InlineData("number")]
    [InlineData("invalid-id")]
    public void InvalidSavedOrder_StopsBeforeHandlerConstructionAndDoesNotRepair(string fault)
    {
        if (fault is not "missing-order" and not "invalid-id")
            _database.Database.GetCollection("orders").Insert(new BsonDocument
            { ["_id"] = 1, ["IsTest"] = fault == "null" ? BsonValue.Null : fault == "string" ? new BsonValue("true") : new BsonValue(1) });
        var before = StoredOrders();
        Assert.Throws<ExecutionContextInvalidException>(() => Send(fault == "invalid-id" ? 0 : 1));
        Assert.Empty(_state.Handlers); Assert.Equal(0, _state.RealEffects);
        Assert.Equal(before, StoredOrders());
        Assert.Contains(_logs.Entries, x => x.Level == LogLevel.Error && x.Message.Contains("execution_context_invalid"));
        Assert.DoesNotContain(_logs.Entries, x => x.Fields.ContainsKey("EffectCode"));
        Seed(2, true); Send(2); // 失敗不污染下一次派送。
        Assert.True(Assert.Single(_state.Handlers).Transaction.Current!.IsTest);
    }

    [Fact]
    public void LegacyMissingPurpose_RemainsNormal_WithoutInventingCheckout()
    {
        _database.Database.GetCollection("orders").Insert(new BsonDocument { ["_id"] = 1 });
        var before = StoredOrders(); Send(1);
        Assert.False(Assert.Single(_state.Handlers).Transaction.Current!.IsTest);
        Assert.Equal(2, _state.RealEffects); Assert.Equal(before, StoredOrders());
        Assert.Empty(_database.ArchivedCheckoutTransactions.FindAll());
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("wrong-id")]
    [InlineData("wrong-kind")]
    [InlineData("wrong-purpose")]
    [InlineData("member")]
    public void LostEventContext_IsBugBeforeAnyCommercialEffect(string fault)
    {
        Seed(1, true);
        _state.Process = (h, id) =>
        {
            var context = (TransactionContext)h.Transaction;
            if (fault == "missing") context.Clear();
            if (fault == "wrong-id") context.Bind(new(TransactionKind.Order, 2, false));
            if (fault == "wrong-kind") context.Bind(new(TransactionKind.Checkout, 1, false));
            if (fault == "wrong-purpose") context.Bind(new(TransactionKind.Order, 1, false));
            if (fault == "member") ((IdentityContext)h.Identity).Initialize(IdentitySnapshot.ForMember(9, false));
            h.RunEffects(id);
        };
        Assert.Throws<ExecutionContextInvalidException>(() => Send(1));
        Assert.Equal(0, _state.Validations); Assert.Equal(0, _state.RealEffects);
        var handler = Assert.Single(_state.Handlers); Assert.Empty(handler.Effects.Skipped); Assert.True(handler.Disposed);
        Assert.Contains(_logs.Entries, x => x.Level == LogLevel.Error);
        Assert.Null(handler.Execution.Result); Assert.Equal(OperationOutcome.Failed, handler.Execution.Outcome);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void HandlerValidationFailure_IsNotASkippedEffect(bool isTest)
    {
        Seed(1, isTest);
        _state.Process = (h, id) => h.Writes.ForOrder(id, "shipment.create", "order", id.ToString(), "出貨",
            () => throw new UnauthorizedAccessException("拒絕"), () => Interlocked.Increment(ref _state.RealEffects));
        Assert.Throws<UnauthorizedAccessException>(() => Send(1));
        Assert.Empty(Assert.Single(_state.Handlers).Effects.Skipped); Assert.Equal(0, _state.RealEffects);
        Assert.Equal(OperationOutcome.Rejected, Assert.Single(_state.Handlers).Execution.Result!.Outcome);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task CheckoutDispatchFailureAndReplay_PreserveCommittedOrderAndFrozenArchive(bool isTest)
    {
        var members = _provider.GetRequiredService<MemberService>();
        var member = members.GetOrCreate("normal");
        var token = members.IssueToken(member.Id, DateTime.UtcNow.AddHours(1)).Token;
        _database.Products.Insert(new Product { Id = "p", Price = 100m, SkuId = "s" });
        _database.Skus.Insert(new SkuRecord { SkuId = "s" });
        _database.InventoryRecords.Insert(new InventoryRecord { SkuId = "s", AvailableQuantity = 10 });
        var cart = new Cart(); cart.AddProducts("p", 2, DateTime.UtcNow); _database.Carts.Insert(cart);
        _state.Process = (h, id) => { h.RunEffects(id); throw new IOException("處理失敗"); };
        int checkoutId; Order order;
        using (var operation = ShopOperationScope.Open(_provider.GetRequiredService<IServiceScopeFactory>(), token))
        {
            var checkout = operation.Services.GetRequiredService<CheckoutService>();
            checkoutId = checkout.Create(new() { CartId = cart.Id, PaymentId = isTest ? 990001 : 0, RequestMember = member }).TransactionId;
            var result = await checkout.CompleteAsync(new() { TransactionId = checkoutId, RequestMember = member });
            Assert.Equal(CheckoutCompleteStatus.Succeeded, result.Status); order = result.OrderDetail!;
            Assert.Equal(OrderFulfillmentStatus.Failed, order.FulfillmentStatus);
            var outer = operation.Services.GetRequiredService<ITransactionContext>().Current!;
            Assert.Equal(TransactionKind.Checkout, outer.Kind); Assert.Equal(checkoutId, outer.Id); Assert.Equal(isTest, outer.IsTest);
            var outerEffects = operation.Services.GetRequiredService<OperationEffects>();
            Assert.Equal(isTest ? 1 : 0, outerEffects.Skipped.Count);
            Assert.All(outerEffects.Skipped, effect => Assert.Equal("inventory.deduct", effect.EffectCode));
        }
        var archive = _database.Database.GetCollection("archived_checkout_transactions").FindById(checkoutId).ToString();
        var orderBefore = StoredOrders();
        _state.Process = null; Send(order.Id);
        Assert.Null(_database.CheckoutTransactions.FindById(checkoutId));
        Assert.Equal(CheckoutState.Completed, _database.ArchivedCheckoutTransactions.FindById(checkoutId).State);
        Assert.Equal(archive, _database.Database.GetCollection("archived_checkout_transactions").FindById(checkoutId).ToString());
        Assert.Equal(orderBefore, StoredOrders());
        Assert.Equal(isTest ? 10 : 8, _database.InventoryRecords.FindById("s").AvailableQuantity);
        Assert.Equal(isTest ? 0 : 4, _state.RealEffects);
        Assert.All(_state.Handlers, h => { Assert.Equal(isTest, h.Transaction.Current!.IsTest); Assert.True(h.Disposed); });
    }

    [Fact]
    public void NoAdapters_DoesNotInventEffects_ButStillRequiresStoredOrder()
    {
        using var provider = Services().BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
        var dispatcher = provider.GetRequiredService<IOrderEventDispatcher>();
        Seed(1, true); dispatcher.Dispatch(new OrderCompletedEvent { OrderId = 1 });
        Assert.Empty(_state.Handlers); Assert.DoesNotContain(_logs.Entries, x => x.Fields.ContainsKey("EffectCode"));
        Assert.Throws<ExecutionContextInvalidException>(() => dispatcher.Dispatch(new OrderCompletedEvent { OrderId = 99 }));
    }

    [Fact]
    public void EventResult_IsReturnedDirectly_AndFailureRetainsEarlierEffects()
    {
        Seed(1, true);
        var dispatcher = (DefaultOrderEventDispatcher)Dispatcher;
        var result = dispatcher.DispatchWithResult(new OrderCompletedEvent { OrderId = 1 });
        Assert.True(result.IsTestBehavior); Assert.Equal(OperationOutcome.Succeeded, result.Outcome);
        Assert.Equal(2, result.SkippedEffects.Count);
        var trace = Assert.Single(_state.Handlers).Execution.TraceId;
        _state.Process = (handler, id) => { handler.RunEffects(id); throw new IOException("後續失敗"); };
        Assert.Throws<IOException>(() => dispatcher.DispatchWithResult(new OrderCancelledEvent { OrderId = 1 }));
        var failed = Assert.Single(_state.Handlers, x => x.Execution.TraceId != trace);
        Assert.True(failed.Execution.Result!.IsTestBehavior);
        Assert.Equal(OperationOutcome.Failed, failed.Execution.Result.Outcome);
        Assert.Equal(2, failed.Execution.Result.SkippedEffects.Count);
        var logs = _logs.Entries.Where(x => x.Fields.ContainsKey("EffectCode") && (string)x.Fields["TraceId"]! == failed.Execution.TraceId).ToArray();
        Assert.Equal(2, logs.Length);
        Assert.All(logs, entry => Assert.Equal(OperationOutcome.Failed, entry.Fields["Outcome"]));
        Assert.Equal(OperationOutcome.Succeeded, result.Outcome);
        Assert.Equal(0, _state.RealEffects);
    }

    private sealed class State
    {
        public Action<Handler, int>? Process;
        public readonly ConcurrentBag<Handler> Handlers = new();
        public int RealEffects;
        public int Validations;
    }

    private sealed class Handler : IOrderEventHandler, IDisposable
    {
        private readonly State _state;
        public IIdentityContext Identity { get; }
        public ITransactionContext Transaction { get; }
        public ProtectedWriteExecutor Writes { get; }
        public OperationEffects Effects { get; }
        public OperationExecution Execution { get; }
        public bool Disposed { get; private set; }
        public bool Cancelled { get; private set; }
        public Handler(State state, IIdentityContext identity, ITransactionContext transaction, ProtectedWriteExecutor writes, OperationEffects effects, OperationExecution execution)
        {
            _state = state; Execution = execution; Identity = identity; Transaction = transaction; Writes = writes; Effects = effects;
            Assert.Equal(IdentityKind.System, identity.Current.Kind);
            Assert.Equal(TransactionKind.Order, transaction.Current!.Kind);
            Assert.Empty(effects.Skipped); state.Handlers.Add(this);
        }
        public void Handle(OrderCompletedEvent orderEvent) => Execute(orderEvent.OrderId);
        public void Handle(OrderCancelledEvent orderEvent) { Cancelled = true; Execute(orderEvent.OrderId); }
        private void Execute(int id) { if (_state.Process != null) _state.Process(this, id); else RunEffects(id); }
        public void RunEffects(int id)
        {
            // 測試替身：只計數，沒有外部出貨或統計 adapter。
            foreach (var effect in new[] { "shipment.create", "statistics.update" })
                Writes.ForOrder(id, effect, "order", id.ToString(), "測試替身",
                    () => Interlocked.Increment(ref _state.Validations), () => Interlocked.Increment(ref _state.RealEffects));
        }
        public void Dispose() => Disposed = true;
    }

    private sealed class CaptureLogs : ILoggerProvider
    {
        public readonly ConcurrentBag<(LogLevel Level, string Message, Dictionary<string, object?> Fields)> Entries = new();
        public ILogger CreateLogger(string categoryName) => new Logger(this);
        public void Dispose() { }
        private sealed class Logger(CaptureLogs owner) : ILogger
        {
            public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
            public bool IsEnabled(LogLevel logLevel) => true;
            public void Log<TState>(LogLevel level, EventId id, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
                => owner.Entries.Add((level, formatter(state, exception), ((IEnumerable<KeyValuePair<string, object?>>)(object)state!).ToDictionary(x => x.Key, x => x.Value)));
        }
    }

    public void Dispose() { _provider.Dispose(); _database.Dispose(); File.Delete(_path); }
}
