using System.Collections.Generic;
using AndrewDemo.NetConf2023.Extension.AppleBTS.Models;
using AndrewDemo.NetConf2023.Extension.AppleBTS.Repositories;

namespace AndrewDemo.NetConf2023.Extension.AppleBTS.Services
{
    public sealed class AppleBtsCatalogService
    {
        private readonly BtsOfferRepository _offerRepository;

        public AppleBtsCatalogService(BtsOfferRepository offerRepository)
        {
            _offerRepository = offerRepository;
        }

        public IReadOnlyList<BtsOfferAggregate> GetPublishedMainOffers(DateTime at)
        {
            throw new NotImplementedException();
        }

        public BtsOfferAggregate GetOfferDetail(string mainProductId, DateTime at)
        {
            throw new NotImplementedException();
        }
    }
}
