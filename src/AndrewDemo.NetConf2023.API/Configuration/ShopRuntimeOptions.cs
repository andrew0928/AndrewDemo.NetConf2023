using System;
using System.Collections.Generic;
using AndrewDemo.NetConf2023.Abstract.Shops;

namespace AndrewDemo.NetConf2023.API.Configuration
{
    public sealed class ShopRuntimeOptions
    {
        public string DefaultShopId { get; set; } = "default";
        public Dictionary<string, ShopManifest> Shops { get; set; } = new Dictionary<string, ShopManifest>(StringComparer.OrdinalIgnoreCase);
    }
}
