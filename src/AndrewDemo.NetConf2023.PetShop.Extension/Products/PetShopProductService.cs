using AndrewDemo.NetConf2023.Abstract.Products;
using AndrewDemo.NetConf2023.PetShop.Extension.Services;

namespace AndrewDemo.NetConf2023.PetShop.Extension.Products
{
    public sealed class PetShopProductService : IProductService
    {
        private readonly IProductService _defaultProductService;
        private readonly PetShopReservationService _reservationService;
        private readonly TimeProvider _timeProvider;

        public PetShopProductService(
            IProductService defaultProductService,
            PetShopReservationService reservationService,
            TimeProvider timeProvider)
        {
            _defaultProductService = defaultProductService ?? throw new ArgumentNullException(nameof(defaultProductService));
            _reservationService = reservationService ?? throw new ArgumentNullException(nameof(reservationService));
            _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        }

        public IReadOnlyList<Product> GetPublishedProducts()
        {
            return _defaultProductService.GetPublishedProducts();
        }

        public Product? GetProductById(string productId)
        {
            var product = _defaultProductService.GetProductById(productId);
            if (product == null)
            {
                return null;
            }

            return _reservationService.ApplyReservationProductPolicy(product, _timeProvider.GetUtcNow().UtcDateTime);
        }
    }
}
