using AndrewDemo.NetConf2023.Abstract.Contexts;

namespace AndrewDemo.NetConf2023.Core.Contexts;

/// <summary>
/// 由執行邊界綁定的交易 context；綁定範圍獨立於身分。
/// </summary>
public sealed class TransactionContext : ITransactionContext
{
    public TransactionSnapshot? Current { get; private set; }
    // 同一 scope 內清除／替換快照不能把交易效果改走獨立維護入口。
    internal bool HasObservedTestTransaction { get; private set; }

    public void Bind(TransactionSnapshot transaction)
    {
        ArgumentNullException.ThrowIfNull(transaction);
        Current = transaction;
        HasObservedTestTransaction |= transaction.IsTest;
    }

    public void Clear()
    {
        Current = null;
    }
}
