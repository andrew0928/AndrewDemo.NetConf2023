
using AndrewDemo.NetConf2023.Core;

namespace AndrewDemo.NetConf2023.API
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            

            builder.Services.AddControllers();
            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
            {
                //                c.SwaggerDoc("1.0",
                //                    new()
                //                    {
                //                        Title = "AndrewDemo.NetConf2023.API",
                //                        Version = "1.0",
                //                        Description = @""
                //                    });
                /*
                
購物系統的前台 (storefront) API。以下是使用這組 API 的主要規範:

1.系統必須提供 x - api - key 才能呼叫。
2.客戶(consumer)的記名操作，都必須提供 access token 來識別使用者。
3.取得 access token 的方式，是透過呼叫 / api / member / login API 來取得。
4.會員註冊成功時，也會傳回 access token。
5.access token 有有效期限(不公開)，過期後必須重新登入並更換新的 access token。
6.在對話過程內，你可以替客戶儲存 access token，以便下次呼叫 API 時使用。

                
                */
                var filePath = Path.Combine(System.AppContext.BaseDirectory, "AndrewDemo.NetConf2023.API.xml");
                c.IncludeXmlComments(filePath);
            });

            var app = builder.Build();


            // init database & apikey settings
            var apikeys = new Dictionary<string, string>
            {
                ["cec8ad70-fa27-4710-a046-7a8d1e65c0d9"] = "AndrewShop GTP v3",
                ["d069d4eb-6a1f-49c4-a8d0-3e32079e54b5"] = "AndrewShop GTP v4",
            };

            Product.Database.Add(1, new Product()
            {
                Id = 1,
                Name = "18天",
                Price = 65m
            });
            Product.Database.Add(2, new Product()
            {
                Id = 2,
                Name = "可樂",
                Price = 18m
            });


            app.Use((context, next) =>
            {
                if (!context.Request.Path.StartsWithSegments("/api"))
                {
                    return next();
                }

                if (!context.Request.Headers.TryGetValue("x-api-key", out var potentialApiKey))
                {
                    context.Response.StatusCode = 401;
                    return context.Response.WriteAsync("Unauthorized");
                }

                if (!apikeys.TryGetValue(potentialApiKey, out var appName))
                {
                    context.Response.StatusCode = 401;
                    return context.Response.WriteAsync("Unauthorized");
                }

                //if (!Member.Database.ContainsKey(userId))
                //{
                //    context.Response.StatusCode = 401;
                //    return context.Response.WriteAsync("Unauthorized");
                //}

                //context.Items["Consumer"] = Member.Database[userId];
                Console.WriteLine($"[system] {appName}({potentialApiKey}) request api {context.Request.Path}.");

                return next();
            });

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseAuthorization();


            app.MapControllers();

            app.Run();
        }
    }


}
