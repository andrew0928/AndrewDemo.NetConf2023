using AndrewDemo.NetConf2023.Core.Operations;
using AndrewDemo.NetConf2023.Core.Orders;
using AndrewDemo.NetConf2023.Abstract.Contexts;
using AndrewDemo.NetConf2023.Abstract.Operations;
using AndrewDemo.NetConf2023.Core.Contexts;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AndrewDemo.NetConf2023.Core.Effects;

/// <summary>標準流程的效果邊界；呼叫端先完成驗證，再依指定來源執行或略過寫入。</summary>
public sealed class ProtectedWriteExecutor(IIdentityContext identity, ITransactionContext transaction,
    OperationEffects effects, ILogger? logger = null, OrderContextLoader? orders = null, OperationExecution? operation = null)
{
    private readonly ILogger _logger = logger ?? NullLogger.Instance;

    public void ForIdentity(string effectCode, string targetType, string targetId, object requestedChange,
        Action<IdentitySnapshot> validate, Action write)
    {
        ArgumentNullException.ThrowIfNull(validate);
        var snapshot = identity.Current;
        if (snapshot.Kind == IdentityKind.Anonymous || (snapshot.Kind == IdentityKind.Member && snapshot.MemberId is not > 0))
            throw ContextError("受保護維護操作缺少已初始化的執行身分。");
        Execute(snapshot.IsTest, snapshot, null, effectCode, targetType, targetId, requestedChange,
            () => validate(snapshot), write);
    }

    public void ForCheckout(int checkoutId, string effectCode, string targetType, string targetId,
        object requestedChange, Action write)
    {
        var snapshot = transaction.Current;
        if (snapshot == null || snapshot.Kind != TransactionKind.Checkout || snapshot.Id != checkoutId || checkoutId <= 0)
            throw ContextError("扣庫存時缺少相符的 Checkout context。");
        // CheckoutService 已在同一資料庫交易內驗證全部商品與庫存；此處不能重新套用會員用途。
        Execute(snapshot.IsTest, identity.Current, snapshot, effectCode, targetType, targetId,
            requestedChange, () => { }, write);
    }

    /// <summary>事件效果以保存的 Order 為準；System 的正式身分不解除 test 限制。</summary>
    public void ForOrder(int orderId, string effectCode, string targetType, string targetId,
        object requestedChange, Action validate, Action write)
    {
        if (orders == null) throw ContextError("事件效果缺少 Order 載入邊界。");
        var snapshot = orders.RequireCurrent(orderId);
        Execute(snapshot.IsTest, identity.Current, snapshot, effectCode, targetType, targetId,
            requestedChange, validate, write);
    }

    private void Execute(bool isTest, IdentitySnapshot identitySnapshot, TransactionSnapshot? transactionSnapshot,
        string effectCode, string targetType, string targetId, object requestedChange, Action validate, Action write)
    {
        ArgumentNullException.ThrowIfNull(validate);
        ArgumentNullException.ThrowIfNull(write);
        var effect = new SkippedEffect(effectCode, targetType, targetId, "test_behavior");
        validate();
        if (!isTest)
        {
            write();
            return;
        }
        effects.Record(effect);
        if (operation != null)
        {
            operation.RecordSkipped(effect, identitySnapshot, transactionSnapshot, requestedChange);
            return;
        }
        _logger.LogInformation(
            "TiP 略過 {EffectCode}，{TargetType}/{TargetId}，原因 {ReasonCode}；身分 {IdentityKind}/{MemberId}；交易 {TransactionKind}/{TransactionId}/{TransactionIsTest}；要求 {RequestedChange}",
            effect.EffectCode, effect.TargetType, effect.TargetId, effect.ReasonCode,
            identitySnapshot.Kind, identitySnapshot.MemberId, transactionSnapshot?.Kind,
            transactionSnapshot?.Id, transactionSnapshot?.IsTest, requestedChange);
    }

    private ExecutionContextInvalidException ContextError(string message)
    {
        _logger.LogError("execution_context_invalid：{Reason}", message);
        return new ExecutionContextInvalidException(message);
    }
}
