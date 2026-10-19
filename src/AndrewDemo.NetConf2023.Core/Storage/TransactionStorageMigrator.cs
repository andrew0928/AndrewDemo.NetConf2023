using LiteDB;

namespace AndrewDemo.NetConf2023.Core.Storage;

public sealed record TransactionMigrationResult(bool AlreadyCurrent, int MovedOrders, int MovedActive, int MovedArchived);

/// <summary>明確離線遷移；呼叫端先停止 writers 並備份。只搬位置，不重算用途或重新序列化。</summary>
public static class TransactionStorageMigrator
{
    public static TransactionMigrationResult Migrate(ILiteDatabase database)
    {
        using var scope = StorageTransaction.Begin(database, requireNew: true);
        database.GetCollection(TransactionCollections.Lock).Query().ForUpdate().ToArray();
        if (TransactionStorageSchema.Marker(database) != null)
        {
            TransactionStorageSchema.RequireVersion(database);
            TransactionStorageSchema.ValidateLayout(database);
            return new(true, 0, 0, 0);
        }
        foreach (var partition in TransactionCollections.All().Where(x => x.IsTest))
            if (database.GetCollection(partition.Name).Exists(Query.All()))
                throw new InvalidOperationException("未標記版本的資料庫已有 test 目標資料，停止遷移，不覆寫或猜測搬移進度。");

        var orders = database.GetCollection(TransactionCollections.Orders(false)).FindAll().ToArray();
        var active = database.GetCollection(TransactionCollections.Checkouts(false)).FindAll().ToArray();
        var archived = database.GetCollection(TransactionCollections.Checkouts(false, true)).FindAll().ToArray();
        var checkouts = active.Concat(archived).ToArray();
        ValidateLegacy(orders, checkouts);
        var before = Snapshot(orders, active, archived);

        var movedOrders = Move(database, orders, TransactionCollections.Orders(false), TransactionCollections.Orders(true));
        var movedActive = Move(database, active, TransactionCollections.Checkouts(false), TransactionCollections.Checkouts(true));
        var movedArchived = Move(database, archived, TransactionCollections.Checkouts(false, true), TransactionCollections.Checkouts(true, true));
        TransactionStorageSchema.EnsureIndexes(database);
        TransactionStorageSchema.MarkReady(database);
        TransactionStorageSchema.ValidateLayout(database);
        var store = new TransactionStore(database);
        var after = Snapshot(store.ReadOrders("all"), store.ReadCheckouts("all", false).Select(x => x.Document),
            store.ReadCheckouts("all", true).Select(x => x.Document));
        if (!before.SequenceEqual(after)) throw new InvalidOperationException("遷移後資料核對不符，全部回復。");
        scope.Commit();
        return new(false, movedOrders, movedActive, movedArchived);
    }

    private static void ValidateLegacy(BsonDocument[] orders, BsonDocument[] checkouts)
    {
        foreach (var doc in orders.Concat(checkouts)) { TransactionCollections.Id(doc); TransactionCollections.Purpose(doc); }
        if (checkouts.Select(TransactionCollections.Id).Distinct().Count() != checkouts.Length)
            throw TransactionCollections.Invalid("遷移來源 Checkout 存在重複 ID。");
        var byId = checkouts.ToDictionary(TransactionCollections.Id);
        foreach (var order in orders)
        {
            var link = order["CheckoutTransactionId"];
            if (link.IsNull) continue;
            if (!link.IsInt32 || link.AsInt32 <= 0) throw TransactionCollections.Invalid("遷移來源 Order 關聯無效。");
            if (byId.TryGetValue(link.AsInt32, out var checkout)
                && TransactionCollections.Purpose(order) != TransactionCollections.Purpose(checkout))
                throw TransactionCollections.Invalid("遷移來源 Order 與 Checkout 用途矛盾。");
        }
    }

    private static int Move(ILiteDatabase database, IEnumerable<BsonDocument> rows, string from, string to)
    {
        var count = 0;
        foreach (var doc in rows.Where(TransactionCollections.Purpose))
        {
            database.GetCollection(to).Insert(doc);
            if (!database.GetCollection(from).Delete(doc["_id"])) throw new InvalidOperationException("遷移來源移除失敗。");
            count++;
        }
        return count;
    }

    private static string[] Snapshot(IEnumerable<BsonDocument> orders, IEnumerable<BsonDocument> active, IEnumerable<BsonDocument> archived) =>
        orders.Select(x => "order:" + x.ToString()).Concat(active.Select(x => "active:" + x.ToString())).Concat(archived.Select(x => "archive:" + x.ToString()))
            .Order(StringComparer.Ordinal).ToArray();
}
