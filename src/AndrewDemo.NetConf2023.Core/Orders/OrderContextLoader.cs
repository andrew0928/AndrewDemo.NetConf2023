using AndrewDemo.NetConf2023.Abstract.Contexts;
using AndrewDemo.NetConf2023.Core.Contexts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AndrewDemo.NetConf2023.Core.Orders;

/// <summary>事件從已提交的 Order 恢復用途；不讀買方、白名單或原 request 的用途。</summary>
public sealed class OrderContextLoader(IShopDatabaseContext database, IIdentityContext identity,
    TransactionContext transaction, ILogger<OrderContextLoader>? logger = null)
{
    private TransactionSnapshot? _loaded;

    public void Bind(int orderId)
    {
        _loaded = null;
        transaction.Clear();
        if (identity.Current.Kind != IdentityKind.System)
            throw Invalid(orderId, "訂單事件缺少 System identity。");
        if (orderId <= 0) throw Invalid(orderId, "訂單事件 ID 無效。");
        var stored = database.Database.GetCollection("orders").FindById(orderId);
        if (stored == null) throw Invalid(orderId, "訂單事件找不到保存的 Order。");
        var purpose = stored[nameof(Order.IsTest)];
        if (stored.ContainsKey(nameof(Order.IsTest)) && !purpose.IsBoolean)
            throw Invalid(orderId, "Order 保存的用途無法判讀。");
        // 既定舊資料缺欄位為 false；明確 null／其他型別不是可用的保存用途。
        _loaded = new TransactionSnapshot(TransactionKind.Order, orderId,
            stored.ContainsKey(nameof(Order.IsTest)) && purpose.AsBoolean);
        transaction.Bind(_loaded);
    }

    // 同一事件內不得以另一份快照替換載入的用途；巢狀事件使用另一個 scoped loader。
    internal TransactionSnapshot RequireCurrent(int orderId)
    {
        if (_loaded == null || _loaded.Id != orderId || !ReferenceEquals(_loaded, transaction.Current)
            || identity.Current.Kind != IdentityKind.System)
            throw Invalid(orderId, "訂單事件的已載入用途遺失或遭替換。");
        return _loaded;
    }

    private ExecutionContextInvalidException Invalid(int orderId, string reason)
    {
        (logger ?? NullLogger<OrderContextLoader>.Instance).LogError(
            "execution_context_invalid：Order {OrderId}，{Reason}", orderId, reason);
        return new ExecutionContextInvalidException(reason);
    }
}
