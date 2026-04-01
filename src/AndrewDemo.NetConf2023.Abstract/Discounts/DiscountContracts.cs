using System.Collections.Generic;
using AndrewDemo.NetConf2023.Abstract.Carts;

namespace AndrewDemo.NetConf2023.Abstract.Discounts
{
    public enum DiscountRecordKind
    {
        Discount = 0,
        Hint = 1
    }

    public interface IDiscountRule
    {
        string RuleId { get; }
        int Priority { get; }
        IReadOnlyList<DiscountRecord> Evaluate(CartContext context);
    }

    public sealed class DiscountRecord
    {
        public string RuleId { get; set; } = string.Empty;
        public DiscountRecordKind Kind { get; set; } = DiscountRecordKind.Discount;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public List<string> RelatedLineIds { get; set; } = new List<string>();
    }
}
