using GameAssistant.Core.Interfaces;
using GameAssistant.Infrastructure.AI;
using GameAssistant.Infrastructure.Capture;
using GameAssistant.Infrastructure.Ocr;
using GameAssistant.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

// 注册核心服务
builder.Services.AddSingleton<IOcrService, TesseractOcrService>();
builder.Services.AddSingleton<IScreenCaptureService, WindowsGraphicsCaptureService>();
builder.Services.AddSingleton<IGameStateParser, GenericGameStateParser>();

// 注册 Python Brain AdviceClient (Singleton)
var brainUrl = builder.Configuration["BrainUrl"] ?? "http://localhost:8000";
builder.Services.AddSingleton<AdviceClient>(sp =>
    new AdviceClient(brainUrl, sp.GetRequiredService<ILogger<AdviceClient>>()));

// 注册 Worker
builder.Services.AddHostedService<Worker>();

var host = builder.Build();
host.Run();
