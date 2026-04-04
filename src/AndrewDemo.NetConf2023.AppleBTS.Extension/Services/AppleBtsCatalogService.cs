using System.Collections.Generic;
using System.Linq;
using AndrewDemo.NetConf2023.AppleBTS.Extension.Models;
using AndrewDemo.NetConf2023.AppleBTS.Extension.Repositories;

namespace AndrewDemo.NetConf2023.AppleBTS.Extension.Services
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
            return _offerRepository
                .GetPublishedMainOffers(at)
                .Select(x => _offerRepository.GetOffer(x.MainProductId, at))
                .ToList();
        }

        public BtsOfferAggregate GetOfferDetail(string mainProductId, DateTime at)
        {
            return _offerRepository.GetOffer(mainProductId, at);
        }
    }
}
