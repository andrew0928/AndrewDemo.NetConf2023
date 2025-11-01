using System.Linq;
using AndrewDemo.NetConf2023.Core;
using Xunit;

namespace AndrewDemo.NetConf2023.Core.Tests
{
    public class CartPersistenceTests : ShopDatabaseTestBase
    {
        [Fact]
        public void CartAddProducts_PersistsAndCanBeReadBack()
        {
            decimal price = 99.5m;
            int quantity = 2;
            int productId = TestDataFactory.CreateProduct(Context, price);

            var cart = new Cart();
            Context.Carts.Insert(cart);
            cart.AddProducts(productId, quantity);
            Context.Carts.Update(cart); // 明確呼叫持久化

            var reloaded = Context.Carts.FindById(cart.Id);
            Assert.NotNull(reloaded);

            var lineItems = reloaded!.LineItems.ToList();
            Assert.Single(lineItems);
            Assert.Equal(productId, lineItems[0].ProductId);
            Assert.Equal(quantity, lineItems[0].Qty);

            Assert.Equal(price * quantity, reloaded.EstimatePrice(Context));
        }
    }
}
