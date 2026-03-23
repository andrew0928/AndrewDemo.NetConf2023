using System;
using System.Collections.Generic;
using System.Linq;
using AndrewDemo.NetConf2023.Abstract.Products;
using AndrewDemo.NetConf2023.Abstract.Shops;

namespace AndrewDemo.NetConf2023.Core.Products
{
    public static class ProductOrderEventFactory
    {
        public static ProductOrderCompletedEvent CreateCompletedEvent(ShopManifest manifest, Order order, DateTime completedAt)
        {
            ArgumentNullException.ThrowIfNull(manifest);
            ArgumentNullException.ThrowIfNull(order);

            return new ProductOrderCompletedEvent
            {
                OrderId = order.Id,
                ShopId = manifest.ShopId,
                BuyerId = order.Buyer.Id,
                BuyerName = order.Buyer.Name,
                CompletedAt = completedAt,
                Lines = order.ProductLines
                    .Select(ToProductOrderLine)
                    .ToArray()
            };
        }

        public static ProductOrderCancelledEvent CreateCancelledEvent(ShopManifest manifest, Order order, IEnumerable<Order.OrderProductLine> affectedLines, DateTime cancelledAt)
        {
            ArgumentNullException.ThrowIfNull(manifest);
            ArgumentNullException.ThrowIfNull(order);
            ArgumentNullException.ThrowIfNull(affectedLines);

            return new ProductOrderCancelledEvent
            {
                OrderId = order.Id,
                ShopId = manifest.ShopId,
                BuyerId = order.Buyer.Id,
                BuyerName = order.Buyer.Name,
                CancelledAt = cancelledAt,
                AffectedLines = affectedLines
                    .Select(ToProductOrderLine)
                    .ToArray()
            };
        }

        private static ProductOrderLine ToProductOrderLine(Order.OrderProductLine line)
        {
            return new ProductOrderLine
            {
                ProductId = line.ProductId,
                ProductName = line.ProductName,
                UnitPrice = line.UnitPrice,
                Quantity = line.Quantity,
                LineAmount = line.LineAmount
            };
        }
    }
}
