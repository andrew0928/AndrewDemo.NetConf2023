using AndrewDemo.NetConf2023.Abstract.Contexts;

namespace AndrewDemo.NetConf2023.Core.Contexts;

/// <summary>
/// 由執行邊界綁定的交易 context；綁定範圍獨立於身分。
/// </summary>
public sealed class TransactionContext : ITransactionContext
{
    public TransactionSnapshot? Current { get; private set; }

    public void Bind(TransactionSnapshot transaction)
    {
        ArgumentNullException.ThrowIfNull(transaction);
        Current = transaction;
    }

    public void Clear()
    {
        Current = null;
    }
}
