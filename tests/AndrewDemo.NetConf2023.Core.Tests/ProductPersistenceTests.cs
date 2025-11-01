using System.Linq;
using AndrewDemo.NetConf2023.Core;
using Xunit;

namespace AndrewDemo.NetConf2023.Core.Tests
{
    public class ProductPersistenceTests : ShopDatabaseTestBase
    {
        [Fact]
        public void ProductUpsertAndGetAll_ReturnsInsertedProduct()
        {
            decimal price = 42m;
            int productId = TestDataFactory.CreateProduct(Context, price);

            var product = Context.Products.FindById(productId);
            Assert.NotNull(product);
            Assert.Equal(price, product!.Price);

            var allProducts = Context.Products.FindAll().ToList();
            Assert.Contains(allProducts, p => p.Id == productId && p.Price == price);
        }
    }
}
