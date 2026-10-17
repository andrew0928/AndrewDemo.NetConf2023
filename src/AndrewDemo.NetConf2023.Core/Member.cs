using LiteDB;

namespace AndrewDemo.NetConf2023.Core
{
    public class Member
    {
        [BsonId(true)]
        public int Id { get; set; }
        // 註冊時依 MemberId 白名單指定；舊資料缺少欄位時為 false。
        public bool IsTest { get; init; }

        public string Name { get; set; } = string.Empty;

        public string? ShopNotes { get; set; }
    }
}
