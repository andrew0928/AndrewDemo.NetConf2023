using AndrewDemo.NetConf2023.Storefront.Shared.Authentication;
using AndrewDemo.NetConf2023.Storefront.Shared.Clients;
using AndrewDemo.NetConf2023.Storefront.Shared.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace AndrewDemo.NetConf2023.Storefront.Shared;

public static class StorefrontSharedServiceCollectionExtensions
{
    public static IServiceCollection AddStorefrontShared(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<StorefrontSessionOptions>(configuration.GetSection(StorefrontSessionOptions.SectionName));
        services.Configure<CoreApiOptions>(configuration.GetSection(CoreApiOptions.SectionName));
        services.AddHttpContextAccessor();
        services.AddDistributedMemoryCache();
        services.AddSession(options =>
        {
            var sessionOptions = configuration.GetSection(StorefrontSessionOptions.SectionName).Get<StorefrontSessionOptions>()
                ?? new StorefrontSessionOptions();

            options.Cookie.Name = sessionOptions.CookieName;
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.IdleTimeout = TimeSpan.FromHours(8);
        });

        services.AddScoped<StorefrontSessionAccessor>();
        services.AddScoped<StorefrontAuthService>();
        services.AddHttpClient<CoreApiClient>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<CoreApiOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/'));
        });

        return services;
    }
}
