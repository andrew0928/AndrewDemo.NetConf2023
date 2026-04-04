using AndrewDemo.NetConf2023.Abstract.Products;
using AndrewDemo.NetConf2023.Abstract.Shops;
using AndrewDemo.NetConf2023.AppleBTS.Extension;
using AndrewDemo.NetConf2023.AppleBTS.API.Configuration;
using AndrewDemo.NetConf2023.Core;
using AndrewDemo.NetConf2023.Core.Products;
using DotNetEnv;

namespace AndrewDemo.NetConf2023.AppleBTS.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Env.Load();

            var builder = WebApplication.CreateBuilder(args);
            var requestedShopId = Environment.GetEnvironmentVariable("SHOP_ID")
                ?? builder.Configuration["shop-id"];

            builder.Services.AddSingleton<IShopManifestResolver>(_ => new ConfigurationShopManifestResolver(builder.Configuration));
            builder.Services.AddSingleton(sp => sp.GetRequiredService<IShopManifestResolver>().Resolve(requestedShopId));

            builder.Services.AddShopDatabase(sp =>
            {
                var manifest = sp.GetRequiredService<ShopManifest>();
                var dbFilePath = Environment.GetEnvironmentVariable("SHOP_DATABASE_FILEPATH")
                    ?? manifest.DatabaseFilePath;

                if (string.IsNullOrWhiteSpace(dbFilePath))
                {
                    throw new InvalidOperationException($"database file path is required for shop {manifest.ShopId}");
                }

                if (!Path.IsPathRooted(dbFilePath))
                {
                    dbFilePath = Path.Combine(AppContext.BaseDirectory, dbFilePath);
                }

                return new ShopDatabaseOptions
                {
                    ConnectionString = $"Filename={dbFilePath};Connection=Shared"
                };
            });

            builder.Services.AddSingleton<DefaultProductService>();
            builder.Services.AddSingleton<IProductService>(sp =>
            {
                var manifest = sp.GetRequiredService<ShopManifest>();

                return manifest.ProductServiceId switch
                {
                    DefaultProductService.ServiceId => sp.GetRequiredService<DefaultProductService>(),
                    _ => throw new InvalidOperationException($"unsupported product service id: {manifest.ProductServiceId}")
                };
            });

            builder.Services.AddAppleBtsExtension();

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                var filePath = Path.Combine(AppContext.BaseDirectory, "AndrewDemo.NetConf2023.AppleBTS.API.xml");
                c.IncludeXmlComments(filePath);
            });

            var app = builder.Build();

            app.Use((context, next) =>
            {
                if (context.Request.Headers.TryGetValue("Authorization", out var tokenValue))
                {
                    var token = tokenValue.ToString();
                    if (token.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    {
                        context.Items["access-token"] = token["Bearer ".Length..];
                    }
                }

                return next();
            });

            app.UseSwagger();
            app.UseSwaggerUI();
            app.MapControllers();
            app.Run();
        }
    }
}
