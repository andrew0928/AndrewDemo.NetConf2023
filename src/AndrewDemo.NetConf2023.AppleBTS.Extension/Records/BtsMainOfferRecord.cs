using LiteDB;

namespace AndrewDemo.NetConf2023.AppleBTS.Extension.Records
{
    public sealed class BtsMainOfferRecord
    {
        [BsonId]
        public string OfferId { get; set; } = string.Empty;
        public string CampaignId { get; set; } = string.Empty;
        public string MainProductId { get; set; } = string.Empty;
        public decimal BtsPrice { get; set; }
        public string? GiftGroupId { get; set; }
        public int MaxGiftQuantity { get; set; } = 1;
        public decimal? MaxGiftSubsidyAmount { get; set; }
    }
}
