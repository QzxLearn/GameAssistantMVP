using GameAssistant.Core.Interfaces;
using GameAssistant.Core.Models;
using Microsoft.Extensions.Logging;
using OpenCvSharp;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace GameAssistant.Infrastructure.Capture;

/// <summary>
/// 基于 Win32 BitBlt 的屏幕捕获实现（兼容 Windows）
/// </summary>
public class WindowsGraphicsCaptureService : IScreenCaptureService
{
    private readonly ILogger<WindowsGraphicsCaptureService>? _logger;

    public WindowsGraphicsCaptureService(ILogger<WindowsGraphicsCaptureService>? logger = null)
    {
        _logger = logger;
    }
    [DllImport("user32.dll")]
    static extern IntPtr GetDC(IntPtr hwnd);

    [DllImport("user32.dll")]
    static extern int ReleaseDC(IntPtr hwnd, IntPtr hdc);

    [DllImport("gdi32.dll")]
    static extern IntPtr CreateCompatibleDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    static extern IntPtr CreateCompatibleBitmap(IntPtr hdc, int nWidth, int nHeight);

    [DllImport("gdi32.dll")]
    static extern IntPtr SelectObject(IntPtr hdc, IntPtr hgdiobj);

    [DllImport("gdi32.dll")]
    static extern bool BitBlt(IntPtr hdcDest, int nXDest, int nYDest, int nWidth, int nHeight,
                              IntPtr hdcSrc, int nXSrc, int nYSrc, int dwRop);

    [DllImport("gdi32.dll")]
    static extern bool DeleteDC(IntPtr hdc);

    [DllImport("gdi32.dll")]
    static extern bool DeleteObject(IntPtr hObject);

    [DllImport("user32.dll")]
    static extern int GetSystemMetrics(int nIndex);

    const int SRCCOPY = 0x00CC0020;
    const int SM_CXSCREEN = 0;
    const int SM_CYSCREEN = 1;

    public Mat CaptureFullscreen()
    {
        int width = GetSystemMetrics(SM_CXSCREEN);
        int height = GetSystemMetrics(SM_CYSCREEN);
        return CaptureRegion(0, 0, width, height);
    }

    public Mat CaptureRegion(CaptureRegion region)
    {
        return CaptureRegion(region.X, region.Y, region.Width, region.Height);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    public Mat CaptureRegion(double x, double y, double width, double height)
    {
        if (width <= 0 || height <= 0)
            throw new ArgumentException("Width and height must be positive.");

        int ix = (int)Math.Round(x);
        int iy = (int)Math.Round(y);
        int iw = Math.Max(1, (int)Math.Round(width));
        int ih = Math.Max(1, (int)Math.Round(height));

        IntPtr screenDC = GetDC(IntPtr.Zero);
        IntPtr memDC = CreateCompatibleDC(screenDC);
        IntPtr hBitmap = CreateCompatibleBitmap(screenDC, iw, ih);
        IntPtr hOld = SelectObject(memDC, hBitmap);

        bool success = BitBlt(memDC, 0, 0, iw, ih, screenDC, ix, iy, SRCCOPY);

        SelectObject(memDC, hOld);
        DeleteDC(memDC);
        ReleaseDC(IntPtr.Zero, screenDC);

        if (!success)
        {
            DeleteObject(hBitmap);
            throw new InvalidOperationException("Failed to capture screen region.");
        }

        var tempFile = Path.GetTempFileName();
        try
        {
            using (var bmp = Image.FromHbitmap(hBitmap))
                bmp.Save(tempFile, ImageFormat.Png);
            var mat = Cv2.ImRead(tempFile, ImreadModes.Color);
            return mat;
        }
        finally
        {
            DeleteObject(hBitmap);
            if (File.Exists(tempFile))
                File.Delete(tempFile);
        }
    }

    public Task<Mat?> CaptureAsync(CancellationToken ct = default)
    {
        return Task.Run(() =>
        {
            try
            {
                var mat = CaptureFullscreen();
                return (Mat?)mat;
            }
            catch (Exception ex)
            {
                var msg = ex is TypeInitializationException tie && tie.InnerException != null
                    ? $"TypeInit: {tie.InnerException.Message}" : ex.Message;
                _logger?.LogWarning("屏幕捕获失败: {Message}", msg);
                return null;
            }
        }, ct);
    }
}
