using AndrewDemo.NetConf2023.Abstract.Shops;

namespace AndrewDemo.NetConf2023.AppleBTS.API.Configuration
{
    public sealed class ShopRuntimeOptions
    {
        public string DefaultShopId { get; set; } = "apple-bts";
        public Dictionary<string, ShopManifest> Shops { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    }
}
