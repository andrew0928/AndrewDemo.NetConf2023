using System;
using LiteDB;

namespace AndrewDemo.NetConf2023.AppleBTS.Extension.Records
{
    public sealed class BtsCampaignRecord
    {
        [BsonId]
        public string CampaignId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public DateTime StartAt { get; set; }
        public DateTime EndAt { get; set; }
        public bool IsEnabled { get; set; }
    }
}
