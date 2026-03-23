using System;
using System.Collections.Generic;
using System.Linq;
using AndrewDemo.NetConf2023.Abstract.Products;

namespace AndrewDemo.NetConf2023.Core.Products
{
    public sealed class DefaultProductService : IProductService
    {
        public const string ServiceId = "default-product-service";

        private readonly IShopDatabaseContext _database;

        public DefaultProductService(IShopDatabaseContext database)
        {
            _database = database;
        }

        public IReadOnlyList<Product> GetPublishedProducts()
        {
            return _database.Products
                .Find(x => x.IsPublished)
                .ToList();
        }

        public Product? GetProductById(string productId)
        {
            if (string.IsNullOrWhiteSpace(productId))
            {
                return null;
            }

            return _database.Products.FindById(productId);
        }

        public void HandleOrderCompleted(ProductOrderCompletedEvent productEvent)
        {
            ArgumentNullException.ThrowIfNull(productEvent);
        }

        public void HandleOrderCancelled(ProductOrderCancelledEvent productEvent)
        {
            ArgumentNullException.ThrowIfNull(productEvent);
        }
    }
}
