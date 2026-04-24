using AndrewDemo.NetConf2023.PetShop.Storefront.Clients;
using AndrewDemo.NetConf2023.PetShop.Storefront.Configuration;
using AndrewDemo.NetConf2023.Storefront.Shared;
using Microsoft.Extensions.Options;

namespace AndrewDemo.NetConf2023.PetShop.Storefront;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddStorefrontShared(builder.Configuration);
        builder.Services.Configure<PetShopApiOptions>(builder.Configuration.GetSection(PetShopApiOptions.SectionName));
        builder.Services.AddHttpClient<PetShopApiClient>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<PetShopApiOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl.TrimEnd('/'));
        });
        builder.Services.AddRazorPages();

        var app = builder.Build();

        app.UseStaticFiles();
        app.UseRouting();
        app.UseSession();
        app.MapRazorPages();
        app.Run();
    }
}
