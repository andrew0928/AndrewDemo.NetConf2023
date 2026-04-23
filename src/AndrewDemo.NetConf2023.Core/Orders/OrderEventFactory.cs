using System;
using System.Collections.Generic;
using System.Linq;
using AndrewDemo.NetConf2023.Abstract.Orders;
using AndrewDemo.NetConf2023.Abstract.Shops;

namespace AndrewDemo.NetConf2023.Core.Orders
{
    public static class OrderEventFactory
    {
        public static OrderCompletedEvent CreateCompletedEvent(ShopManifest manifest, Order order, DateTime completedAt)
        {
            ArgumentNullException.ThrowIfNull(manifest);
            ArgumentNullException.ThrowIfNull(order);

            return new OrderCompletedEvent
            {
                OrderId = order.Id,
                ShopId = manifest.ShopId,
                BuyerId = order.Buyer.Id,
                BuyerName = order.Buyer.Name,
                CompletedAt = completedAt,
                Lines = order.ProductLines
                    .Select(ToOrderProductLine)
                    .ToArray()
            };
        }

        public static OrderCancelledEvent CreateCancelledEvent(ShopManifest manifest, Order order, IEnumerable<Order.OrderProductLine> affectedLines, DateTime cancelledAt)
        {
            ArgumentNullException.ThrowIfNull(manifest);
            ArgumentNullException.ThrowIfNull(order);
            ArgumentNullException.ThrowIfNull(affectedLines);

            return new OrderCancelledEvent
            {
                OrderId = order.Id,
                ShopId = manifest.ShopId,
                BuyerId = order.Buyer.Id,
                BuyerName = order.Buyer.Name,
                CancelledAt = cancelledAt,
                AffectedLines = affectedLines
                    .Select(ToOrderProductLine)
                    .ToArray()
            };
        }

        private static OrderProductLine ToOrderProductLine(Order.OrderProductLine line)
        {
            return new OrderProductLine
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
