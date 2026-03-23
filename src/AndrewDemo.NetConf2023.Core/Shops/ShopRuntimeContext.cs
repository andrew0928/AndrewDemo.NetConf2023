using System;
using AndrewDemo.NetConf2023.Abstract.Shops;

namespace AndrewDemo.NetConf2023.Core
{
    public sealed class ShopRuntimeContext : IShopRuntimeContext
    {
        public ShopRuntimeContext(ShopManifest manifest)
        {
            Manifest = manifest ?? throw new ArgumentNullException(nameof(manifest));

            if (string.IsNullOrWhiteSpace(manifest.ShopId))
            {
                throw new ArgumentException("shop id is required", nameof(manifest));
            }

            ShopId = manifest.ShopId;
        }

        public string ShopId { get; }

        public ShopManifest Manifest { get; }
    }
}
