using LiteDB;

namespace AndrewDemo.NetConf2023.Core.Testing;

/// <summary>
/// 保存測試會員與付款來源的白名單；建立後唯讀，可由不同執行範圍共用。
/// </summary>
public sealed class TestAllowlist
{
    // 標準示範站的預設清單；名稱與付款 ID 各自判定，不共用同一個集合。
    private static readonly string[] DefaultMemberNames = ["test"];
    private static readonly int[] DefaultPaymentIds = [990001];

    private readonly string[] _memberNames;
    private readonly HashSet<int> _paymentIds;

    public TestAllowlist() : this(DefaultMemberNames, DefaultPaymentIds)
    {
    }

    /// <summary>
    /// 複製指定清單，避免呼叫端後續修改集合而改變正在使用的白名單。
    /// </summary>
    public TestAllowlist(IEnumerable<string> memberNames, IEnumerable<int> paymentIds)
    {
        ArgumentNullException.ThrowIfNull(memberNames);
        ArgumentNullException.ThrowIfNull(paymentIds);
        _memberNames = memberNames.ToArray();
        _paymentIds = new HashSet<int>(paymentIds);
    }

    /// <summary>會員服務傳入作用中資料庫的 collation，與名稱查找使用相同比對規則。</summary>
    public bool IsTestMember(string memberName, Collation? collation = null)
    {
        ArgumentNullException.ThrowIfNull(memberName);
        return _memberNames.Any(name => (collation ?? Collation.Default).Compare(name, memberName) == 0);
    }

    public bool IsTestPayment(int paymentId)
    {
        return _paymentIds.Contains(paymentId);
    }
}
