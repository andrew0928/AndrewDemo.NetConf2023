using LiteDB;

namespace AndrewDemo.NetConf2023.Core.Storage;

/// <summary>新資料庫初始化與啟動版本檢查；既有資料只能走明確離線遷移。</summary>
internal static class TransactionStorageSchema
{
    internal static void InitializeOrRequireReady(ILiteDatabase database)
    {
        using var scope = StorageTransaction.Begin(database, requireNew: true);
        database.GetCollection(TransactionCollections.Lock).Query().ForUpdate().ToArray();
        var marker = Marker(database);
        if (marker == null)
        {
            if (database.GetCollectionNames().Any(name => database.GetCollection(name).Exists(Query.All())))
                throw new InvalidOperationException("交易儲存尚未遷移。請停止 writers、備份後，以 DatabaseInit --migrate-tip-storage --database <path> 執行離線遷移。");
            EnsureIndexes(database);
            MarkReady(database);
        }
        else
        {
            RequireVersion(database);
            ValidateLayout(database);
        }
        scope.Commit();
    }

    internal static BsonDocument? Marker(ILiteDatabase database) => database.GetCollection(TransactionCollections.Metadata).FindById(1);
    internal static void RequireVersion(ILiteDatabase database)
    {
        if (Marker(database)?["Version"] != new BsonValue(TransactionCollections.Version))
            throw new InvalidOperationException("交易儲存版本不受支援或尚未遷移；拒絕使用此資料庫。");
    }
    internal static void MarkReady(ILiteDatabase database) => database.GetCollection(TransactionCollections.Metadata)
        .Insert(new BsonDocument { ["_id"] = 1, ["Version"] = TransactionCollections.Version });

    internal static void EnsureIndexes(ILiteDatabase database)
    {
        foreach (var test in new[] { false, true })
        {
            database.GetCollection(TransactionCollections.Orders(test)).EnsureIndex("Buyer._id");
            database.GetCollection(TransactionCollections.Orders(test)).EnsureIndex("CheckoutTransactionId");
            foreach (var archived in new[] { false, true })
                database.GetCollection(TransactionCollections.Checkouts(test, archived)).EnsureIndex("MemberId");
        }
    }

    internal static void ValidateLayout(ILiteDatabase database)
    {
        var store = new TransactionStore(database);
        store.ReadOrders("all");
        store.ReadCheckouts("all");
    }
}
