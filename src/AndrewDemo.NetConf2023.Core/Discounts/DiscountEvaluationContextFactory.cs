using System;
using System.Collections.Generic;
using AndrewDemo.NetConf2023.Abstract.Discounts;

namespace AndrewDemo.NetConf2023.Core.Discounts
{
    public static class DiscountEvaluationContextFactory
    {
        public static DiscountEvaluationContext Create(string shopId, Cart cart, Member? consumer, IShopDatabaseContext context)
        {
            if (string.IsNullOrWhiteSpace(shopId))
            {
                throw new ArgumentException("shop id is required", nameof(shopId));
            }

            if (cart == null)
            {
                throw new ArgumentNullException(nameof(cart));
            }

            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            var cartLines = new List<DiscountCartLine>();
            foreach (var lineItem in cart.LineItems)
            {
                var product = context.Products.FindById(lineItem.ProductId)
                    ?? throw new InvalidOperationException($"product {lineItem.ProductId} not found");

                cartLines.Add(new DiscountCartLine
                {
                    ProductId = product.Id,
                    ProductName = product.Name,
                    UnitPrice = product.Price,
                    Quantity = lineItem.Qty
                });
            }

            return new DiscountEvaluationContext
            {
                ShopId = shopId,
                Consumer = consumer == null
                    ? null
                    : new DiscountConsumerSnapshot
                    {
                        MemberId = consumer.Id,
                        MemberName = consumer.Name
                    },
                CartLines = cartLines
            };
        }
    }
}
