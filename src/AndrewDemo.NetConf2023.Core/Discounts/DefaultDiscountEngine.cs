using System;
using System.Collections.Generic;
using System.Linq;
using AndrewDemo.NetConf2023.Abstract.Discounts;
using AndrewDemo.NetConf2023.Abstract.Shops;

namespace AndrewDemo.NetConf2023.Core.Discounts
{
    public sealed class DefaultDiscountEngine : IDiscountEngine
    {
        private readonly IReadOnlyList<IDiscountRulePlugin> _rules;
        private readonly IShopRuntimeContext _shopRuntimeContext;

        public DefaultDiscountEngine(IShopRuntimeContext shopRuntimeContext, IEnumerable<IDiscountRulePlugin> rules)
        {
            _shopRuntimeContext = shopRuntimeContext ?? throw new ArgumentNullException(nameof(shopRuntimeContext));
            _rules = (rules ?? throw new ArgumentNullException(nameof(rules))).ToList();
        }

        public IReadOnlyList<DiscountApplication> Evaluate(DiscountEvaluationContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var enabledRuleIds = new HashSet<string>(
                _shopRuntimeContext.Manifest.EnabledDiscountRuleIds ?? Enumerable.Empty<string>(),
                StringComparer.OrdinalIgnoreCase);

            if (enabledRuleIds.Count == 0)
            {
                return Array.Empty<DiscountApplication>();
            }

            var applications = new List<DiscountApplication>();

            foreach (var rule in _rules
                .Where(x => enabledRuleIds.Contains(x.RuleId))
                .OrderBy(x => x.Priority)
                .ThenBy(x => x.RuleId, StringComparer.OrdinalIgnoreCase))
            {
                applications.AddRange(rule.Evaluate(context));
            }

            return applications;
        }
    }
}
