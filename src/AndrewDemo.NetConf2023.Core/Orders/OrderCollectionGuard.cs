using System.Reflection;
using System.Runtime.ExceptionServices;
using LiteDB;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AndrewDemo.NetConf2023.Core.Orders;

/// <summary>訂單只能新增與讀取；合法欄位異動由 Core 的明確操作執行，拒絕整筆覆寫用途。</summary>
public class OrderCollectionGuard : DispatchProxy
{
    private ILiteCollection<Order> _inner = null!;
    private ILogger _logger = NullLogger.Instance;

    internal static ILiteCollection<Order> Wrap(ILiteCollection<Order> inner, ILogger? logger)
    {
        var proxy = Create<ILiteCollection<Order>, OrderCollectionGuard>();
        ((OrderCollectionGuard)proxy)._inner = inner;
        ((OrderCollectionGuard)proxy)._logger = logger ?? NullLogger.Instance;
        return proxy;
    }

    protected override object? Invoke(MethodInfo? targetMethod, object?[]? args)
    {
        var method = targetMethod ?? throw new ArgumentNullException(nameof(targetMethod));
        if (method.Name.StartsWith("Update", StringComparison.Ordinal)
            || method.Name.StartsWith("Upsert", StringComparison.Ordinal)
            || method.Name.StartsWith("Delete", StringComparison.Ordinal))
        {
            _logger.LogWarning("拒絕 Order {Operation}；必須透過 Core 操作以保留既定用途與交易內容。", method.Name);
            throw new InvalidOperationException("訂單異動必須透過 Core 操作，不能整筆覆寫或刪除後重建用途。");
        }
        try { return method.Invoke(_inner, args); }
        catch (TargetInvocationException ex) when (ex.InnerException != null)
        {
            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
            throw;
        }
    }
}
