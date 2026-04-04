using AndrewDemo.NetConf2023.Abstract.Shops;

namespace AndrewDemo.NetConf2023.AppleBTS.API.Configuration
{
    public sealed class ConfigurationShopManifestResolver : IShopManifestResolver
    {
        private readonly ShopRuntimeOptions _options;

        public ConfigurationShopManifestResolver(IConfiguration configuration)
        {
            _options = new ShopRuntimeOptions();
            configuration.GetSection("ShopRuntime").Bind(_options);
        }

        public ShopManifest Resolve(string? requestedShopId)
        {
            var resolvedShopId = string.IsNullOrWhiteSpace(requestedShopId)
                ? _options.DefaultShopId
                : requestedShopId;

            if (string.IsNullOrWhiteSpace(resolvedShopId))
            {
                throw new InvalidOperationException("shop id is required");
            }

            if (!_options.Shops.TryGetValue(resolvedShopId, out var manifest))
            {
                throw new InvalidOperationException($"shop manifest not found: {resolvedShopId}");
            }

            return new ShopManifest
            {
                ShopId = string.IsNullOrWhiteSpace(manifest.ShopId) ? resolvedShopId : manifest.ShopId,
                DatabaseFilePath = manifest.DatabaseFilePath,
                ProductServiceId = manifest.ProductServiceId,
                EnabledDiscountRuleIds = manifest.EnabledDiscountRuleIds?.ToList() ?? new List<string>()
            };
        }
    }
}
