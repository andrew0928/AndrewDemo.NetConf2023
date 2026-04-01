using System;
using System.Collections.Generic;

namespace AndrewDemo.NetConf2023.Abstract.Carts
{
    public sealed class CartContext
    {
        public string ShopId { get; init; } = string.Empty;
        public int? ConsumerId { get; init; }
        public string? ConsumerName { get; init; }
        public DateTime EvaluatedAt { get; init; }
        public IReadOnlyList<LineItem> LineItems { get; init; } = Array.Empty<LineItem>();
    }

    public sealed record LineItem
    {
        public string LineId { get; init; } = string.Empty;
        public string? ParentLineId { get; init; }
        public DateTime AddedAt { get; init; }
        public string ProductId { get; init; } = string.Empty;
        public int Quantity { get; init; }
        public string? ProductName { get; init; }
        public decimal? UnitPrice { get; init; }
    }
}
