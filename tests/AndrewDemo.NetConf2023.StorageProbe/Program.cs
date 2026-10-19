using AndrewDemo.NetConf2023.Abstract.Orders;
using AndrewDemo.NetConf2023.Abstract.Shops;
using AndrewDemo.NetConf2023.Core;
using AndrewDemo.NetConf2023.Core.Checkouts;
using AndrewDemo.NetConf2023.Core.Discounts;
using AndrewDemo.NetConf2023.Core.Products;
using LiteDB;

if (args[0] == "--inspect")
{
    // smoke read-back 不輸出 Token；僅輸出交易位置、用途及庫存。
    using var inspectionDb = new LiteDatabase(new ConnectionString { Filename = args[1], Connection = ConnectionType.Shared });
    var names = new[] { "orders", "test_orders", "checkout_transactions", "test_checkout_transactions",
        "archived_checkout_transactions", "test_archived_checkout_transactions" };
    var report = new
    {
        Collections = names.ToDictionary(name => name, name => inspectionDb.GetCollection(name).FindAll()
            .Select(doc => new { Id = doc["_id"].AsInt32, IsTest = doc["IsTest"].AsBoolean,
                TotalPrice = doc["TotalPrice"].IsNull ? (decimal?)null : doc["TotalPrice"].AsDecimal,
                CheckoutTransactionId = doc["CheckoutTransactionId"].IsNull ? (int?)null : doc["CheckoutTransactionId"].AsInt32,
                PaymentId = doc["PaymentId"].IsNull ? (int?)null : doc["PaymentId"].AsInt32 }).ToArray()),
        Inventory = inspectionDb.GetCollection("inventory_records").FindAll()
            .ToDictionary(doc => doc["_id"].AsString, doc => doc["AvailableQuantity"].AsInt32),
        Members = inspectionDb.GetCollection("members").Count(),
        Tokens = inspectionDb.GetCollection("member_tokens").Count(),
        Version = inspectionDb.GetCollection("transaction_storage").FindById(1)?["Version"].AsInt32
    };
    Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(report));
    return;
}

// 只供跨程序測試的 writer；不隨正式 host 部署。
using var database = new ShopDatabaseContext(new() { ConnectionString = $"Filename={args[0]};Connection=Shared" });
File.WriteAllText(args[1], "ready");
using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(45));
while (!File.Exists(args[2])) await Task.Delay(10, timeout.Token);
foreach (var record in database.Transactions.Checkouts("all"))
{
    var service = new CheckoutService(database, new DiscountEngine([]), new NoEvents(),
        new DefaultProductService(database), new ShopManifest { ShopId = "probe" }, TimeProvider.System);
    var result = await service.CompleteAsync(new() { TransactionId = record.TransactionId,
        RequestMember = database.Members.FindById(record.MemberId) });
    if (result.Status != CheckoutCompleteStatus.Succeeded) throw new InvalidOperationException(result.ErrorMessage);
}
Console.WriteLine("completed");

sealed class NoEvents : IOrderEventDispatcher
{
    public void Dispatch(OrderCompletedEvent orderEvent) { }
    public void Dispatch(OrderCancelledEvent orderEvent) { }
}
