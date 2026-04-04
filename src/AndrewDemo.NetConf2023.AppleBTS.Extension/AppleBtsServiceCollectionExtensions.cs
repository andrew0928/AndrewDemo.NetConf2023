using AndrewDemo.NetConf2023.Abstract.Discounts;
using AndrewDemo.NetConf2023.AppleBTS.Extension.Discounts;
using AndrewDemo.NetConf2023.AppleBTS.Extension.Repositories;
using AndrewDemo.NetConf2023.AppleBTS.Extension.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AndrewDemo.NetConf2023.AppleBTS.Extension
{
    public static class AppleBtsServiceCollectionExtensions
    {
        public static IServiceCollection AddAppleBtsExtension(this IServiceCollection services)
        {
            ArgumentNullException.ThrowIfNull(services);

            services.AddSingleton<BtsOfferRepository>();
            services.AddSingleton<MemberEducationVerificationRepository>();
            services.AddSingleton<MemberEducationQualificationService>();
            services.AddSingleton<AppleBtsCatalogService>();
            services.AddSingleton<AppleBtsAdminService>();
            services.AddSingleton<BtsDiscountRule>();
            services.AddSingleton<IDiscountRule>(sp => sp.GetRequiredService<BtsDiscountRule>());
            return services;
        }
    }
}
