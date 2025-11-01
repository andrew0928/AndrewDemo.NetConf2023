using System.Linq;
using AndrewDemo.NetConf2023.Core;
using Xunit;

namespace AndrewDemo.NetConf2023.Core.Tests
{
    public class ProductPersistenceTests
    {
        [Fact]
        public void ProductUpsertAndGetAll_ReturnsInsertedProduct()
        {
            decimal price = 42m;
            int productId = TestDataFactory.CreateProduct(price);

            var product = Product.GetById(productId);
            Assert.NotNull(product);
            Assert.Equal(price, product!.Price);

            var allProducts = Product.GetAll();
            Assert.Contains(allProducts, p => p.Id == productId && p.Price == price);
        }
    }
}
