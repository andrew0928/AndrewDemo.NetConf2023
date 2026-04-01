using System.Collections.Generic;
using System.Linq;
using AndrewDemo.NetConf2023.Abstract.Carts;
using AndrewDemo.NetConf2023.Abstract.Discounts;

namespace AndrewDemo.NetConf2023.Core.Discounts
{
    public sealed class Product1SecondItemDiscountRule : IDiscountRule
    {
        public const string BuiltInRuleId = "product-1-second-item-40-off";

        public string RuleId => BuiltInRuleId;

        public int Priority => 100;

        public IReadOnlyList<DiscountRecord> Evaluate(CartContext context)
        {
            var matchedLines = context.LineItems
                .Where(x => x.ProductId == "1")
                .ToList();

            var totalQuantity = matchedLines.Sum(x => x.Quantity);
            if (totalQuantity < 2)
            {
                return new List<DiscountRecord>();
            }

            var sampleLine = matchedLines.FirstOrDefault();
            if (sampleLine == null)
            {
                return new List<DiscountRecord>();
            }

            var records = new List<DiscountRecord>();
            var unitPrice = sampleLine.UnitPrice
                ?? throw new InvalidOperationException($"unit price is required for product {sampleLine.ProductId}");
            var productName = sampleLine.ProductName
                ?? throw new InvalidOperationException($"product name is required for product {sampleLine.ProductId}");
            var relatedLineIds = matchedLines
                .Select(x => x.LineId)
                .Distinct()
                .ToList();

            for (int index = 1; index <= totalQuantity; index++)
            {
                if (index % 2 != 0)
                {
                    continue;
                }

                records.Add(new DiscountRecord
                {
                    RuleId = RuleId,
                    Kind = DiscountRecordKind.Discount,
                    Name = "第二件六折",
                    Description = $"符合商品: {productName} x 2",
                    Amount = unitPrice * -0.4m,
                    RelatedLineIds = relatedLineIds
                });
            }

            return records;
        }
    }
}
