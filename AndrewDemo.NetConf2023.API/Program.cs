
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
                var filePath = Path.Combine(System.AppContext.BaseDirectory, "AndrewDemo.NetConf2023.API.xml");
                c.IncludeXmlComments(filePath);
            });


            builder.Services.AddHttpClient();

            var app = builder.Build();



            Product.Database.Add(1, new Product()
            {
                Id = 1,
                Name = "18天台灣生啤酒 355ml",
                Description = "18天台灣生啤酒未經過巴氏德高溫殺菌，採用歐洲優質原料，全程0-7°C冷藏保鮮，猶如鮮奶與生魚片般珍貴，保留最多啤酒營養及麥香風味；這樣高品質、超新鮮、賞味期只有18天的台灣生啤酒，值得您搶鮮到手!",
                Price = 65m
            });
            Product.Database.Add(2, new Product()
            {
                Id = 2,
                Name = "可口可樂® 350ml",
                Description = "1886年，美國喬治亞州的亞特蘭大市，有位名叫約翰•潘伯頓（Dr. John S. Pemberton）的藥劑師，他挑選了幾種特別的成分，發明出一款美味的糖漿，沒想到清涼、暢快的「可口可樂」就奇蹟般的出現了！潘伯頓相信這產品可能具有商業價值，因此把它送到傑柯藥局（Jacobs' Pharmacy）販售，開始了「可口可樂」這個美國飲料的傳奇。而潘伯頓的事業合夥人兼會計師：法蘭克•羅賓森（Frank M. Robinson），認為兩個大寫C字母在廣告上可以有不錯的表現，所以創造了\"Coca‑Cola\"這個名字。但是讓「可口可樂」得以大展鋒頭的，卻是從艾薩•坎德勒（Asa G. Candler）這個具有行銷頭腦的企業家開始。",
                Price = 18m
            });
            Product.Database.Add(3, new Product()
            {
                Id = 3,
                Name = "御茶園 特撰冰釀綠茶 550ml",
                Description = "新升級!台灣在地茶葉入，冰釀回甘。台灣在地茶葉，原葉沖泡。如同現泡般的清新綠茶香。",
                Price = 25m
            });

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
            if (app.Environment.IsDevelopment())
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
