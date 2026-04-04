using System.Linq;
using AndrewDemo.NetConf2023.Abstract.Shops;
using AndrewDemo.NetConf2023.Core;
using AndrewDemo.NetConf2023.Core.Products;
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
            Assert.False(string.IsNullOrWhiteSpace(lineItems[0].LineId));
            Assert.NotEqual(default, lineItems[0].AddedAt);
            Assert.Equal(productId, lineItems[0].ProductId);
            Assert.Equal(quantity, lineItems[0].Quantity);

            var manifest = new ShopManifest
            {
                ShopId = "test",
                DatabaseFilePath = "test.db",
                ProductServiceId = DefaultProductService.ServiceId
            };
            var cartContext = CartContextFactory.Create(manifest, reloaded, consumer: null, new DefaultProductService(Context));

            Assert.Equal("test", cartContext.ShopId);
            Assert.Single(cartContext.LineItems);
            Assert.NotEqual(default, cartContext.EvaluatedAt);
            Assert.Equal(lineItems[0].LineId, cartContext.LineItems[0].LineId);
            Assert.Equal(lineItems[0].AddedAt, cartContext.LineItems[0].AddedAt);
            Assert.Equal(productId, cartContext.LineItems[0].ProductId);
            Assert.Equal(price, cartContext.LineItems[0].UnitPrice);
            Assert.Equal(quantity, cartContext.LineItems[0].Quantity);
            Assert.Equal(price * quantity, cartContext.LineItems.Sum(x => x.UnitPrice!.Value * x.Quantity));
        }

        [Fact]
        public void CartAddProducts_SameProductTwice_PersistsDistinctLines()
        {
            string productId = TestDataFactory.CreateProduct(Context, 25m);

            var cart = new Cart();
            Context.Carts.Insert(cart);
            cart.AddProducts(productId, 1);
            cart.AddProducts(productId, 1);
            Context.Carts.Update(cart);

            var reloaded = Context.Carts.FindById(cart.Id);
            Assert.NotNull(reloaded);

            var lineItems = reloaded!.LineItems.ToList();
            Assert.Equal(2, lineItems.Count);
            Assert.All(lineItems, item => Assert.Equal(productId, item.ProductId));
            Assert.NotEqual(lineItems[0].LineId, lineItems[1].LineId);
        }

        [Fact]
        public void CartAddProducts_WithParentLineId_PersistsGiftRelation()
        {
            string mainProductId = TestDataFactory.CreateProduct(Context, 100m);
            string giftProductId = TestDataFactory.CreateProduct(Context, 50m);

            var cart = new Cart();
            Context.Carts.Insert(cart);
            cart.AddProducts(mainProductId, 1);
            var mainLineId = cart.LineItems.Single().LineId;
            cart.AddProducts(giftProductId, 1, mainLineId);
            Context.Carts.Update(cart);

            var reloaded = Context.Carts.FindById(cart.Id);
            Assert.NotNull(reloaded);

            var lineItems = reloaded!.LineItems.ToList();
            Assert.Equal(2, lineItems.Count);

            var giftLine = lineItems.Single(x => x.ProductId == giftProductId);
            Assert.Equal(mainLineId, giftLine.ParentLineId);
        }
    }
}
