using System;
using System.Collections.Generic;
using System.Linq;
using AndrewDemo.NetConf2023.Abstract.Shops;
using Microsoft.Extensions.Configuration;

namespace AndrewDemo.NetConf2023.API.Configuration
{
    /// <summary>
    /// 從 host 設定載入商店 manifest，並依啟動參數解析目前要使用的 shop。
    /// </summary>
    public sealed class ConfigurationShopManifestResolver : IShopManifestResolver
    {
        private readonly ShopRuntimeOptions _options;

        /// <summary>
        /// 建立設定式商店 manifest 解析器。
        /// </summary>
        /// <param name="configuration">應用程式設定來源。</param>
        public ConfigurationShopManifestResolver(IConfiguration configuration)
        {
            _options = new ShopRuntimeOptions();
            configuration.GetSection("ShopRuntime").Bind(_options);
        }

        /// <summary>
        /// 解析目前啟動所使用的商店 manifest。
        /// </summary>
        /// <param name="requestedShopId">由環境變數或命令列指定的 shop id。</param>
        /// <returns>解析後的商店 manifest。</returns>
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
