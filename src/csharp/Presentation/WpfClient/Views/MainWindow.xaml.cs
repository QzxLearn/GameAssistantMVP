using GameAssistant.Core.Enums;
using GameAssistant.Core.Interfaces;
using GameAssistant.Core.Models;
using GameAssistant.Infrastructure.Capture;
using GameAssistant.Infrastructure.Ocr;
using GameAssistant.Infrastructure.Storage.Data;
using GameAssistant.WpfClient.Views;
using Microsoft.EntityFrameworkCore;
using OpenCvSharp;
using System.IO;
using System.Windows;

namespace GameAssistant.WpfClient;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : System.Windows.Window
{
    private readonly IOcrService _ocrService;
    private readonly AppDbContext _dbContext;
    // 新增：允许外部触发捕获的委托
    public Action? OnCaptureRequested { get; set; }
    public MainWindow(IOcrService ocrService, AppDbContext dbContext)
    {
        InitializeComponent();
        _ocrService = ocrService;
        _dbContext = dbContext;
        // 初始化时注册自身捕获逻辑
        OnCaptureRequested = async () => await PerformCaptureAsync();
    }

    private async Task PerformCaptureAsync()
    {
        // 注意：此方法可能从非 UI 线程调用（热键触发），需确保线程安全
        if (!Dispatcher.CheckAccess())
        {
            await Dispatcher.Invoke(PerformCaptureAsync);
            return;
        }
        try
        {
            // 1. 区域选择（保持模态对话框）
            var selectionWin = new ScreenSelectionWindow();
            bool? result = selectionWin.ShowDialog();
            if (!result.HasValue || !selectionWin.SelectedRegion.HasValue)
            {
                ResultBox.Dispatcher.Invoke(() => ResultBox.Text = "? 未选择有效区域");
                return;
            }
            var region = selectionWin.SelectedRegion.Value;

            // 2. 截图
            var captureService = new ScreenCaptureService();
            using var originalMat = captureService.CaptureRegion(region.X, region.Y, region.Width, region.Height);
            Cv2.ImWrite("screenshot_original.png", originalMat);

            // 3. API 处理OCR
            byte[] imageBytes = originalMat.ToBytes(".png");
            // 关键：OCR 是 CPU 密集型，需放到后台线程避免 UI 卡顿
            var text = await Task.Run(() => _ocrService.RecognizeFromBytes(imageBytes, OcrMode.CardText));
            if (string.IsNullOrWhiteSpace(text))
            {
                ResultBox.Dispatcher.Invoke(() => ResultBox.Text = "?? 未识别到文字");
                return;
            }

            // 5. 解析为结构化状态
            var parser = new GenericGameStateParser();
            var gameState = parser.Parse(text);

            // 6. 序列化并存入数据库
            var dbPath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "game_memory.db"
            );
            var record = new GameSessionRecord
            {
                GameName = gameState.GameName,
                GameStateJson = System.Text.Json.JsonSerializer.Serialize(gameState),
                Timestamp = DateTime.UtcNow
            };
            _dbContext.GameSessions.Add(record);
            await _dbContext.SaveChangesAsync();

            // 7. 显示结果
            // 关键：所有 UI 操作需 Dispatcher.Invoke（因可能从托盘线程触发）
            ResultBox.Dispatcher.Invoke(() => ResultBox.Text = $"? 识别成功: {text}");
        }
        catch (Exception ex)
        {
            ResultBox.Dispatcher.Invoke(() =>
                ResultBox.Text = $"? 错误: {ex.Message}");
        }

    }
    private async void CaptureButton_Click(object sender, RoutedEventArgs e)
    {
        await PerformCaptureAsync();
    }

    private void Window_StateChanged(object sender, EventArgs e)
    {
        if (WindowState == WindowState.Minimized)
        {
            Hide(); // 最小化时隐藏窗口（保留托盘）
        }
    }

    private void MinimizeToTrayButton_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }
}
