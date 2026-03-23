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
            var lineItem = context.LineItems
                .Where(x => x.ProductId == "1" && x.Quantity >= 2)
                .FirstOrDefault();

            if (lineItem == null)
            {
                return new List<DiscountRecord>();
            }

            var records = new List<DiscountRecord>();
            var unitPrice = lineItem.UnitPrice
                ?? throw new InvalidOperationException($"unit price is required for product {lineItem.ProductId}");
            var productName = lineItem.ProductName
                ?? throw new InvalidOperationException($"product name is required for product {lineItem.ProductId}");

            for (int index = 1; index <= lineItem.Quantity; index++)
            {
                if (index % 2 != 0)
                {
                    continue;
                }

                records.Add(new DiscountRecord
                {
                    RuleId = RuleId,
                    Name = "第二件六折",
                    Description = $"符合商品: {productName} x 2",
                    Amount = unitPrice * -0.4m
                });
            }

            return records;
        }
    }
}
