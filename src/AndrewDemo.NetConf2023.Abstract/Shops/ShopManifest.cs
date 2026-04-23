using System.Collections.Generic;

namespace AndrewDemo.NetConf2023.Abstract.Shops
{
    public sealed class ShopManifest
    {
        public string ShopId { get; set; } = string.Empty;
        public string DatabaseFilePath { get; set; } = string.Empty;
        public string ProductServiceId { get; set; } = string.Empty;
        public string OrderEventDispatcherId { get; set; } = string.Empty;
        public List<string> EnabledDiscountRuleIds { get; set; } = new List<string>();
    }

    public interface IShopManifestResolver
    {
        ShopManifest Resolve(string? requestedShopId);
    }
}
