using AndrewDemo.NetConf2023.Abstract.Discounts;
using AndrewDemo.NetConf2023.Core.Products;
using AndrewDemo.NetConf2023.PetShop.Extension.Discounts;
using AndrewDemo.NetConf2023.PetShop.Extension.Products;
using AndrewDemo.NetConf2023.PetShop.Extension.Reservations;
using AndrewDemo.NetConf2023.PetShop.Extension.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AndrewDemo.NetConf2023.PetShop.Extension
{
    public static class PetShopServiceCollectionExtensions
    {
        public static IServiceCollection AddPetShopExtension(this IServiceCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);

            services.AddSingleton<PetShopReservationRepository>();
            services.AddSingleton<PetShopReservationService>();
            services.AddSingleton<PetShopServiceCatalog>();
            services.AddSingleton<PetShopAvailabilityService>();
            services.AddSingleton(sp => new PetShopProductService(
                sp.GetRequiredService<DefaultProductService>(),
                sp.GetRequiredService<PetShopReservationService>(),
                sp.GetRequiredService<TimeProvider>()));
            services.AddSingleton<PetShopOrderEventDispatcher>();
            services.AddSingleton<PetShopReservationPurchaseThresholdDiscountRule>();
            services.AddSingleton<IDiscountRule>(sp => sp.GetRequiredService<PetShopReservationPurchaseThresholdDiscountRule>());
            return services;
        }
    }
}
