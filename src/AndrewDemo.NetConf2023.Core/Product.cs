using LiteDB;

namespace AndrewDemo.NetConf2023.Core
{
    public class Product
    {
        [BsonId(true)]
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public decimal Price { get; set; }
    }
}