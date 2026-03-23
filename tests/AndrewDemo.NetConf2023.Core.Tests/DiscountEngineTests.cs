using System;
using System.Linq;
using AndrewDemo.NetConf2023.Abstract.Discounts;
using AndrewDemo.NetConf2023.Abstract.Shops;
using AndrewDemo.NetConf2023.Core;
using AndrewDemo.NetConf2023.Core.Discounts;

namespace AndrewDemo.NetConf2023.Core.Tests
{
    public class DiscountEngineTests : ShopDatabaseTestBase
    {
        [Fact]
        public void EnabledRule_IsAppliedToMatchingCart()
        {
            Context.Products.Upsert(new Product
            {
                Id = 1,
                Name = "Test Beer",
                Price = 50m
            });

            var cart = new Cart();
            cart.AddProducts(1, 2);

            var manifest = new ShopManifest
            {
                ShopId = "default",
                DatabaseFilePath = "shop-database.db",
                EnabledDiscountRuleIds =
                {
                    Product1SecondItemDiscountRule.BuiltInRuleId
                }
            };

            var engine = CreateEngine(manifest, new Product1SecondItemDiscountRule());
            var cartContext = CartContextFactory.Create(manifest, cart, consumer: null, Context);

            var discounts = engine.Evaluate(cartContext);

            Assert.Single(discounts);
            Assert.Equal(-20m, discounts[0].Amount);
        }

        [Fact]
        public void DisabledRule_IsIgnoredEvenWhenCartMatches()
        {
            Context.Products.Upsert(new Product
            {
                Id = 1,
                Name = "Test Beer",
                Price = 50m
            });

            var cart = new Cart();
            cart.AddProducts(1, 2);

            var manifest = new ShopManifest
            {
                ShopId = "default",
                DatabaseFilePath = "shop-database.db"
            };

            var engine = CreateEngine(manifest, new Product1SecondItemDiscountRule());
            var cartContext = CartContextFactory.Create(manifest, cart, consumer: null, Context);

            var discounts = engine.Evaluate(cartContext);

            Assert.Empty(discounts);
        }

        private static DiscountEngine CreateEngine(ShopManifest manifest, params IDiscountRule[] rules)
        {
            var enabledRules = rules.Where(rule =>
                manifest.EnabledDiscountRuleIds.Contains(rule.RuleId, StringComparer.OrdinalIgnoreCase));

            return new DiscountEngine(enabledRules);
        }
    }
}
