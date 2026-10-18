namespace AndrewDemo.NetConf2023.Core.AdminQueries;

/// <summary>由 host 驗證後傳入的唯讀條件；日期已換成 UTC。</summary>
public sealed record AdminQueryOptions
{
    public string Purpose { get; init; } = "normal";
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 50;
    public int? MemberId { get; init; }
    public string? Name { get; init; }
    public DateTime? From { get; init; }
    public DateTime? To { get; init; }
    public string State { get; init; } = "all";
    public string TimeField { get; init; } = "createdAt";
}

public sealed record AdminOrderRow(int Id, int? MemberId, string? MemberName, bool IsTest,
    DateTime? CreatedAt, decimal? TotalPrice, string FulfillmentStatus, int? CheckoutTransactionId);
public sealed record AdminCheckoutRow(int TransactionId, int? MemberId, int? CartId, int? PaymentId,
    bool IsTest, string State, DateTime? CreatedAt, DateTime? ClosedAt, decimal? TotalPrice, string Storage);
public sealed record AdminMemberRow(int Id, string? Name, bool IsTest);
public sealed record AdminPage<T>(DateTime QueriedAt, int TotalCount, int ExcludedUnknownTimeCount, IReadOnlyList<T> Items);
public sealed record AdminOrderResult(AdminPage<AdminOrderRow> Page, decimal? TotalAmount,
    decimal KnownTotalAmount, int UnknownAmountCount);

/// <summary>資料無法可靠判讀，不能用預設值偽造查核結果。</summary>
public sealed class AdminDataInvalidException(string message) : InvalidOperationException(message);
