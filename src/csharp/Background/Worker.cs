using GameAssistant.Core.Data;
using GameAssistant.Core.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json;

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
        // 示例：在 Worker.cs 中临时加入
        var dbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "game_memory.db");
        using var db = new AppDbContext(dbPath);
        db.Database.EnsureCreated(); // 确保表存在（开发阶段可用）

        var record = new GameSessionRecord
        {
            GameName = "TestGame",
            GameStateJson = JsonSerializer.Serialize(new { hp = 100, gold = 50 }),
            Timestamp = DateTime.UtcNow
        };
        db.GameSessions.Add(record);
        await db.SaveChangesAsync();

        var count = await db.GameSessions.CountAsync();
        _logger.LogInformation("当前记录数: {Count}", count);
        // 后续在这里集成屏幕捕获、OCR、存储逻辑
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(1000, stoppingToken);
        }
    }
}
