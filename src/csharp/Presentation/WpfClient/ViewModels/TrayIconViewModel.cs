using Hardcodet.Wpf.TaskbarNotification;
using NHotkey;
using NHotkey.Wpf;
using System.Windows;
using System.Windows.Input;

namespace GameAssistant.WpfClient.ViewModels;

public class TrayIconViewModel : DependencyObject
{
    private readonly Window _mainWindow;
    private readonly TaskbarIcon _trayIcon;
    private readonly Action _onCaptureRequested;

    public TrayIconViewModel(Window mainWindow, TaskbarIcon trayIcon, Action onCaptureRequested)
    {
        _mainWindow = mainWindow;
        _trayIcon = trayIcon;
        _onCaptureRequested = onCaptureRequested;

        // 注册全局热键：Ctrl+Alt+G
        HotkeyManager.Current.AddOrReplace("CaptureHotkey",
            Key.G,
            ModifierKeys.Control | ModifierKeys.Alt,
            OnHotkeyPressed);

        // 命令绑定
        ShowMainWindowCommand = new RelayCommand(ShowMainWindow);
        CaptureCommand = new RelayCommand(TriggerCapture);
        ExitCommand = new RelayCommand(ExitApplication);
    }

    // 命令定义
    public ICommand ShowMainWindowCommand { get; }
    public ICommand CaptureCommand { get; }
    public ICommand ExitCommand { get; }

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
        // 触发主窗口的捕获逻辑（通过事件或直接调用）
        _onCaptureRequested?.Invoke();
    }

    private void ExitApplication()
    {
        // 清理热键
        HotkeyManager.Current.Remove("CaptureHotkey");

        // 隐式清理托盘图标（TaskbarIcon 会在 Dispose 时自动处理）
        Application.Current.Shutdown();
    }

    // 简易 RelayCommand 实现（避免引入 MVVM 框架）
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        public RelayCommand(Action execute) => _execute = execute;
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => _execute();
        public event EventHandler? CanExecuteChanged;
    }
}
