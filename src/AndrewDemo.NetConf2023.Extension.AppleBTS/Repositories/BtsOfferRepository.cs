using System;
using System.Collections.Generic;
using AndrewDemo.NetConf2023.Core;
using AndrewDemo.NetConf2023.Extension.AppleBTS.Models;
using AndrewDemo.NetConf2023.Extension.AppleBTS.Records;

namespace AndrewDemo.NetConf2023.Extension.AppleBTS.Repositories
{
    public sealed class BtsOfferRepository
    {
        private readonly IShopDatabaseContext _database;

        public BtsOfferRepository(IShopDatabaseContext database)
        {
            _database = database;
        }

        public BtsCampaignRecord? GetActiveCampaign(DateTime at)
        {
            throw new NotImplementedException();
        }

        public IReadOnlyList<BtsMainOfferRecord> GetPublishedMainOffers(DateTime at)
        {
            throw new NotImplementedException();
        }

        public BtsOfferAggregate GetOffer(string mainProductId, DateTime at)
        {
            throw new NotImplementedException();
        }

        public IReadOnlyList<BtsGiftOptionRecord> GetGiftOptions(string campaignId, string giftGroupId)
        {
            throw new NotImplementedException();
        }

        public void UpsertCampaign(BtsCampaignRecord record)
        {
            throw new NotImplementedException();
        }

        public void UpsertMainOffer(BtsMainOfferRecord record)
        {
            throw new NotImplementedException();
        }

        public void UpsertGiftOption(BtsGiftOptionRecord record)
        {
            throw new NotImplementedException();
        }

        public void DeleteGiftOption(string optionId)
        {
            throw new NotImplementedException();
        }
    }
}
