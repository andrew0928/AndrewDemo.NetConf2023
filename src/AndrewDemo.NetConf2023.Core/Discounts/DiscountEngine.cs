using System;
using System.Collections.Generic;
using System.Linq;
using AndrewDemo.NetConf2023.Abstract.Carts;
using AndrewDemo.NetConf2023.Abstract.Discounts;

namespace AndrewDemo.NetConf2023.Core.Discounts
{
    public sealed class DiscountEngine
    {
        private readonly IReadOnlyList<IDiscountRule> _rules;

        public DiscountEngine(IEnumerable<IDiscountRule> rules)
        {
            _rules = (rules ?? throw new ArgumentNullException(nameof(rules))).ToList();
        }

        public IReadOnlyList<DiscountRecord> Evaluate(CartContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            if (_rules.Count == 0)
            {
                return Array.Empty<DiscountRecord>();
            }

            var records = new List<DiscountRecord>();

            foreach (var rule in _rules
                .OrderBy(x => x.Priority)
                .ThenBy(x => x.RuleId, StringComparer.OrdinalIgnoreCase))
            {
                records.AddRange(rule.Evaluate(context));
            }

            return records;
        }
    }
}
