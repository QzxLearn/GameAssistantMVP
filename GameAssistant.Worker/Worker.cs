using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace GameAssistant.Worker;

public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;

    public Worker(ILogger<Worker> logger)
    {
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // 后续在这里集成屏幕捕获、OCR、存储逻辑
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }
}