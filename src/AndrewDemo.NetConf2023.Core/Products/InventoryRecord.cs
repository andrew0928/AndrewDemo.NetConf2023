using System;
using LiteDB;

namespace AndrewDemo.NetConf2023.Core.Products
{
    public sealed class InventoryRecord
    {
        [BsonId]
        public string SkuId { get; set; } = string.Empty;

        public int AvailableQuantity { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}
