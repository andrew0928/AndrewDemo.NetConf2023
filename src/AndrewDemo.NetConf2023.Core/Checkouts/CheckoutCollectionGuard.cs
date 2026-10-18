using System.Reflection;
using System.Runtime.ExceptionServices;
using LiteDB;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AndrewDemo.NetConf2023.Core.Checkouts;

/// <summary>保留既有讀取入口；固定交易不允許覆寫；封存只允許首次 Insert，並拒絕刪除或重新啟用。</summary>
public class CheckoutCollectionGuard : DispatchProxy
{
    private ILogger _logger = NullLogger.Instance;
    private ILiteCollection<CheckoutTransactionRecord> _inner = null!;
    private ILiteCollection<CheckoutTransactionRecord>? _archive;

    internal static ILiteCollection<CheckoutTransactionRecord> Wrap(ILiteCollection<CheckoutTransactionRecord> inner, ILiteCollection<CheckoutTransactionRecord>? archive = null, ILogger? logger = null)
    {
        var proxy = Create<ILiteCollection<CheckoutTransactionRecord>, CheckoutCollectionGuard>();
        ((CheckoutCollectionGuard)proxy)._inner = inner;
        ((CheckoutCollectionGuard)proxy)._archive = archive;
        ((CheckoutCollectionGuard)proxy)._logger = logger ?? NullLogger.Instance;
        return proxy;
    }

    private InvalidOperationException Rejected(string reason)
    {
        _logger.LogWarning("拒絕 Checkout 寫入：{Reason}", reason);
        return new InvalidOperationException(reason);
    }

    private void CheckId(int id)
    {
        // LiteDB 的 AutoId 只看作用中分區；已有封存時必須由服務跨分區配發。
        if (id == 0 && _archive!.Count() > 0)
            throw Rejected("已有封存交易，必須透過 CheckoutService 配發跨分區 ID。");
        if (_archive!.FindById(id) != null)
            throw Rejected("已封存交易不可重新啟用或重用 ID。");
    }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        var method = targetMethod ?? throw new ArgumentNullException(nameof(targetMethod));
        if (method.Name.StartsWith("Update", StringComparison.Ordinal)
            || method.Name.StartsWith("Upsert", StringComparison.Ordinal)
            || (_archive == null && method.Name.StartsWith("Delete", StringComparison.Ordinal)))
            throw Rejected("Checkout 內容已固定；封存不允許覆寫、刪除或重新啟用。");
        if (_archive != null && method.Name.StartsWith("Insert", StringComparison.Ordinal))
        {
            foreach (var argument in args ?? [])
            {
                if (argument is CheckoutTransactionRecord record) CheckId(record.TransactionId);
                if (argument is BsonValue id && id.IsInt32) CheckId(id.AsInt32);
                if (argument is IEnumerable<CheckoutTransactionRecord>)
                    throw Rejected("Checkout 不接受批次寫入，請透過 CheckoutService 建立。");
            }
        }
        try { return method.Invoke(_inner, args); }
        catch (TargetInvocationException ex) when (ex.InnerException != null)
        {
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            throw;
        }
    }
}
