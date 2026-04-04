using System;
using Microsoft.Extensions.DependencyInjection;

namespace AndrewDemo.NetConf2023.Core.Time
{
    public static class TimeProviderServiceCollectionExtensions
    {
        public static IServiceCollection AddConfiguredTimeProvider(
            this IServiceCollection services,
            Func<IServiceProvider, TimeOptions> optionsFactory)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(optionsFactory);

            services.AddSingleton<TimeProvider>(sp => TimeProviderFactory.Create(optionsFactory(sp), TimeProvider.System));
            return services;
        }
    }
}
