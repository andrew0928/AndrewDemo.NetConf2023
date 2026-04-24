using AndrewDemo.NetConf2023.Abstract.Shops;
using AndrewDemo.NetConf2023.Core;
using AndrewDemo.NetConf2023.Core.Time;
using AndrewDemo.NetConf2023.PetShop.API.Configuration;
using AndrewDemo.NetConf2023.PetShop.Extension;
using AndrewDemo.NetConf2023.PetShop.Extension.Services;
using DotNetEnv;
using Microsoft.Extensions.Options;

namespace AndrewDemo.NetConf2023.PetShop.API
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
            builder.Services.Configure<TimeOptions>(builder.Configuration.GetSection(TimeOptions.SectionName));
            builder.Services.AddConfiguredTimeProvider(sp => sp.GetRequiredService<IOptions<TimeOptions>>().Value);

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

            builder.Services.AddSingleton(sp =>
            {
                var options = new PetShopCatalogOptions();
                builder.Configuration.GetSection("PetShop:Catalog").Bind(options);

                if (options.Services.Count == 0)
                {
                    throw new InvalidOperationException("PetShop:Catalog:Services must not be empty.");
                }

                return options;
            });
            builder.Services.AddPetShopExtension();

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                var filePath = Path.Combine(AppContext.BaseDirectory, "AndrewDemo.NetConf2023.PetShop.API.xml");
                c.IncludeXmlComments(filePath);
            });

            var app = builder.Build();
            var shopManifest = app.Services.GetRequiredService<ShopManifest>();
            Console.WriteLine($"[system] petshop runtime initialized: {shopManifest.ShopId}");

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
