using System.Linq;
using AndrewDemo.NetConf2023.Core;
using Xunit;

namespace AndrewDemo.NetConf2023.Core.Tests
{
    public class CartPersistenceTests
    {
        [Fact]
        public void CartAddProducts_PersistsAndCanBeReadBack()
        {
            decimal price = 99.5m;
            int quantity = 2;
            int productId = TestDataFactory.CreateProduct(price);

            var cart = Cart.Create();
            cart.AddProducts(productId, quantity);

            var reloaded = Cart.Get(cart.Id);
            Assert.NotNull(reloaded);

            var lineItems = reloaded!.LineItems.ToList();
            Assert.Single(lineItems);
            Assert.Equal(productId, lineItems[0].ProductId);
            Assert.Equal(quantity, lineItems[0].Qty);

            Assert.Equal(price * quantity, reloaded.EstimatePrice());
        }
    }
}
