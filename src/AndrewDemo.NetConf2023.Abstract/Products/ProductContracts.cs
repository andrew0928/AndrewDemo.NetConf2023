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
    }
}
