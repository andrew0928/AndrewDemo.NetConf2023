using System;
using System.Collections.Generic;

namespace AndrewDemo.NetConf2023.Abstract.Shops
{
    public sealed class ShopManifest
    {
        public string ShopId { get; set; } = string.Empty;
        public string DatabaseFilePath { get; set; } = string.Empty;
        public List<string> EnabledDiscountRuleIds { get; set; } = new List<string>();
    }

    public sealed class ShopRuntimeOptions
    {
        public string DefaultShopId { get; set; } = "default";
        public Dictionary<string, ShopManifest> Shops { get; set; } = new Dictionary<string, ShopManifest>(StringComparer.OrdinalIgnoreCase);
    }

    public interface IShopManifestResolver
    {
        ShopManifest Resolve(string? requestedShopId);
    }

    public interface IShopRuntimeContext
    {
        string ShopId { get; }
        ShopManifest Manifest { get; }
    }
}
