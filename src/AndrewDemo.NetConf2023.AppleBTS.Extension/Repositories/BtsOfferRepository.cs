using System;
using System.Collections.Generic;
using System.Linq;
using AndrewDemo.NetConf2023.Core;
using AndrewDemo.NetConf2023.AppleBTS.Extension.Models;
using AndrewDemo.NetConf2023.AppleBTS.Extension.Records;
using LiteDB;

namespace AndrewDemo.NetConf2023.AppleBTS.Extension.Repositories
{
    public sealed class BtsOfferRepository
    {
        private readonly IShopDatabaseContext _database;

        public BtsOfferRepository(IShopDatabaseContext database)
        {
            _database = database;
        }

        private ILiteCollection<BtsCampaignRecord> Campaigns =>
            _database.Database.GetCollection<BtsCampaignRecord>(AppleBtsConstants.CampaignsCollectionName);

        private ILiteCollection<BtsMainOfferRecord> MainOffers =>
            _database.Database.GetCollection<BtsMainOfferRecord>(AppleBtsConstants.MainOffersCollectionName);

        private ILiteCollection<BtsGiftOptionRecord> GiftOptions =>
            _database.Database.GetCollection<BtsGiftOptionRecord>(AppleBtsConstants.GiftOptionsCollectionName);

        public BtsCampaignRecord? GetActiveCampaign(DateTime at)
        {
            var evaluationAt = NormalizeUtc(at);

            return Campaigns
                .Find(x => x.IsEnabled)
                .Where(x => NormalizeUtc(x.StartAt) <= evaluationAt && NormalizeUtc(x.EndAt) >= evaluationAt)
                .OrderByDescending(x => NormalizeUtc(x.StartAt))
                .FirstOrDefault();
        }

        public IReadOnlyList<BtsMainOfferRecord> GetPublishedMainOffers(DateTime at)
        {
            var campaign = GetActiveCampaign(at);
            if (campaign == null)
            {
                return Array.Empty<BtsMainOfferRecord>();
            }

            return MainOffers
                .Query()
                .Where(x => x.CampaignId == campaign.CampaignId)
                .OrderBy(x => x.MainProductId)
                .ToList();
        }

        public BtsOfferAggregate GetOffer(string mainProductId, DateTime at)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(mainProductId);

            var campaign = GetActiveCampaign(at);
            if (campaign == null)
            {
                return new BtsOfferAggregate();
            }

            var mainOffer = MainOffers
                .Query()
                .Where(x => x.CampaignId == campaign.CampaignId && x.MainProductId == mainProductId)
                .FirstOrDefault();

            var giftOptions = mainOffer?.GiftGroupId == null
                ? Array.Empty<BtsGiftOptionRecord>()
                : GetGiftOptions(campaign.CampaignId, mainOffer.GiftGroupId);

            return new BtsOfferAggregate
            {
                Campaign = campaign,
                MainOffer = mainOffer,
                GiftOptions = giftOptions
            };
        }

        public IReadOnlyList<BtsGiftOptionRecord> GetGiftOptions(string campaignId, string giftGroupId)
        {
            if (string.IsNullOrWhiteSpace(campaignId) || string.IsNullOrWhiteSpace(giftGroupId))
            {
                return Array.Empty<BtsGiftOptionRecord>();
            }

            return GiftOptions
                .Query()
                .Where(x => x.CampaignId == campaignId && x.GiftGroupId == giftGroupId)
                .OrderBy(x => x.GiftProductId)
                .ToList();
        }

        public bool HasConfiguredMainOffer(string mainProductId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(mainProductId);

            return MainOffers
                .Query()
                .Where(x => x.MainProductId == mainProductId)
                .Limit(1)
                .ToList()
                .Count > 0;
        }

        public void UpsertCampaign(BtsCampaignRecord record)
        {
            ArgumentNullException.ThrowIfNull(record);
            Campaigns.Upsert(record);
        }

        public void UpsertMainOffer(BtsMainOfferRecord record)
        {
            ArgumentNullException.ThrowIfNull(record);
            MainOffers.Upsert(record);
        }

        public void UpsertGiftOption(BtsGiftOptionRecord record)
        {
            ArgumentNullException.ThrowIfNull(record);
            GiftOptions.Upsert(record);
        }

        public void DeleteGiftOption(string optionId)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(optionId);
            GiftOptions.Delete(optionId);
        }

        private static DateTime NormalizeUtc(DateTime value)
        {
            return value.Kind == DateTimeKind.Utc
                ? value
                : value.ToUniversalTime();
        }
    }
}
