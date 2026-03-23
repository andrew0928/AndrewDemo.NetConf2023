using System.Collections.Generic;
using AndrewDemo.NetConf2023.Abstract.Carts;

namespace AndrewDemo.NetConf2023.Abstract.Discounts
{
    public interface IDiscountRule
    {
        string RuleId { get; }
        int Priority { get; }
        IReadOnlyList<DiscountRecord> Evaluate(CartContext context);
    }

    public sealed class DiscountRecord
    {
        public string RuleId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }
}
