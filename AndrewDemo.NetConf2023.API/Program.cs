
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
                var filePath = Path.Combine(System.AppContext.BaseDirectory, "AndrewDemo.NetConf2023.API.xml");
                c.IncludeXmlComments(filePath);
            });


            builder.Services.AddHttpClient();
            //builder.Services.AddSingleton<IGitHubAuthenticationService, GitHubAuthenticationService>();

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
                Console.WriteLine();
                Console.WriteLine($"[system] request api:");
                Console.WriteLine($"  {context.Request.Method} {context.Request.Path}{context.Request.QueryString.Value}");

                foreach (var header in context.Request.Headers)
                {
                    Console.WriteLine($"  {header.Key}: {header.Value}");
                }

                //Console.WriteLine();
                //StreamReader sr = new StreamReader(context.Request.Body);
                //Console.WriteLine($"  {sr.ReadToEndAsync().Result}");



                //if (!context.Request.Path.StartsWithSegments("/api"))
                //{
                //    return next();
                //}
                //if (context.Request.Path.StartsWithSegments("/api/login", StringComparison.OrdinalIgnoreCase))
                //{
                //    return next();
                //}

                //if (!context.Request.Headers.TryGetValue("x-api-key", out var potentialApiKey))
                //{
                //    context.Response.StatusCode = 401;
                //    return context.Response.WriteAsync("Unauthorized");
                //}

                //if (!apikeys.TryGetValue(potentialApiKey, out var appName))
                //{
                //    context.Response.StatusCode = 401;
                //    return context.Response.WriteAsync("Unauthorized");
                //}

                if (context.Request.Path.StartsWithSegments("/api/login"))
                {
                    return next();
                }
                if (!context.Request.Path.StartsWithSegments("/api"))
                {
                    return next();
                }

                if (!context.Request.Headers.TryGetValue("Authorization", out var token))
                {
                    Console.WriteLine($"[system] Authorization Header not found");
                    context.Response.StatusCode = 401;
                    return context.Response.WriteAsync("Unauthorized");
                }

                var accessToken = token.ToString().Substring("Bearer ".Length);
                Console.WriteLine($"[system] token: {accessToken}");
                context.Items["access-token"] = accessToken;



                return next();
            });

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseAuthorization();
            app.UseAuthorization();

            app.UseDefaultFiles();
            app.UseStaticFiles();

            app.MapControllers();

            app.Run();
        }
    }
}
