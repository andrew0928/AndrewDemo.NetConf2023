using System;
using System.Linq;
using AndrewDemo.NetConf2023.Abstract.Discounts;
using AndrewDemo.NetConf2023.Abstract.Products;
using AndrewDemo.NetConf2023.Abstract.Shops;
using AndrewDemo.NetConf2023.Core;
using AndrewDemo.NetConf2023.Core.Discounts;
using AndrewDemo.NetConf2023.Core.Products;

namespace AndrewDemo.NetConf2023.Core.Tests
{
    public class DiscountEngineTests : ShopDatabaseTestBase
    {
        [Fact]
        public void EnabledRule_IsAppliedToMatchingCart()
        {
            Context.Products.Upsert(new Product
            {
                Id = "1",
                Name = "Test Beer",
                Price = 50m,
                IsPublished = true
            });

            var cart = new Cart();
            cart.AddProducts("1", 2);

            var manifest = new ShopManifest
            {
                ShopId = "default",
                DatabaseFilePath = "shop-database.db",
                ProductServiceId = DefaultProductService.ServiceId,
                EnabledDiscountRuleIds =
                {
                    Product1SecondItemDiscountRule.BuiltInRuleId
                }
            };

            var engine = CreateEngine(manifest, new Product1SecondItemDiscountRule());
            var cartContext = CartContextFactory.Create(manifest, cart, consumer: null, new DefaultProductService(Context));

            var discounts = engine.Evaluate(cartContext);

            Assert.Single(discounts);
            Assert.Equal(-20m, discounts[0].Amount);
        }

        [Fact]
        public void DisabledRule_IsIgnoredEvenWhenCartMatches()
        {
            Context.Products.Upsert(new Product
            {
                Id = "1",
                Name = "Test Beer",
                Price = 50m,
                IsPublished = true
            });

            var cart = new Cart();
            cart.AddProducts("1", 2);

            var manifest = new ShopManifest
            {
                ShopId = "default",
                DatabaseFilePath = "shop-database.db",
                ProductServiceId = DefaultProductService.ServiceId
            };

            var engine = CreateEngine(manifest, new Product1SecondItemDiscountRule());
            var cartContext = CartContextFactory.Create(manifest, cart, consumer: null, new DefaultProductService(Context));

            var discounts = engine.Evaluate(cartContext);

            Assert.Empty(discounts);
        }

        [Fact]
        public void EnabledRule_IsAppliedAcrossDistinctLinesWithSameProduct()
        {
            Context.Products.Upsert(new Product
            {
                Id = "1",
                Name = "Test Beer",
                Price = 50m,
                IsPublished = true
            });

            var cart = new Cart();
            cart.AddProducts("1", 1);
            cart.AddProducts("1", 1);

            var manifest = new ShopManifest
            {
                ShopId = "default",
                DatabaseFilePath = "shop-database.db",
                ProductServiceId = DefaultProductService.ServiceId,
                EnabledDiscountRuleIds =
                {
                    Product1SecondItemDiscountRule.BuiltInRuleId
                }
            };

            var engine = CreateEngine(manifest, new Product1SecondItemDiscountRule());
            var cartContext = CartContextFactory.Create(manifest, cart, consumer: null, new DefaultProductService(Context));

            var discounts = engine.Evaluate(cartContext);

            Assert.Single(discounts);
            Assert.Equal(-20m, discounts[0].Amount);
        }

        private static DiscountEngine CreateEngine(ShopManifest manifest, params IDiscountRule[] rules)
        {
            var enabledRules = rules.Where(rule =>
                manifest.EnabledDiscountRuleIds.Contains(rule.RuleId, StringComparer.OrdinalIgnoreCase));

            return new DiscountEngine(enabledRules);
        }
    }
}
