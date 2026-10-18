using System.Reflection;
using AndrewDemo.NetConf2023.Core.AdminQueries;
using AndrewDemo.NetConf2023.Core.Checkouts;
using LiteDB;

namespace AndrewDemo.NetConf2023.Core.Tests;

public sealed class AdminQueryTests : ShopDatabaseTestBase
{
    private AdminQueryService Service => new(Context, FixedTimeProvider);
    private static BsonDocument OrderDoc(int id, decimal? amount = 100, bool isTest = false) => new()
    {
        ["_id"] = id, ["IsTest"] = isTest, ["TotalPrice"] = amount is { } value ? new BsonValue(value) : BsonValue.Null,
        ["CreatedAt"] = FixedUtcNow, ["Buyer"] = new BsonDocument { ["_id"] = 10, ["Name"] = "saved", ["IsTest"] = false },
        ["FulfillmentStatus"] = "Failed"
    };
    private static BsonDocument CheckoutDoc(int id, string state = "Created") => new()
    {
        ["_id"] = id, ["MemberId"] = 10, ["IsTest"] = false, ["State"] = state, ["CreatedAt"] = FixedUtcNow,
        ["ClosedAt"] = state == "Completed" ? new BsonValue(FixedUtcNow) : BsonValue.Null, ["TotalPrice"] = 100m
    };

    [Theory]
    [InlineData("normal", 2, 300)]
    [InlineData("test", 1, 900)]
    [InlineData("all", 3, 1200)]
    public void Orders_UseOwnPurposeAndSummarizeBeforePagination(string purpose, int count, int amount)
    {
        var collection = Context.Database.GetCollection("orders");
        collection.Insert(OrderDoc(1)); collection.Insert(OrderDoc(2, 200)); collection.Insert(OrderDoc(3, 900, true));
        Context.Members.Insert(new Member { Id = 10, Name = "changed", IsTest = true });
        var result = Service.GetOrders(new() { Purpose = purpose, PageSize = 1 });
        Assert.Equal(count, result.Page.TotalCount);
        Assert.Equal((decimal)amount, result.TotalAmount);
        Assert.Equal(result.TotalAmount, result.KnownTotalAmount);
        Assert.Equal("saved", Assert.Single(result.Page.Items).MemberName);
        Assert.Equal("failed", result.Page.Items[0].FulfillmentStatus);
        Assert.Equal(FixedUtcNow, result.Page.QueriedAt);
        Assert.Equal(purpose == "normal" ? 2 : 3, result.Page.Items[0].Id);
        Assert.Empty(Service.GetOrders(new() { Purpose = purpose, Page = int.MaxValue }).Page.Items);
        Assert.Equal(amount, Service.GetOrders(new() { Purpose = purpose, Page = int.MaxValue }).TotalAmount);
    }

    [Fact]
    public void LegacyMissingPurposeTimeAndAmount_AreNotFabricated()
    {
        var legacy = OrderDoc(1, null); legacy.Remove("IsTest"); legacy.Remove("CreatedAt"); legacy.Remove("Buyer");
        Context.Database.GetCollection("orders").Insert(legacy);
        Context.Database.GetCollection("orders").Insert(OrderDoc(2));
        Context.Database.GetCollection("orders").Insert(OrderDoc(3, 0));
        var result = Service.GetOrders(new());
        Assert.Null(result.TotalAmount); Assert.Equal(100, result.KnownTotalAmount); Assert.Equal(1, result.UnknownAmountCount);
        var row = result.Page.Items.Single(x => x.Id == 1);
        Assert.False(row.IsTest); Assert.Null(row.CreatedAt); Assert.Null(row.MemberId); Assert.Null(row.CheckoutTransactionId);
        var dated = Service.GetOrders(new() { From = FixedUtcNow, To = FixedUtcNow.AddSeconds(1) });
        Assert.Equal(2, dated.Page.TotalCount); Assert.Equal(1, dated.Page.ExcludedUnknownTimeCount); Assert.Equal(100, dated.TotalAmount);
        var excluded = Service.GetOrders(new() { To = FixedUtcNow });
        Assert.Equal(0, excluded.Page.TotalCount); Assert.Equal(0, excluded.TotalAmount);
        Assert.Equal(0, Service.GetOrders(new() { MemberId = 999, From = FixedUtcNow }).Page.ExcludedUnknownTimeCount);
    }

    [Theory]
    [InlineData("members")]
    [InlineData("orders")]
    [InlineData("checkout_transactions")]
    public void InvalidPurpose_FailsInsteadOfClassifyingNormal(string collection)
    {
        Context.Database.GetCollection(collection).Insert(new BsonDocument { ["_id"] = 1, ["IsTest"] = "false" });
        Assert.Throws<AdminDataInvalidException>(() =>
        {
            if (collection == "members") Service.GetMembers(new());
            else if (collection == "orders") Service.GetOrders(new());
            else Service.GetCheckouts(new());
        });
    }

    [Theory]
    [InlineData("TotalPrice")]
    [InlineData("CreatedAt")]
    [InlineData("Buyer")]
    [InlineData("FulfillmentStatus")]
    public void InvalidStoredFields_AreDataErrors(string field)
    {
        var doc = OrderDoc(1); doc[field] = "invalid";
        Context.Database.GetCollection("orders").Insert(doc);
        Assert.Throws<AdminDataInvalidException>(() => Service.GetOrders(new()));
    }

    [Fact]
    public void Checkouts_MergeBeforePagingAndPreserveUnknownFields()
    {
        Context.Database.GetCollection("checkout_transactions").Insert(CheckoutDoc(1));
        Context.Database.GetCollection("checkout_transactions").Insert(new BsonDocument { ["_id"] = 4 });
        Context.Database.GetCollection("archived_checkout_transactions").Insert(CheckoutDoc(2, "Completed"));
        Context.Database.GetCollection("archived_checkout_transactions").Insert(CheckoutDoc(3, "Cancelled"));
        var result = Service.GetCheckouts(new() { PageSize = 2 });
        Assert.Equal(4, result.TotalCount); Assert.Equal(new[] { 4, 3 }, result.Items.Select(x => x.TransactionId));
        var unknown = result.Items[0];
        Assert.Equal("unknown", unknown.State); Assert.Null(unknown.CreatedAt); Assert.Null(unknown.PaymentId); Assert.Null(unknown.TotalPrice);
        var completed = Service.GetCheckouts(new() { State = "completed", TimeField = "closedAt", From = FixedUtcNow, To = FixedUtcNow.AddSeconds(1) });
        Assert.Equal("archived", Assert.Single(completed.Items).Storage);
        Assert.Equal(2, completed.Items[0].TransactionId);
        var closed = Service.GetCheckouts(new() { TimeField = "closedAt", From = FixedUtcNow });
        Assert.Equal(3, closed.ExcludedUnknownTimeCount); Assert.Single(closed.Items);
    }

    [Theory]
    [InlineData("en-US/IgnoreCase", "TEST", 1)]
    [InlineData("en-US/None", "TEST", 0)]
    [InlineData("tr-TR/IgnoreCase", "İ", 1)]
    public void MemberName_UsesDatabaseCollation(string collation, string name, int count)
    {
        using var database = new ShopDatabaseContext(new() { ConnectionString = $"Filename=:memory:;Collation={collation}" });
        database.Members.Insert(new Member { Id = 1, Name = name == "İ" ? "i" : "test", IsTest = true });
        var service = new AdminQueryService(database, FixedTimeProvider);
        Assert.Equal(count, service.GetMembers(new() { Purpose = "test", Name = name, MemberId = 1 }).TotalCount);
        Assert.Empty(service.GetMembers(new() { Purpose = "test", Name = name, MemberId = 2 }).Items);
    }

    [Fact]
    public void Reads_DoNotCreateCollectionsOrRewriteLegacyDocuments()
    {
        Context.Database.GetCollection("members").Insert(new BsonDocument { ["_id"] = 1, ["Name"] = "legacy", ["ShopNotes"] = "private" });
        var collections = Context.Database.GetCollectionNames().Order().ToArray();
        var before = Context.Database.GetCollection("members").FindById(1).ToString();
        Service.GetMembers(new()); Service.GetOrders(new()); Service.GetCheckouts(new());
        Assert.Equal(collections, Context.Database.GetCollectionNames().Order().ToArray());
        Assert.Equal(before, Context.Database.GetCollection("members").FindById(1).ToString());
        Assert.Equal(0, Context.MemberTokens.Count());
    }

    [Theory]
    [InlineData(false, "Shared")]
    [InlineData(true, "Shared")]
    [InlineData(false, "Direct")]
    [InlineData(true, "Direct")]
    public async Task CheckoutMove_BlocksBetweenPartitionReads_ThenFreshReadSeesArchive(bool lockExists, string connection)
    {
        var path = Path.Combine(Path.GetTempPath(), $"admin-concurrent-{Guid.NewGuid():N}.db");
        try
        {
            using var reader = new ShopDatabaseContext(new() { ConnectionString = $"Filename={path};Connection={connection}" });
            using var writer = connection == "Direct" ? reader : new ShopDatabaseContext(new() { ConnectionString = $"Filename={path};Connection=Shared" });
            reader.Database.GetCollection("checkout_transactions").Insert(CheckoutDoc(1));
            if (lockExists) reader.Database.GetCollection("checkout_write_lock").Insert(new BsonDocument { ["_id"] = 1 });
            using var atBoundary = new ManualResetEventSlim();
            using var tryingWrite = new ManualResetEventSlim();
            using var finishedWrite = new ManualResetEventSlim();
            var write = Task.Run(() =>
            {
                Assert.True(atBoundary.Wait(TimeSpan.FromSeconds(10)));
                tryingWrite.Set();
                Assert.True(writer.Database.BeginTrans());
                try
                {
                    writer.Database.GetCollection("checkout_write_lock").Upsert(new BsonDocument { ["_id"] = 1 });
                    writer.Database.GetCollection("archived_checkout_transactions").Insert(CheckoutDoc(1, "Completed"));
                    writer.Database.GetCollection("checkout_transactions").Delete(1);
                    writer.Database.Commit();
                }
                finally { writer.Database.Rollback(); finishedWrite.Set(); }
            });
            var intercepted = Intercept(reader.Database, (method, args) =>
            {
                if (method.Name == "GetCollection" && Equals(args?[0], "archived_checkout_transactions"))
                {
                    atBoundary.Set();
                    Assert.True(tryingWrite.Wait(TimeSpan.FromSeconds(10)));
                    Assert.False(finishedWrite.Wait(TimeSpan.FromMilliseconds(200)));
                }
                return (false, null);
            });
            var context = Intercept<IShopDatabaseContext>(reader, (method, _) => method.Name == "get_Database" ? (true, intercepted) : (false, null));
            var result = new AdminQueryService(context, FixedTimeProvider).GetCheckouts(new());
            Assert.Equal("active", Assert.Single(result.Items).Storage);
            await write.WaitAsync(TimeSpan.FromSeconds(10));
            Assert.Equal("archived", Assert.Single(new AdminQueryService(reader, FixedTimeProvider).GetCheckouts(new()).Items).Storage);
        }
        finally { File.Delete(path); File.Delete(Path.ChangeExtension(path, null) + "-log.db"); }
    }

    [Fact]
    public async Task Orders_CommittingDuringEnumeration_DoesNotMixSummaryAndPage()
    {
        var orders = Context.Database.GetCollection("orders");
        orders.Insert(OrderDoc(1)); orders.Insert(OrderDoc(2, 200));
        using var firstRead = new ManualResetEventSlim(); using var committed = new ManualResetEventSlim();
        var writer = Task.Run(() =>
        {
            Assert.True(firstRead.Wait(TimeSpan.FromSeconds(10)));
            orders.Insert(OrderDoc(3, 900)); committed.Set();
        });
        IEnumerable<BsonDocument> DuringRead()
        {
            using var cursor = orders.FindAll().GetEnumerator();
            Assert.True(cursor.MoveNext());
            firstRead.Set();
            Assert.True(committed.Wait(TimeSpan.FromSeconds(10)));
            yield return cursor.Current;
            while (cursor.MoveNext()) yield return cursor.Current;
        }
        var collection = Intercept(orders, (method, _) => method.Name == "FindAll" ? (true, DuringRead()) : (false, null));
        var db = Intercept(Context.Database, (method, args) =>
            method.Name == "GetCollection" && Equals(args?[0], "orders") ? (true, collection) : (false, null));
        var context = Intercept<IShopDatabaseContext>(Context, (method, _) => method.Name == "get_Database" ? (true, db) : (false, null));
        var result = new AdminQueryService(context, FixedTimeProvider).GetOrders(new());
        Assert.Equal(2, result.Page.TotalCount); Assert.Equal(300, result.TotalAmount);
        Assert.Equal(new[] { 2, 1 }, result.Page.Items.Select(x => x.Id));
        await writer;
        Assert.Equal(1200, Service.GetOrders(new()).TotalAmount);
    }

    [Fact]
    public async Task UncommittedOrder_IsNotVisible_AndRollbackLeavesNoRow()
    {
        using var inserted = new ManualResetEventSlim(); using var release = new ManualResetEventSlim();
        var writing = Task.Run(() =>
        {
            Context.Database.BeginTrans();
            try { Context.Database.GetCollection("orders").Insert(OrderDoc(1)); inserted.Set(); Assert.True(release.Wait(TimeSpan.FromSeconds(10))); }
            finally { Context.Database.Rollback(); }
        });
        try { Assert.True(inserted.Wait(TimeSpan.FromSeconds(10))); Assert.Empty(Service.GetOrders(new()).Page.Items); }
        finally { release.Set(); }
        await writing;
        Assert.Empty(Service.GetOrders(new()).Page.Items);
        Context.Database.GetCollection("orders").Insert(OrderDoc(2));
        Assert.Equal(2, Assert.Single(Service.GetOrders(new()).Page.Items).Id);
    }

    private static T Intercept<T>(T target, Func<MethodInfo, object?[]?, (bool, object?)> before) where T : class
    {
        var result = DispatchProxy.Create<T, Proxy>();
        ((Proxy)(object)result).Target = target; ((Proxy)(object)result).Before = before;
        return result;
    }
    public class Proxy : DispatchProxy
    {
        public object Target = null!;
        public Func<MethodInfo, object?[]?, (bool, object?)> Before = null!;
        protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
        {
            var (handled, value) = Before(targetMethod!, args);
            if (handled) return value;
            try { return targetMethod!.Invoke(Target, args); }
            catch (TargetInvocationException error) when (error.InnerException != null)
            { System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(error.InnerException).Throw(); throw; }
        }
    }
}
