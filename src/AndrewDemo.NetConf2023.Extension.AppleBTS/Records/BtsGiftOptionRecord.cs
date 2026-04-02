using LiteDB;

namespace AndrewDemo.NetConf2023.Extension.AppleBTS.Records
{
    public sealed class BtsGiftOptionRecord
    {
        [BsonId]
        public string OptionId { get; set; } = string.Empty;
        public string CampaignId { get; set; } = string.Empty;
        public string GiftGroupId { get; set; } = string.Empty;
        public string GiftProductId { get; set; } = string.Empty;
    }
}
