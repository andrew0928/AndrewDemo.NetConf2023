
using AndrewDemo.NetConf2023.Abstract.Discounts;
using AndrewDemo.NetConf2023.Abstract.Shops;
using AndrewDemo.NetConf2023.API.Configuration;
using AndrewDemo.NetConf2023.Core;
using AndrewDemo.NetConf2023.Core.Discounts;
using DotNetEnv;

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

            builder.Services.AddSingleton<IShopManifestResolver>(_ => new ConfigurationShopManifestResolver(builder.Configuration));
            builder.Services.AddSingleton<IShopRuntimeContext>(sp =>
            {
                var requestedShopId = Environment.GetEnvironmentVariable("SHOP_ID")
                    ?? builder.Configuration["shop-id"];

                var manifest = sp.GetRequiredService<IShopManifestResolver>().Resolve(requestedShopId);
                return new ShopRuntimeContext(manifest);
            });

            builder.Services.AddShopDatabase(sp =>
            {
                var runtime = sp.GetRequiredService<IShopRuntimeContext>();
                var dbFilePath = runtime.Manifest.DatabaseFilePath;
                if (string.IsNullOrWhiteSpace(dbFilePath))
                {
                    throw new InvalidOperationException($"database file path is required for shop {runtime.ShopId}");
                }

                if (!Path.IsPathRooted(dbFilePath))
                {
                    dbFilePath = Path.Combine(AppContext.BaseDirectory, dbFilePath);
                }

                return new ShopDatabaseOptions
                {
                    ConnectionString = $"Filename={dbFilePath};Connection=Direct"
                };
            });

            builder.Services.AddSingleton<IDiscountRulePlugin, Product1SecondItemDiscountRulePlugin>();
            builder.Services.AddSingleton<IDiscountEngine, DefaultDiscountEngine>();

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
            var shopRuntime = app.Services.GetRequiredService<IShopRuntimeContext>();
            Console.WriteLine($"[system] shop runtime initialized: {shopRuntime.ShopId}");



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



                
                if (context.Request.Path.StartsWithSegments("/api") && !context.Request.Path.StartsWithSegments("/api/login"))
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
    }
}
