namespace AndrewDemo.NetConf2023.Abstract.Contexts
{
    public enum TransactionKind
    {
        Checkout = 0,
        Order = 1
    }

    /// <summary>
    /// 由持久化 Checkout 或 Order 恢復的不可變交易快照。
    /// </summary>
    public sealed class TransactionSnapshot
    {
        public TransactionKind Kind { get; }
        public int Id { get; }
        public bool IsTest { get; }

        public TransactionSnapshot(TransactionKind kind, int id, bool isTest)
        {
            Kind = kind;
            Id = id;
            IsTest = isTest;
        }
    }

    /// <summary>
    /// 目前處理的單筆交易；沒有交易時 Current 為 null。
    /// </summary>
    public interface ITransactionContext
    {
        TransactionSnapshot? Current { get; }
    }
}
