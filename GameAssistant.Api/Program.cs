
using GameAssistant.Api.Services;

namespace GameAssistant.Api
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.

            builder.Services.AddControllers();
            // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
            builder.Services.AddOpenApi();
            // 注册Ocr服务
            builder.Services.AddSingleton<OcrService>();

            // 允许WPF调用（开发阶段）
            builder.Services.AddCors(options =>
            {
                options.AddPolicy("AllowWpf", policy =>
                {
                    policy.WithOrigins("http://localhost", "https://localhost")
                          .AllowAnyHeader()
                          .AllowAnyMethod();
                });
            });

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.UseCors("AllowWpf");
            app.MapControllers();

            app.Run();
        }
    }
}
