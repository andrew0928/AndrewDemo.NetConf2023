using System.Collections.Generic;
using AndrewDemo.NetConf2023.Extension.AppleBTS.Records;

namespace AndrewDemo.NetConf2023.Extension.AppleBTS.Models
{
    public sealed class BtsOfferAggregate
    {
        public BtsCampaignRecord? Campaign { get; init; }
        public BtsMainOfferRecord? MainOffer { get; init; }
        public IReadOnlyList<BtsGiftOptionRecord> GiftOptions { get; init; } = new List<BtsGiftOptionRecord>();
    }
}
