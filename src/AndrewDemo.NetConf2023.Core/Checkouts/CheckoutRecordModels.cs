using System.Collections.Generic;

namespace AndrewDemo.NetConf2023.Core.Checkouts
{
    public enum CheckoutState
    {
        // 舊資料沒有狀態時，不推定為已完成或可繼續執行。
        Unknown = 0,
        Created = 1,
        Completed = 2,
        Cancelled = 3
    }

    /// <summary>
    /// 建立 Checkout 時固定的交易明細；只保存識別與成交條件，不複製商品資訊。
    /// </summary>
    public sealed class CheckoutProductLine
    {
        public string LineId { get; init; } = string.Empty;
        public string? ParentLineId { get; init; }
        public string ProductId { get; init; } = string.Empty;
        public string? SkuId { get; init; }
        public decimal UnitPrice { get; init; }
        public int Quantity { get; init; }
        public decimal LineAmount { get; init; }
    }

    /// <summary>
    /// 建立時已套用的折扣結果；名稱與說明屬折扣紀錄，不是商品主檔快照。
    /// </summary>
    public sealed class CheckoutDiscountLine
    {
        public string RuleId { get; init; } = string.Empty;
        public string Name { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public decimal Amount { get; init; }
        public List<string> RelatedLineIds { get; init; } = new();
    }
}
