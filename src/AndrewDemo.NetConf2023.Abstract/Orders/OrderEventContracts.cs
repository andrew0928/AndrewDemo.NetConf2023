using System;
using System.Collections.Generic;

namespace AndrewDemo.NetConf2023.Abstract.Orders
{
    public interface IOrderEventDispatcher
    {
        void Dispatch(OrderCompletedEvent orderEvent);
        void Dispatch(OrderCancelledEvent orderEvent);
    }

    public sealed class OrderProductLine
    {
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal LineAmount { get; set; }
    }

    public sealed class OrderCompletedEvent
    {
        public int OrderId { get; set; }
        public string ShopId { get; set; } = string.Empty;
        public int BuyerId { get; set; }
        public string BuyerName { get; set; } = string.Empty;
        public DateTime CompletedAt { get; set; }
        public IReadOnlyList<OrderProductLine> Lines { get; set; } = Array.Empty<OrderProductLine>();
    }

    public sealed class OrderCancelledEvent
    {
        public int OrderId { get; set; }
        public string ShopId { get; set; } = string.Empty;
        public int BuyerId { get; set; }
        public string BuyerName { get; set; } = string.Empty;
        public DateTime CancelledAt { get; set; }
        public IReadOnlyList<OrderProductLine> AffectedLines { get; set; } = Array.Empty<OrderProductLine>();
    }
}
