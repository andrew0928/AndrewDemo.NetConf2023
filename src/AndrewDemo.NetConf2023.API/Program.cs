using System;
using System.Collections.Generic;
using System.Linq;
using AndrewDemo.NetConf2023.Abstract.Discounts;
using AndrewDemo.NetConf2023.Abstract.Orders;
using AndrewDemo.NetConf2023.Abstract.Products;
using AndrewDemo.NetConf2023.Abstract.Shops;
using AndrewDemo.NetConf2023.AppleBTS.Extension;
using AndrewDemo.NetConf2023.API.Configuration;
using AndrewDemo.NetConf2023.Core;
using AndrewDemo.NetConf2023.Core.Checkouts;
using AndrewDemo.NetConf2023.Core.Discounts;
using AndrewDemo.NetConf2023.Core.Orders;
using AndrewDemo.NetConf2023.Core.Products;
using AndrewDemo.NetConf2023.Core.Time;
using DotNetEnv;
using Microsoft.Extensions.Options;

namespace AndrewDemo.NetConf2023.API
{
    /// <summary>
    /// 
    /// </summary>
    public class Program
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="args"></param>
        public static void Main(string[] args)
        {
            // 載入 .env 檔案 (如果存在)
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

            builder.Services.AddSingleton<DefaultProductService>();
            builder.Services.AddSingleton<DefaultOrderEventDispatcher>();
            builder.Services.AddSingleton<IProductService>(sp =>
            {
                var manifest = sp.GetRequiredService<ShopManifest>();

                return manifest.ProductServiceId switch
                {
                    DefaultProductService.ServiceId => sp.GetRequiredService<DefaultProductService>(),
                    _ => throw new InvalidOperationException($"unsupported product service id: {manifest.ProductServiceId}")
                };
            });
            builder.Services.AddSingleton<IOrderEventDispatcher>(sp =>
            {
                var manifest = sp.GetRequiredService<ShopManifest>();

                return manifest.OrderEventDispatcherId switch
                {
                    DefaultOrderEventDispatcher.DispatcherId => sp.GetRequiredService<DefaultOrderEventDispatcher>(),
                    _ => throw new InvalidOperationException($"unsupported order event dispatcher id: {manifest.OrderEventDispatcherId}")
                };
            });

            builder.Services.AddSingleton<IDiscountRule, Product1SecondItemDiscountRule>();
            var enabledModules = builder.Configuration
                .GetSection("RuntimeModules:Enabled")
                .Get<string[]>() ?? Array.Empty<string>();

            if (enabledModules.Contains("apple-bts", StringComparer.OrdinalIgnoreCase))
            {
                builder.Services.AddAppleBtsExtension();
            }

            builder.Services.AddSingleton<DiscountEngine>(sp =>
            {
                var manifest = sp.GetRequiredService<ShopManifest>();
                var enabledRuleIds = new HashSet<string>(
                    manifest.EnabledDiscountRuleIds ?? Enumerable.Empty<string>(),
                    StringComparer.OrdinalIgnoreCase);

                var rules = sp.GetServices<IDiscountRule>()
                    .Where(rule => enabledRuleIds.Contains(rule.RuleId));

                return new DiscountEngine(rules);
            });
            builder.Services.AddSingleton<CheckoutService>();

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                var filePath = Path.Combine(System.AppContext.BaseDirectory, "AndrewDemo.NetConf2023.API.xml");
                c.IncludeXmlComments(filePath);
            });


            builder.Services.AddHttpClient();

            var app = builder.Build();
            var shopManifest = app.Services.GetRequiredService<ShopManifest>();
            Console.WriteLine($"[system] shop runtime initialized: {shopManifest.ShopId}");



            //{ 
            //    var token = Member.Register("andrew");
            //    Member.SetShopNotes(
            //        token,
            //        @"安德魯是個很認真工作的工程師，平日工作或是需要專注時都喝茶飲料來提神，但是在休息或放鬆時則喜歡喝可樂。除非特定場合或是好友聚會，不然都不喝酒。");
            //}

            app.Use((context, next) =>
            {
                Console.WriteLine();
                Console.WriteLine($"[system] request api:");
                Console.WriteLine($"  {context.Request.Method} {context.Request.Path}{context.Request.QueryString.Value}");

                //foreach (var header in context.Request.Headers)
                //{
                //    Console.WriteLine($"  {header.Key}: {header.Value}");
                //}



                if (context.Request.Path.StartsWithSegments("/api")
                    && !context.Request.Path.StartsWithSegments("/api/login")
                    && !IsAnonymousApiRequest(context))
                {
                    if (!context.Request.Headers.TryGetValue("Authorization", out var token))
                    {
                        Console.WriteLine($"[system] Authorization Header not found");
                        context.Response.StatusCode = 401;
                        return context.Response.WriteAsync("Unauthorized");
                    }

                    var accessToken = token.ToString().Substring("Bearer ".Length);
                    Console.WriteLine($"[system] token: {accessToken}");
                    context.Items["access-token"] = accessToken;
                }

                return next();
            });

            // Configure the HTTP request pipeline.
            //if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            //app.UseAuthorization();
            //app.UseAuthorization();

            app.UseDefaultFiles();
            app.UseStaticFiles();

            app.MapControllers();

            app.Run();
        }

        private static bool IsAnonymousApiRequest(HttpContext context)
        {
            return (HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsHead(context.Request.Method))
                && context.Request.Path.StartsWithSegments("/api/products");
        }
    }
}
