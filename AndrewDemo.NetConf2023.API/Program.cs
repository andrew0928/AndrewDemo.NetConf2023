
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
                
�ʪ��t�Ϊ��e�x (storefront) API�C�H�U�O�ϥγo�� API ���D�n�W�d:

1.�t�Υ������� x - api - key �~��I�s�C
2.�Ȥ�(consumer)���O�W�ާ@�A���������� access token ���ѧO�ϥΪ̡C
3.���o access token ���覡�A�O�z�L�I�s / api / member / login API �Ө��o�C
4.�|�����U���\�ɡA�]�|�Ǧ^ access token�C
5.access token �����Ĵ���(�����})�A�L���ᥲ�����s�n�J�ç󴫷s�� access token�C
6.�b��ܹL�{���A�A�i�H���Ȥ��x�s access token�A�H�K�U���I�s API �ɨϥΡC

                
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
                Name = "18��",
                Price = 65m
            });
            Product.Database.Add(2, new Product()
            {
                Id = 2,
                Name = "�i��",
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
