using System.Collections.Generic;
using System.Linq;
using AndrewDemo.NetConf2023.Abstract.Discounts;

namespace AndrewDemo.NetConf2023.Core.Discounts
{
    public sealed class Product1SecondItemDiscountRulePlugin : IDiscountRulePlugin
    {
        public const string BuiltInRuleId = "product-1-second-item-40-off";

        public string RuleId => BuiltInRuleId;

        public int Priority => 100;

        public IReadOnlyList<DiscountApplication> Evaluate(DiscountEvaluationContext context)
        {
            var lineItem = context.CartLines
                .Where(x => x.ProductId == 1 && x.Quantity >= 2)
                .FirstOrDefault();

            if (lineItem == null)
            {
                return new List<DiscountApplication>();
            }

            var applications = new List<DiscountApplication>();

            for (int index = 1; index <= lineItem.Quantity; index++)
            {
                if (index % 2 != 0)
                {
                    continue;
                }

                applications.Add(new DiscountApplication
                {
                    RuleId = RuleId,
                    Name = "第二件六折",
                    Description = $"符合商品: {lineItem.ProductName} x 2",
                    Amount = lineItem.UnitPrice * -0.4m
                });
            }

            return applications;
        }
    }
}
