using System;
using System.Linq;
using AndrewDemo.NetConf2023.Abstract.Carts;
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
            Assert.Equal(DiscountRecordKind.Discount, discounts[0].Kind);
            Assert.Equal(-20m, discounts[0].Amount);
            Assert.Single(discounts[0].RelatedLineIds);
            Assert.Equal(cart.LineItems[0].LineId, discounts[0].RelatedLineIds[0]);
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
            Assert.Equal(DiscountRecordKind.Discount, discounts[0].Kind);
            Assert.Equal(-20m, discounts[0].Amount);
            Assert.Equal(2, discounts[0].RelatedLineIds.Count);
            Assert.Contains(cart.LineItems[0].LineId, discounts[0].RelatedLineIds);
            Assert.Contains(cart.LineItems[1].LineId, discounts[0].RelatedLineIds);
        }

        [Fact]
        public void Rule_CanReturnHintRecordWithRelatedLineIds()
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

            var manifest = new ShopManifest
            {
                ShopId = "default",
                DatabaseFilePath = "shop-database.db",
                ProductServiceId = DefaultProductService.ServiceId,
                EnabledDiscountRuleIds =
                {
                    HintOnlyDiscountRule.TestRuleId
                }
            };

            var engine = CreateEngine(manifest, new HintOnlyDiscountRule());
            var cartContext = CartContextFactory.Create(manifest, cart, consumer: null, new DefaultProductService(Context));

            var discounts = engine.Evaluate(cartContext);

            Assert.Single(discounts);
            Assert.Equal(DiscountRecordKind.Hint, discounts[0].Kind);
            Assert.Equal(0m, discounts[0].Amount);
            Assert.Single(discounts[0].RelatedLineIds);
            Assert.Equal(cart.LineItems[0].LineId, discounts[0].RelatedLineIds[0]);
        }

        [Fact]
        public void Rule_WhenHintCarriesNonZeroAmount_ThrowsInvalidOperationException()
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

            var manifest = new ShopManifest
            {
                ShopId = "default",
                DatabaseFilePath = "shop-database.db",
                ProductServiceId = DefaultProductService.ServiceId,
                EnabledDiscountRuleIds =
                {
                    InvalidHintAmountRule.TestRuleId
                }
            };

            var engine = CreateEngine(manifest, new InvalidHintAmountRule());
            var cartContext = CartContextFactory.Create(manifest, cart, consumer: null, new DefaultProductService(Context));

            var ex = Assert.Throws<InvalidOperationException>(() => engine.Evaluate(cartContext));
            Assert.Contains(InvalidHintAmountRule.TestRuleId, ex.Message);
        }

        private static DiscountEngine CreateEngine(ShopManifest manifest, params IDiscountRule[] rules)
        {
            var enabledRules = rules.Where(rule =>
                manifest.EnabledDiscountRuleIds.Contains(rule.RuleId, StringComparer.OrdinalIgnoreCase));

            return new DiscountEngine(enabledRules);
        }

        private sealed class HintOnlyDiscountRule : IDiscountRule
        {
            public const string TestRuleId = "test-hint-rule";

            public string RuleId => TestRuleId;

            public int Priority => 10;

            public IReadOnlyList<DiscountRecord> Evaluate(CartContext context)
            {
                return new[]
                {
                    new DiscountRecord
                    {
                        RuleId = RuleId,
                        Kind = DiscountRecordKind.Hint,
                        Name = "差一點達標",
                        Description = "再多買一些就能符合優惠",
                        Amount = 0m,
                        RelatedLineIds = context.LineItems.Select(x => x.LineId).ToList()
                    }
                };
            }
        }

        private sealed class InvalidHintAmountRule : IDiscountRule
        {
            public const string TestRuleId = "test-invalid-hint-amount-rule";

            public string RuleId => TestRuleId;

            public int Priority => 10;

            public IReadOnlyList<DiscountRecord> Evaluate(CartContext context)
            {
                return new[]
                {
                    new DiscountRecord
                    {
                        RuleId = RuleId,
                        Kind = DiscountRecordKind.Hint,
                        Name = "非法提示",
                        Description = "提示不可帶金額",
                        Amount = -100m,
                        RelatedLineIds = context.LineItems.Select(x => x.LineId).ToList()
                    }
                };
            }
        }
    }
}
