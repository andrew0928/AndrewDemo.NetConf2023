using AndrewDemo.NetConf2023.API.Configuration;
using AndrewDemo.NetConf2023.PetShop.Extension;
using Microsoft.Extensions.Configuration;

namespace AndrewDemo.NetConf2023.PetShop.API.Tests
{
    public sealed class StandardApiShopManifestResolverTests
    {
        [Fact]
        public void Resolve_PetShopManifest_PreservesOrderEventDispatcherId()
        {
            var configuration = new ConfigurationManager();
            configuration["ShopRuntime:DefaultShopId"] = "petshop";
            configuration["ShopRuntime:Shops:petshop:ShopId"] = "petshop";
            configuration["ShopRuntime:Shops:petshop:DatabaseFilePath"] = "shop-database.db";
            configuration["ShopRuntime:Shops:petshop:ProductServiceId"] = PetShopConstants.ProductServiceId;
            configuration["ShopRuntime:Shops:petshop:OrderEventDispatcherId"] = PetShopConstants.OrderEventDispatcherId;
            configuration["ShopRuntime:Shops:petshop:EnabledDiscountRuleIds:0"] = PetShopConstants.ReservationPurchaseThresholdDiscountRuleId;

            var resolver = new ConfigurationShopManifestResolver(configuration);

            var manifest = resolver.Resolve("petshop");

            Assert.Equal("petshop", manifest.ShopId);
            Assert.Equal(PetShopConstants.ProductServiceId, manifest.ProductServiceId);
            Assert.Equal(PetShopConstants.OrderEventDispatcherId, manifest.OrderEventDispatcherId);
            Assert.Contains(PetShopConstants.ReservationPurchaseThresholdDiscountRuleId, manifest.EnabledDiscountRuleIds);
        }
    }
}
