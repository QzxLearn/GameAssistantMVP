using GameAssistant.Core.Interfaces;
using GameAssistant.Infrastructure.Ocr;
using GameAssistant.Infrastructure.Storage.Data;
using GameAssistant.WpfClient.Constants;
using GameAssistant.WpfClient.Helpers;
using Microsoft.Extensions.DependencyInjection;
using System.Windows;

namespace GameAssistant.WpfClient;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    private IServiceProvider _serviceProvider;
    private TrayIconHelper? _trayHelper;
    private MainWindow? _mainWindow;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 1. 配置 DI 容器
        var services = new ServiceCollection();

        // 注册核心服务
        services.AddSingleton<IOcrService>(sp =>
            new TesseractOcrService(AppConstants.TessDataPath));

        services.AddSingleton<AppDbContext>(sp =>
            new AppDbContext(AppConstants.DbPath));

        _serviceProvider = services.BuildServiceProvider();
        // 1. 创建主窗口但不立即显示（启动到托盘）
        var ocrService = _serviceProvider.GetRequiredService<IOcrService>();
        var dbContext = _serviceProvider.GetRequiredService<AppDbContext>();
        _mainWindow = new MainWindow(ocrService, dbContext); // ? 正确传参

        // 2. 创建托盘助手（传递捕获委托）
        _trayHelper = new TrayIconHelper(
            _mainWindow,
            _mainWindow.OnCaptureRequested // 方法组转换为 Action
        );
        // 4. 应用启动后最小化到托盘
        _mainWindow.Loaded += (s, args) =>
        {
            _mainWindow.Hide(); // 启动时不显示窗口
        };
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // 确保资源清理
        _trayHelper?.Dispose();
        base.OnExit(e);
    }
}


