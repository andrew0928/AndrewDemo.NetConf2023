using LiteDB;

namespace AndrewDemo.NetConf2023.Core
{
    public class Member
    {
        [BsonId(true)]
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;

        public string? ShopNotes { get; set; }
    }
}