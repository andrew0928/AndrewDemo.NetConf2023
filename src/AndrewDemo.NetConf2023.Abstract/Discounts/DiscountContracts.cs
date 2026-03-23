using System;
using System.Collections.Generic;

namespace AndrewDemo.NetConf2023.Abstract.Discounts
{
    public interface IDiscountEngine
    {
        IReadOnlyList<DiscountApplication> Evaluate(DiscountEvaluationContext context);
    }

    public interface IDiscountRulePlugin
    {
        string RuleId { get; }
        int Priority { get; }
        IReadOnlyList<DiscountApplication> Evaluate(DiscountEvaluationContext context);
    }

    public sealed class DiscountEvaluationContext
    {
        public string ShopId { get; set; } = string.Empty;
        public DiscountConsumerSnapshot? Consumer { get; set; }
        public IReadOnlyList<DiscountCartLine> CartLines { get; set; } = Array.Empty<DiscountCartLine>();
    }

    public sealed class DiscountConsumerSnapshot
    {
        public int MemberId { get; set; }
        public string? MemberName { get; set; }
    }

    public sealed class DiscountCartLine
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
    }

    public sealed class DiscountApplication
    {
        public string RuleId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }
}
