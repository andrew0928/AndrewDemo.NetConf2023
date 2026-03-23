using System;
using System.Collections.Generic;
using AndrewDemo.NetConf2023.Abstract.Carts;
using AndrewDemo.NetConf2023.Abstract.Products;
using AndrewDemo.NetConf2023.Abstract.Shops;

namespace AndrewDemo.NetConf2023.Core
{
    public static class CartContextFactory
    {
        public static CartContext Create(ShopManifest manifest, Cart cart, Member? consumer, IProductService productService)
        {
            if (manifest == null)
            {
                throw new ArgumentNullException(nameof(manifest));
            }

            if (string.IsNullOrWhiteSpace(manifest.ShopId))
            {
                throw new ArgumentException("shop id is required", nameof(manifest));
            }

            if (cart == null)
            {
                throw new ArgumentNullException(nameof(cart));
            }

            if (productService == null)
            {
                throw new ArgumentNullException(nameof(productService));
            }

            var lineItems = new List<LineItem>();

            foreach (var lineItem in cart.LineItems)
            {
                var product = productService.GetProductById(lineItem.ProductId)
                    ?? throw new InvalidOperationException($"product {lineItem.ProductId} not found");

                lineItems.Add(new LineItem
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    UnitPrice = product.Price,
                    Quantity = lineItem.Quantity
                });
            }

            return new CartContext
            {
                ShopId = manifest.ShopId,
                ConsumerId = consumer?.Id,
                ConsumerName = consumer?.Name,
                LineItems = lineItems
            };
        }
    }
}
