
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
            builder.Services.AddSwaggerGen();

            var app = builder.Build();



            var result = Member.Register("nancy");
            var apikeys = new Dictionary<string, int>
            {
                ["cec8ad70-fa27-4710-a046-7a8d1e65c0d9"] = result.registeredMemberId,
            };


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

                if (!apikeys.TryGetValue(potentialApiKey, out var userId))
                {
                    context.Response.StatusCode = 401;
                    return context.Response.WriteAsync("Unauthorized");
                }

                if (!Member.Database.ContainsKey(userId))
                {
                    context.Response.StatusCode = 401;
                    return context.Response.WriteAsync("Unauthorized");
                }

                context.Items["Consumer"] = Member.Database[userId];

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
