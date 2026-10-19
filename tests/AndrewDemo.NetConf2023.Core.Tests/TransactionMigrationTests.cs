using System.Reflection;
using AndrewDemo.NetConf2023.Core.Storage;
using LiteDB;

namespace AndrewDemo.NetConf2023.Core.Tests;

public sealed class TransactionMigrationTests
{
    [Fact]
    public void MigrationPreservesRawFieldsAndLegacyMissingValues_AndRepeatsAsNoOp()
    {
        using var db = new LiteDatabase(":memory:");
        Seed(db);
        var before = Snapshot(db);
        var result = TransactionStorageMigrator.Migrate(db);
        Assert.False(result.AlreadyCurrent);
        Assert.Equal((1, 1, 1), (result.MovedOrders, result.MovedActive, result.MovedArchived));
        Assert.Equal(before, Snapshot(db));
        Assert.Null(db.GetCollection("orders").FindById(2));
        Assert.NotNull(db.GetCollection("test_orders").FindById(2));
        Assert.False(db.GetCollection("checkout_transactions").FindById(1).ContainsKey("IsTest"));
        Assert.False(db.GetCollection("test_checkout_transactions").FindById(2).ContainsKey("PaymentId"));
        Assert.Equal(7, db.GetCollection("test_orders").FindById(2)["unknown"]["nested"].AsInt32);
        Assert.True(TransactionStorageMigrator.Migrate(db).AlreadyCurrent);
        Assert.Equal(before, Snapshot(db));
    }

    [Theory]
    [InlineData("null")]
    [InlineData("type")]
    [InlineData("duplicate")]
    [InlineData("link")]
    [InlineData("target")]
    [InlineData("version")]
    public void MigrationRejectsAmbiguityWithoutPartialWrites(string issue)
    {
        using var db = new LiteDatabase(":memory:"); Seed(db);
        var source = db.GetCollection("orders");
        var doc = source.FindById(2);
        if (issue is "null" or "type") { doc["IsTest"] = issue == "null" ? BsonValue.Null : new BsonValue("true"); source.Update(doc); }
        if (issue == "duplicate") db.GetCollection("archived_checkout_transactions").Insert(new BsonDocument { ["_id"] = 2, ["IsTest"] = true });
        if (issue == "link") { doc["CheckoutTransactionId"] = 1; source.Update(doc); }
        if (issue == "target") db.GetCollection("test_orders").Insert(new BsonDocument { ["_id"] = 99, ["IsTest"] = true });
        if (issue == "version") db.GetCollection("transaction_storage").Insert(new BsonDocument { ["_id"] = 1, ["Version"] = 999 });
        var before = EntireDatabase(db);
        Assert.ThrowsAny<InvalidOperationException>(() => TransactionStorageMigrator.Migrate(db));
        Assert.Equal(before, EntireDatabase(db));
    }

    [Theory]
    [InlineData("test_orders", "Insert")]
    [InlineData("orders", "Delete")]
    [InlineData("test_checkout_transactions", "Insert")]
    [InlineData("checkout_transactions", "Delete")]
    [InlineData("test_archived_checkout_transactions", "Insert")]
    [InlineData("archived_checkout_transactions", "Delete")]
    [InlineData("transaction_storage", "Insert")]
    [InlineData("", "Commit")]
    public void FailureRollsBackEveryMoveAndVersion_ThenRetrySucceeds(string name, string operation)
    {
        using var db = new LiteDatabase(":memory:"); Seed(db);
        var before = EntireDatabase(db);
        var raw = Intercept<ILiteDatabase>(db, (method, args) =>
        {
            if (name == "" && method.Name == operation) throw new IOException("提交前故障");
            if (method.Name == "GetCollection" && Equals(args?[0], name))
            {
                var target = db.GetCollection(name);
                return (true, Intercept(target, (call, values) =>
                {
                    if (call.Name != operation) return (false, null);
                    call.Invoke(target, values);
                    throw new IOException("寫入後故障");
                }));
            }
            return (false, null);
        });
        Assert.Throws<IOException>(() => TransactionStorageMigrator.Migrate(raw));
        Assert.Equal(before, EntireDatabase(db));
        Assert.False(TransactionStorageMigrator.Migrate(db).AlreadyCurrent);
        Assert.True(TransactionStorageMigrator.Migrate(db).AlreadyCurrent);
    }

    [Fact]
    public void HostRejectsUnmigratedAndUnknownVersion_AndAcceptsMigratedDatabase()
    {
        var path = Path.Combine(Path.GetTempPath(), $"tip-migration-{Guid.NewGuid():N}.db");
        try
        {
            using (var db = new LiteDatabase(path)) Seed(db);
            Assert.Contains("遷移", Assert.Throws<InvalidOperationException>(() => new ShopDatabaseContext(new() { ConnectionString = path })).Message);
            using (var db = new LiteDatabase(path)) TransactionStorageMigrator.Migrate(db);
            using (var context = new ShopDatabaseContext(new() { ConnectionString = path })) Assert.Equal(2, context.Transactions.Orders("all").Length);
            using (var db = new LiteDatabase(path)) db.GetCollection("transaction_storage").Update(new BsonDocument { ["_id"] = 1, ["Version"] = 2 });
            Assert.Throws<InvalidOperationException>(() => new ShopDatabaseContext(new() { ConnectionString = path }));
        }
        finally { File.Delete(path); File.Delete(Path.ChangeExtension(path, null) + "-log.db"); }
    }

    private static void Seed(ILiteDatabase db)
    {
        db.GetCollection("members").Insert(new BsonDocument { ["_id"] = 1, ["Name"] = "shared", ["IsTest"] = false });
        db.GetCollection("checkout_transactions").Insert(new BsonDocument { ["_id"] = 1 });
        db.GetCollection("checkout_transactions").Insert(new BsonDocument { ["_id"] = 2, ["IsTest"] = true });
        db.GetCollection("archived_checkout_transactions").Insert(new BsonDocument { ["_id"] = 3, ["IsTest"] = true, ["State"] = "Completed" });
        db.GetCollection("orders").Insert(new BsonDocument { ["_id"] = 1 });
        db.GetCollection("orders").Insert(new BsonDocument { ["_id"] = 2, ["IsTest"] = true, ["CheckoutTransactionId"] = 3,
            ["unknown"] = new BsonDocument { ["nested"] = 7 } });
    }
    private static string[] Snapshot(ILiteDatabase db) => db.GetCollectionNames().Where(x => x != "transaction_storage")
        .SelectMany(name => db.GetCollection(name).FindAll().Select(x => (name.StartsWith("test_") ? name[5..] : name) + ":" + x.ToString()))
        .Order(StringComparer.Ordinal).ToArray();
    private static string[] EntireDatabase(ILiteDatabase db) => db.GetCollectionNames()
        .SelectMany(name => db.GetCollection(name).FindAll().Select(x => name + ":" + x.ToString())).Order(StringComparer.Ordinal).ToArray();
    private static T Intercept<T>(T target, Func<MethodInfo, object?[]?, (bool, object?)> before) where T : class
    {
        var proxy = DispatchProxy.Create<T, MemberServiceTests.CallInterceptor>();
        var interceptor = (MemberServiceTests.CallInterceptor)(object)proxy;
        interceptor.Target = target; interceptor.Before = before;
        return proxy;
    }
}
