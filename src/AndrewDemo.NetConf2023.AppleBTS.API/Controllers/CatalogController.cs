using AndrewDemo.NetConf2023.Abstract.Products;
using AndrewDemo.NetConf2023.AppleBTS.Extension.Services;
using AndrewDemo.NetConf2023.Core.Time;
using Microsoft.AspNetCore.Mvc;

namespace AndrewDemo.NetConf2023.AppleBTS.API.Controllers
{
    [Route("bts-api/catalog")]
    [ApiController]
    public class CatalogController : ControllerBase
    {
        private readonly AppleBtsCatalogService _catalogService;
        private readonly IProductService _productService;
        private readonly TimeProvider _timeProvider;

        public CatalogController(AppleBtsCatalogService catalogService, IProductService productService, TimeProvider timeProvider)
        {
            _catalogService = catalogService;
            _productService = productService;
            _timeProvider = timeProvider;
        }

        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public ActionResult<IReadOnlyList<CatalogItemResponse>> GetPublishedOffers()
        {
            var at = _timeProvider.GetUtcDateTime();
            var responses = _catalogService
                .GetPublishedMainOffers(at)
                .Select(x => ToResponse(x, at))
                .Where(x => x != null)
                .Cast<CatalogItemResponse>()
                .ToList();

            return responses;
        }

        [HttpGet("{mainProductId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public ActionResult<CatalogItemResponse> GetOfferDetail(string mainProductId)
        {
            var at = _timeProvider.GetUtcDateTime();
            var aggregate = _catalogService.GetOfferDetail(mainProductId, at);
            var response = ToResponse(aggregate, at);
            if (response == null)
            {
                return NotFound();
            }

            return response;
        }

        private CatalogItemResponse? ToResponse(AndrewDemo.NetConf2023.AppleBTS.Extension.Models.BtsOfferAggregate aggregate, DateTime at)
        {
            if (aggregate.Campaign == null || aggregate.MainOffer == null)
            {
                return null;
            }

            var mainProduct = _productService.GetProductById(aggregate.MainOffer.MainProductId);
            if (mainProduct == null)
            {
                return null;
            }

            var giftOptions = aggregate.GiftOptions
                .Select(x => _productService.GetProductById(x.GiftProductId))
                .Where(x => x != null)
                .Select(x => new GiftOptionResponse
                {
                    ProductId = x!.Id,
                    ProductName = x.Name,
                    Price = x.Price
                })
                .ToList();

            return new CatalogItemResponse
            {
                CampaignId = aggregate.Campaign.CampaignId,
                CampaignName = aggregate.Campaign.Name,
                StartAt = aggregate.Campaign.StartAt,
                EndAt = aggregate.Campaign.EndAt,
                QueriedAt = at,
                MainProductId = mainProduct.Id,
                MainProductName = mainProduct.Name,
                MainProductDescription = mainProduct.Description,
                RetailPrice = mainProduct.Price,
                BtsPrice = aggregate.MainOffer.BtsPrice,
                MaxGiftQuantity = aggregate.MainOffer.MaxGiftQuantity,
                MaxGiftSubsidyAmount = aggregate.MainOffer.MaxGiftSubsidyAmount,
                GiftOptions = giftOptions
            };
        }

        public sealed class CatalogItemResponse
        {
            public string CampaignId { get; set; } = string.Empty;
            public string CampaignName { get; set; } = string.Empty;
            public DateTime StartAt { get; set; }
            public DateTime EndAt { get; set; }
            public DateTime QueriedAt { get; set; }
            public string MainProductId { get; set; } = string.Empty;
            public string MainProductName { get; set; } = string.Empty;
            public string? MainProductDescription { get; set; }
            public decimal RetailPrice { get; set; }
            public decimal BtsPrice { get; set; }
            public int MaxGiftQuantity { get; set; }
            public decimal? MaxGiftSubsidyAmount { get; set; }
            public List<GiftOptionResponse> GiftOptions { get; set; } = new();
        }

        public sealed class GiftOptionResponse
        {
            public string ProductId { get; set; } = string.Empty;
            public string ProductName { get; set; } = string.Empty;
            public decimal Price { get; set; }
        }
    }
}
