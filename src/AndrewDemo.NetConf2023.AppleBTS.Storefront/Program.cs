using AndrewDemo.NetConf2023.AppleBTS.Storefront.Clients;
using AndrewDemo.NetConf2023.AppleBTS.Storefront.Configuration;
using AndrewDemo.NetConf2023.Storefront.Shared;
using Microsoft.Extensions.Options;

namespace AndrewDemo.NetConf2023.AppleBTS.Storefront;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddStorefrontShared(builder.Configuration);
        builder.Services.Configure<AppleBtsApiOptions>(builder.Configuration.GetSection(AppleBtsApiOptions.SectionName));
        builder.Services.AddHttpClient<AppleBtsApiClient>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<IOptions<AppleBtsApiOptions>>().Value;
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
