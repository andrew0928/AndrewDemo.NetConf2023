using AndrewDemo.NetConf2023.Core.Checkouts;
using LiteDB;

namespace AndrewDemo.NetConf2023.Core.Storage;

public sealed record StoredCheckout(BsonDocument Document, bool Archived);

/// <summary>交易存取集中處理用途、唯一識別與跨 collection 一致讀取；不暴露可寫 collection。</summary>
public sealed class TransactionStore(ILiteDatabase database)
{
    public Order? Order(int id) => Read(() => Map<Order>(LocateOrder(id)));
    public CheckoutTransactionRecord? Checkout(int id, bool archived = false) =>
        Read(() => Map<CheckoutTransactionRecord>(LocateCheckout(id) is { } item && item.Archived == archived ? item.Document : null));
    public Order[] Orders(string purpose) => ReadOrders(purpose).Select(database.Mapper.ToObject<Order>).ToArray();
    public CheckoutTransactionRecord[] Checkouts(string purpose, bool archived = false) =>
        ReadCheckouts(purpose, archived).Select(x => database.Mapper.ToObject<CheckoutTransactionRecord>(x.Document)).ToArray();
    public BsonDocument? FindOrder(int id) => Read(() => LocateOrder(id));
    public BsonDocument? FindActiveCheckout(int id) => Read(() => LocateCheckout(id) is { Archived: false } item ? item.Document : null);

    public BsonDocument[] ReadOrders(string purpose) => Read(() =>
    {
        var rows = TransactionCollections.Purposes(purpose).SelectMany(test => Partition(TransactionCollections.Orders(test), test)).ToArray();
        foreach (var row in rows) LocateOrder(TransactionCollections.Id(row));
        return rows;
    });

    public StoredCheckout[] ReadCheckouts(string purpose, bool? archived = null) => Read(() =>
    {
        var rows = (from test in TransactionCollections.Purposes(purpose)
                    from archive in new[] { false, true }
                    where archived == null || archived == archive
                    from doc in Partition(TransactionCollections.Checkouts(test, archive), test)
                    select new StoredCheckout(doc, archive)).ToArray();
        foreach (var row in rows) LocateCheckout(TransactionCollections.Id(row.Document));
        return rows;
    });

    public void InsertCheckout(CheckoutTransactionRecord checkout) => Write(() =>
    {
        CheckoutStoredContent.Validate(checkout);
        if (checkout.TransactionId == 0) checkout.TransactionId = NextId(false);
        if (LocateCheckout(checkout.TransactionId) != null) throw TransactionCollections.Invalid("Checkout ID 已存在，不得覆寫、跨用途複製或重用封存 ID。");
        var doc = database.Mapper.ToDocument(checkout);
        TransactionCollections.Validate(doc, checkout.IsTest);
        database.GetCollection(TransactionCollections.Checkouts(checkout.IsTest)).Insert(doc);
    });

    public void InsertOrder(Order order) => Write(() =>
    {
        var doc = database.Mapper.ToDocument(order);
        TransactionCollections.Validate(doc, order.IsTest);
        if (LocateOrder(order.Id) != null) throw TransactionCollections.Invalid("Order ID 已存在，不得覆寫或跨用途複製。");
        ValidateOrderLink(doc);
        database.GetCollection(TransactionCollections.Orders(order.IsTest)).Insert(doc);
    });

    internal int NextOrderId()
    {
        StorageTransaction.RequireActive(database);
        return NextId(true);
    }

    internal void ArchiveCheckout(int id, DateTime closedAt)
    {
        StorageTransaction.RequireActive(database);
        var item = LocateCheckout(id);
        if (item == null || item.Archived) throw TransactionCollections.Invalid("只有作用中 Checkout 可以完成封存。");
        var doc = item.Document;
        CheckoutStoredContent.Validate(database.Mapper.ToObject<CheckoutTransactionRecord>(doc));
        var test = TransactionCollections.Purpose(doc);
        // 原 BSON 只設定終態與時間，保留未知擴充欄位與歷史缺值。
        doc["State"] = CheckoutState.Completed.ToString();
        doc["ClosedAt"] = closedAt;
        database.GetCollection(TransactionCollections.Checkouts(test, true)).Insert(doc);
        if (!database.GetCollection(TransactionCollections.Checkouts(test)).Delete(id))
            throw new InvalidOperationException("作用中交易移除失敗。");
    }

    internal void UpdateFulfillment(int id, OrderFulfillmentStatus status) => Write(() =>
    {
        var doc = LocateOrder(id) ?? throw TransactionCollections.Invalid("履約更新找不到 Order。");
        var collection = database.GetCollection(TransactionCollections.Orders(TransactionCollections.Purpose(doc)));
        if (collection.UpdateMany(BsonExpression.Create("{ FulfillmentStatus: @0 }", new BsonValue(status.ToString())),
            BsonExpression.Create("_id = @0", new BsonValue(id))) != 1)
            throw TransactionCollections.Invalid("履約更新失敗。");
    });

    internal StorageTransaction BeginWrite()
    {
        var scope = StorageTransaction.Begin(database, requireNew: true);
        try { AcquireLock(); TransactionStorageSchema.RequireVersion(database); return scope; }
        catch { scope.Dispose(); throw; }
    }

    private int NextId(bool order)
    {
        var names = TransactionCollections.All().Where(x => (x.Archived == null) == order).Select(x => x.Name);
        return checked(names.Select(name => database.GetCollection(name).Query().OrderByDescending("_id").FirstOrDefault())
            .Where(x => x != null).Select(x => TransactionCollections.Id(x!)).DefaultIfEmpty(0).Max() + 1);
    }

    private BsonDocument[] Partition(string name, bool isTest)
    {
        var rows = database.GetCollection(name).FindAll().ToArray();
        foreach (var row in rows) TransactionCollections.Validate(row, isTest);
        return rows;
    }

    private BsonDocument? LocateOrder(int id)
    {
        BsonDocument? found = null;
        foreach (var test in new[] { false, true })
        {
            var doc = database.GetCollection(TransactionCollections.Orders(test)).FindById(id);
            if (doc == null) continue;
            TransactionCollections.Validate(doc, test);
            if (found != null) throw TransactionCollections.Invalid("Order 跨用途存在重複 ID。");
            ValidateOrderLink(doc);
            found = doc;
        }
        return found;
    }

    private StoredCheckout? LocateCheckout(int id)
    {
        StoredCheckout? found = null;
        foreach (var partition in TransactionCollections.All().Where(x => x.Archived != null))
        {
            var doc = database.GetCollection(partition.Name).FindById(id);
            if (doc == null) continue;
            TransactionCollections.Validate(doc, partition.IsTest);
            if (found != null) throw TransactionCollections.Invalid("Checkout 跨用途或生命週期存在重複 ID。");
            found = new(doc, partition.Archived!.Value);
        }
        if (found != null)
            foreach (var test in new[] { false, true })
                foreach (var order in database.GetCollection(TransactionCollections.Orders(test))
                    .Find(BsonExpression.Create("CheckoutTransactionId = @0", new BsonValue(id))))
                {
                    TransactionCollections.Validate(order, test);
                    ValidateLink(order, found.Document);
                }
        return found;
    }

    private void ValidateOrderLink(BsonDocument order)
    {
        var link = order["CheckoutTransactionId"];
        if (link.IsNull) return;
        if (!link.IsInt32 || link.AsInt32 <= 0) throw TransactionCollections.Invalid("Order 的 Checkout 關聯無效。");
        if (LocateCheckout(link.AsInt32) is { } checkout) ValidateLink(order, checkout.Document);
    }

    private static void ValidateLink(BsonDocument order, BsonDocument checkout)
    {
        if (TransactionCollections.Purpose(order) != TransactionCollections.Purpose(checkout))
            throw TransactionCollections.Invalid("Order 與 Checkout 的交易用途矛盾。");
    }

    private T? Map<T>(BsonDocument? doc) => doc == null ? default : database.Mapper.ToObject<T>(doc);

    // LiteDB 的 collection read version 各自取得；必須先持有共同名稱鎖，再讀任何交易資料。
    private void AcquireLock() => database.GetCollection(TransactionCollections.Lock).Query().ForUpdate().ToArray();
    private T Read<T>(Func<T> read)
    {
        using var scope = StorageTransaction.Begin(database);
        AcquireLock(); TransactionStorageSchema.RequireVersion(database); return read();
    }
    private void Write(Action write)
    {
        using var scope = StorageTransaction.Begin(database);
        AcquireLock(); TransactionStorageSchema.RequireVersion(database); write();
        scope.Commit();
    }
}
