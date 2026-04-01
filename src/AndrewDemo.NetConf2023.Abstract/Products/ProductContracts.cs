using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace AndrewDemo.NetConf2023.Abstract.Products
{
    public sealed class Product
    {
        public string Id { get; set; } = string.Empty;
        [JsonIgnore]
        public string? SkuId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
        public bool IsPublished { get; set; }
    }

    public interface IProductService
    {
        IReadOnlyList<Product> GetPublishedProducts();
        Product? GetProductById(string productId);
        void HandleOrderCompleted(ProductOrderCompletedEvent productEvent);
        void HandleOrderCancelled(ProductOrderCancelledEvent productEvent);
    }

    public sealed class ProductOrderLine
    {
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public decimal UnitPrice { get; set; }
        public int Quantity { get; set; }
        public decimal LineAmount { get; set; }
    }

    public sealed class ProductOrderCompletedEvent
    {
        public int OrderId { get; set; }
        public string ShopId { get; set; } = string.Empty;
        public int BuyerId { get; set; }
        public string BuyerName { get; set; } = string.Empty;
        public DateTime CompletedAt { get; set; }
        public IReadOnlyList<ProductOrderLine> Lines { get; set; } = Array.Empty<ProductOrderLine>();
    }

    public sealed class ProductOrderCancelledEvent
    {
        public int OrderId { get; set; }
        public string ShopId { get; set; } = string.Empty;
        public int BuyerId { get; set; }
        public string BuyerName { get; set; } = string.Empty;
        public DateTime CancelledAt { get; set; }
        public IReadOnlyList<ProductOrderLine> AffectedLines { get; set; } = Array.Empty<ProductOrderLine>();
    }
}
