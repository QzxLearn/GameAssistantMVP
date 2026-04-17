// GameAssistant.WpfClient/Helpers/TrayIconHelper.cs
using GameAssistant.Infrastructure.Storage.Data;
using GameAssistant.WpfClient.Views;
using Hardcodet.Wpf.TaskbarNotification;
using Microsoft.Extensions.DependencyInjection;
using NHotkey;
using NHotkey.Wpf;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace GameAssistant.WpfClient.Helpers;

public class TrayIconHelper : IDisposable
{
    private readonly TaskbarIcon _trayIcon;
    private readonly Window _mainWindow;
    private readonly Action _onCaptureRequested;
    private readonly IServiceProvider _serviceProvider;
    private bool _disposed;

    public TrayIconHelper(Window mainWindow, Action onCaptureRequested, IServiceProvider serviceProvider)
    {
        _mainWindow = mainWindow;
        _onCaptureRequested = onCaptureRequested;
        _serviceProvider = serviceProvider;

        // 1. 创建托盘图标（纯代码，无 XAML 依赖）
        _trayIcon = new TaskbarIcon
        {
            ToolTipText = "Game Assistant (Ctrl+Alt+G)",
            IconSource = CreateDefaultIcon(),
            DoubleClickCommand = new RelayCommand(ShowMainWindow)
        };

        // 2. 设置右键菜单
        _trayIcon.ContextMenu = CreateContextMenu();

        // 3. 注册全局热键
        HotkeyManager.Current.AddOrReplace(
            "CaptureHotkey",
            Key.G,
            ModifierKeys.Control | ModifierKeys.Alt,
            OnHotkeyPressed
        );
    }

    private ImageSource CreateDefaultIcon()
    {
        // 创建简单的蓝色圆形图标（无需外部文件）
        var drawing = new GeometryDrawing
        {
            Brush = new SolidColorBrush(Colors.CornflowerBlue),
            Geometry = new EllipseGeometry(new Point(8, 8), 8, 8)
        };
        return new DrawingImage(drawing);
    }

    private ContextMenu CreateContextMenu()
    {
        return new ContextMenu
        {
            Items =
            {
                new MenuItem { Header = "Show Window", Command = new RelayCommand(ShowMainWindow) },
                new MenuItem { Header = "Capture Region (Ctrl+Alt+G)", Command = new RelayCommand(TriggerCapture) },
                new MenuItem {
                    Header = "Training Data Capture",
                    Command = new RelayCommand(OpenTrainingWindow)
                },
                new Separator(),
                new MenuItem { Header = "Exit", Command = new RelayCommand(ExitApplication) }
            }
        };
    }

    private void OnHotkeyPressed(object sender, HotkeyEventArgs e)
    {
        TriggerCapture();
        e.Handled = true;
    }

    private void ShowMainWindow()
    {
        if (_mainWindow.WindowState == WindowState.Minimized)
            _mainWindow.WindowState = WindowState.Normal;

        _mainWindow.Show();
        _mainWindow.Activate();
    }

    private void TriggerCapture()
    {
        _onCaptureRequested?.Invoke();
    }

    private void ExitApplication()
    {
        Dispose();
        Application.Current.Shutdown();
    }

    private void OpenTrainingWindow()
    {
        var dbContext = _serviceProvider.GetRequiredService<AppDbContext>();
        var trainingWin = new TrainingDataCaptureWindow(dbContext);
        trainingWin.Show();
        trainingWin.Activate();
    }

    public void Dispose()
    {
        if (_disposed) return;

        // 安全清理热键
        try { HotkeyManager.Current.Remove("CaptureHotkey"); } catch { }

        // 清理托盘图标
        _trayIcon.Dispose();
        _disposed = true;
    }

    // 简易 RelayCommand（避免引入 MVVM 框架）
    private class RelayCommand : ICommand
    {
        private readonly Action _execute;
        public RelayCommand(Action execute) => _execute = execute;
        public bool CanExecute(object? _) => true;
        public void Execute(object? _) => _execute();
        public event EventHandler? CanExecuteChanged;
    }
}
