using System.Linq;
using AndrewDemo.NetConf2023.Abstract.Products;
using AndrewDemo.NetConf2023.Core;
using AndrewDemo.NetConf2023.Core.Products;
using Xunit;

namespace AndrewDemo.NetConf2023.Core.Tests
{
    public class ProductPersistenceTests : ShopDatabaseTestBase
    {
        [Fact]
        public void ProductUpsertAndGetAll_ReturnsInsertedProduct()
        {
            decimal price = 42m;
            string productId = TestDataFactory.CreateProduct(Context, price);

            var product = Context.Products.FindById(productId);
            Assert.NotNull(product);
            Assert.Equal(price, product!.Price);

            var allProducts = Context.Products.FindAll().ToList();
            Assert.Contains(allProducts, p => p.Id == productId && p.Price == price);
        }

        [Fact]
        public void ProductSkuAndInventory_CanBePersistedForStockTrackedProduct()
        {
            var (productId, skuId) = TestDataFactory.CreateStockTrackedProduct(Context, 88m, availableQuantity: 12);

            var product = Context.Products.FindById(productId);
            Assert.NotNull(product);
            Assert.Equal(skuId, product!.SkuId);

            var sku = Context.Skus.FindById(skuId);
            Assert.NotNull(sku);
            Assert.Equal(skuId, sku!.SkuId);

            var inventory = Context.InventoryRecords.FindById(skuId);
            Assert.NotNull(inventory);
            Assert.Equal(12, inventory!.AvailableQuantity);
        }
    }
}
