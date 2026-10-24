using AndrewDemo.NetConf2023.Abstract.Contexts;
using AndrewDemo.NetConf2023.Abstract.Operations;
using AndrewDemo.NetConf2023.Abstract.Shops;
using AndrewDemo.NetConf2023.Core.Checkouts;
using AndrewDemo.NetConf2023.Core.Effects;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace AndrewDemo.NetConf2023.Core.Operations;

/// <summary>單次出口的結果與診斷快照；不依賴 HTTP，也不合併兩種 context。</summary>
public sealed class OperationExecution(IIdentityContext identity, ITransactionContext transaction,
    OperationEffects effects, ShopManifest? shop = null, ILogger<OperationExecution>? logger = null)
{
    private readonly ILogger _logger = logger ?? NullLogger<OperationExecution>.Instance;
    private readonly List<EffectLog> _pending = new();
    private bool _finished;
    private bool _trusted = true;
    public string TraceId { get; private set; } = Guid.NewGuid().ToString("N");
    public OperationResult? Result { get; private set; }
    public OperationOutcome? Outcome { get; private set; }
    public TransactionSnapshot? Transaction { get; private set; }
    public IdentitySnapshot? Identity { get; private set; }

    public void Begin(string? traceId = null)
    {
        if (traceId != null) TraceId = traceId;
        effects.BeginOperation();
    }

    /// <summary>失去可信用途時仍記錄失敗，但不以 false 捏造正式結果。</summary>
    public void InvalidatePurpose() => _trusted = false;

    internal void RecordSkipped(SkippedEffect effect, IdentitySnapshot actor, TransactionSnapshot? target, object change)
        => _pending.Add(new(effect, actor, target, change));

    public OperationResult? Complete(OperationOutcome outcome)
    {
        if (_finished) return Result;
        _finished = true;
        Outcome = outcome;
        Identity = _trusted ? identity.Current : null;
        Transaction = _trusted ? transaction.Current : null;
        Result = _trusted ? new OperationResult(Transaction?.IsTest ?? Identity!.IsTest, outcome, effects.Skipped) : null;
        foreach (var entry in _pending)
        {
            // 用效果判定當時的快照留下逐筆證據；後續失敗不能抹除已略過的步驟。
            _logger.LogInformation(
                "TiP 略過 {EffectCode}，{TargetType}/{TargetId}，原因 {ReasonCode}；TraceId={TraceId} ShopId={ShopId} IdentityKind={IdentityKind} MemberId={MemberId} TransactionKind={TransactionKind} TransactionId={TransactionId} TransactionIsTest={TransactionIsTest} Outcome={Outcome} RequestedChange={RequestedChange}",
                entry.Effect.EffectCode, entry.Effect.TargetType, entry.Effect.TargetId, entry.Effect.ReasonCode,
                TraceId, shop?.ShopId, entry.Identity.Kind, entry.Identity.MemberId, entry.Transaction?.Kind,
                entry.Transaction?.Id, entry.Transaction?.IsTest, outcome, entry.Change);
        }
        _logger.Log(outcome == OperationOutcome.Failed ? LogLevel.Error : LogLevel.Information,
            "操作結束 TraceId={TraceId} ShopId={ShopId} IdentityKind={IdentityKind} MemberId={MemberId} TransactionKind={TransactionKind} TransactionId={TransactionId} TransactionIsTest={TransactionIsTest} IsTestBehavior={IsTestBehavior} Outcome={Outcome} SkippedEffectCount={SkippedEffectCount}",
            TraceId, shop?.ShopId, Identity?.Kind, Identity?.MemberId, Transaction?.Kind, Transaction?.Id,
            Transaction?.IsTest, Result?.IsTestBehavior, outcome, effects.Skipped.Count);
        return Result;
    }

    public OperationResult? Fail(Exception error)
    {
        if (error is ExecutionContextInvalidException) InvalidatePurpose();
        // 只記錄類型；例外訊息可能包含呼叫者提供的秘密。
        _logger.Log(Classify(error) == OperationOutcome.Failed ? LogLevel.Error : LogLevel.Information,
            "操作異常 TraceId={TraceId} ShopId={ShopId} ErrorType={ErrorType}", TraceId, shop?.ShopId, error.GetType().Name);
        return Complete(Classify(error));
    }

    public static OperationOutcome Classify(Exception error) => error is
        CheckoutRejectedException or UnauthorizedAccessException or ArgumentException or KeyNotFoundException or FormatException
        ? OperationOutcome.Rejected : OperationOutcome.Failed;

    private sealed record EffectLog(SkippedEffect Effect, IdentitySnapshot Identity, TransactionSnapshot? Transaction, object Change);
}
