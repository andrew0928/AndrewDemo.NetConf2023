using AndrewDemo.NetConf2023.Storefront.Shared;

namespace AndrewDemo.NetConf2023.CommonStorefront;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        builder.Services.AddStorefrontShared(builder.Configuration);
        builder.Services.AddRazorPages();

        var app = builder.Build();

        app.UseStaticFiles();
        app.UseRouting();
        app.UseSession();
        app.MapRazorPages();
        app.Run();
    }
}
