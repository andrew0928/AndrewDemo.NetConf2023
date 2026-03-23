using System.Linq;
using AndrewDemo.NetConf2023.Abstract.Shops;
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
            string productId = TestDataFactory.CreateProduct(Context, price);

            var cart = new Cart();
            Context.Carts.Insert(cart);
            cart.AddProducts(productId, quantity);
            Context.Carts.Update(cart); // 明確呼叫持久化

            var reloaded = Context.Carts.FindById(cart.Id);
            Assert.NotNull(reloaded);

            var lineItems = reloaded!.LineItems.ToList();
            Assert.Single(lineItems);
            Assert.Equal(productId, lineItems[0].ProductId);
            Assert.Equal(quantity, lineItems[0].Quantity);

            var manifest = new ShopManifest
            {
                ShopId = "test",
                DatabaseFilePath = "test.db"
            };
            var cartContext = CartContextFactory.Create(manifest, reloaded, consumer: null, Context);

            Assert.Equal("test", cartContext.ShopId);
            Assert.Single(cartContext.LineItems);
            Assert.Equal(productId, cartContext.LineItems[0].ProductId);
            Assert.Equal(price, cartContext.LineItems[0].UnitPrice);
            Assert.Equal(quantity, cartContext.LineItems[0].Quantity);
            Assert.Equal(price * quantity, cartContext.LineItems.Sum(x => x.UnitPrice!.Value * x.Quantity));
        }
    }
}
