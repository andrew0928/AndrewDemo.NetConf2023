namespace AndrewDemo.NetConf2023.Core.Testing;

/// <summary>
/// 保存測試會員與付款來源的白名單；建立後唯讀，可由不同執行範圍共用。
/// </summary>
public sealed class TestAllowlist
{
    // 標準示範站的預設清單；兩種 ID 各自判定，不共用同一個集合。
    private static readonly int[] DefaultMemberIds = [900001];
    private static readonly int[] DefaultPaymentIds = [990001];

    private readonly HashSet<int> _memberIds;
    private readonly HashSet<int> _paymentIds;

    public TestAllowlist() : this(DefaultMemberIds, DefaultPaymentIds)
    {
    }

    /// <summary>
    /// 複製指定清單，避免呼叫端後續修改集合而改變正在使用的白名單。
    /// </summary>
    public TestAllowlist(IEnumerable<int> memberIds, IEnumerable<int> paymentIds)
    {
        ArgumentNullException.ThrowIfNull(memberIds);
        ArgumentNullException.ThrowIfNull(paymentIds);
        _memberIds = new HashSet<int>(memberIds);
        _paymentIds = new HashSet<int>(paymentIds);
    }

    public bool IsTestMember(int memberId)
    {
        return _memberIds.Contains(memberId);
    }

    public bool IsTestPayment(int paymentId)
    {
        return _paymentIds.Contains(paymentId);
    }
}
