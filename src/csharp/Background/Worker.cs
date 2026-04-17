using GameAssistant.Core.Enums;
using GameAssistant.Core.Interfaces;
using GameAssistant.Core.Models;
using GameAssistant.Infrastructure.AI;
using GameAssistant.Infrastructure.Ocr;
using GameAssistant.Infrastructure.Storage.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenCvSharp;
using System.Text.Json;

namespace GameAssistant.Worker;

/// <summary>
/// 后台 Worker：负责截屏 → OCR → 解析 → 存储的完整循环
/// </summary>
public class Worker : BackgroundService
{
    private readonly ILogger<Worker> _logger;
    private readonly IScreenCaptureService _captureService;
    private readonly IOcrService _ocrService;
    private readonly IGameStateParser _gameStateParser;
    private readonly AdviceClient _adviceClient;

    public Worker(
        ILogger<Worker> logger,
        IScreenCaptureService captureService,
        IOcrService ocrService,
        IGameStateParser gameStateParser,
        AdviceClient adviceClient)
    {
        _logger = logger;
        _captureService = captureService;
        _ocrService = ocrService;
        _gameStateParser = gameStateParser;
        _adviceClient = adviceClient;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // 初始化数据库
        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "GameAssistant", "game_memory.db");

        var screenshotDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "GameAssistant", "screenshots");

        Directory.CreateDirectory(Path.GetDirectoryName(dbPath)!);
        Directory.CreateDirectory(screenshotDir);

        using var db = new AppDbContext(dbPath);
        await db.Database.EnsureCreatedAsync(stoppingToken);

        _logger.LogInformation("GameAssistant Worker 已启动，数据目录: {DbPath}, 截图目录: {ScreenshotDir}", dbPath, screenshotDir);

        // 主循环：每 2 秒截屏分析一次
        var captureIntervalMs = 2000;

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // 1. 截取游戏窗口
                using var screenshot = await _captureService.CaptureAsync(stoppingToken);
                if (screenshot == null)
                {
                    await Task.Delay(captureIntervalMs, stoppingToken);
                    continue;
                }

                // 保存截图用于调试
                var screenshotPath = Path.Combine(screenshotDir, $"{DateTime.Now:yyyyMMdd_HHmmss_fff}.png");
                Cv2.ImEncode(".png", screenshot, out byte[] pngBytes);
                await File.WriteAllBytesAsync(screenshotPath, pngBytes, stoppingToken);
                _logger.LogDebug("已保存截图: {ScreenshotPath}", screenshotPath);

                // 2. OCR 识别
                var ocrText = await _ocrService.RecognizeAsync(screenshot, OcrMode.Generic, stoppingToken);

                // 3. 解析为结构化状态
                var gameState = _gameStateParser.Parse(ocrText);
                gameState.RawOcrText = ocrText;

                // 4. 存入数据库
                var record = new GameSessionRecord
                {
                    GameName = gameState.GameName,
                    GameStateJson = JsonSerializer.Serialize(gameState),
                    Timestamp = DateTime.UtcNow
                };

                db.GameSessions.Add(record);

                // 每 10 条记录批量提交一次，减少 IO
                if (db.GameSessions.Count() % 10 == 0)
                {
                    await db.SaveChangesAsync(stoppingToken);
                    _logger.LogInformation("已保存 10 条记录，当前总数: {Count}",
                        await db.GameSessions.CountAsync(stoppingToken));
                }

_logger.LogDebug("解析结果: {GameName} @ {Floor}层",
                    gameState.GameName, GetFloor(gameState));

                // 调用 Python Brain 获取出牌建议（仅 SlayTheSpire 支持）
                if (gameState is SlayTheSpireGameState sts)
                {
                    var advice = await _adviceClient.GetAdviceAsync(sts, stoppingToken);
                    if (advice != null)
                    {
                        _logger.LogInformation("Card advice: {Suggestion} | Reasoning: {Reasoning}",
                            advice.Suggestion, advice.Reasoning);
                    }
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "处理帧时发生错误");
                await Task.Delay(5000, stoppingToken); // 出错时等待 5 秒
            }
        }

        // 最终保存
        await db.SaveChangesAsync(CancellationToken.None);
        _logger.LogInformation("GameAssistant Worker 已停止");
    }

    private static int GetFloor(GameState state)
    {
        return state switch
        {
            SlayTheSpireGameState sts => sts.Floor,
            _ => 0
        };
    }
}
