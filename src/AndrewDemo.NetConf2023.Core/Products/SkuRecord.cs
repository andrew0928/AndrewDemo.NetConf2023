using LiteDB;

namespace AndrewDemo.NetConf2023.Core.Products
{
    public sealed class SkuRecord
    {
        [BsonId]
        public string SkuId { get; set; } = string.Empty;

        public string? ModelCode { get; set; }

        public string? SpecificationSummary { get; set; }
    }
}
