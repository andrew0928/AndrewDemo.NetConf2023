using AndrewDemo.NetConf2023.Abstract.Shops;
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

            var runtime = new ShopRuntimeContext(new ShopManifest
            {
                ShopId = "default",
                DatabaseFilePath = "shop-database.db",
                EnabledDiscountRuleIds =
                {
                    Product1SecondItemDiscountRulePlugin.BuiltInRuleId
                }
            });

            var engine = new DefaultDiscountEngine(runtime, new[] { new Product1SecondItemDiscountRulePlugin() });
            var evaluationContext = DiscountEvaluationContextFactory.Create(runtime.ShopId, cart, consumer: null, Context);

            var discounts = engine.Evaluate(evaluationContext);

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

            var runtime = new ShopRuntimeContext(new ShopManifest
            {
                ShopId = "default",
                DatabaseFilePath = "shop-database.db"
            });

            var engine = new DefaultDiscountEngine(runtime, new[] { new Product1SecondItemDiscountRulePlugin() });
            var evaluationContext = DiscountEvaluationContextFactory.Create(runtime.ShopId, cart, consumer: null, Context);

            var discounts = engine.Evaluate(evaluationContext);

            Assert.Empty(discounts);
        }
    }
}
