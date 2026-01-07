// GameAssistantMVP/ScreenCaptureService.cs
using GameAssistantMVP.Models;
using GameAssistantMVP.Services;
using OpenCvSharp;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace GameAssistantMVP;

public class ScreenCaptureService : IScreenCaptureService
{
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

    // 全屏截图
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

    // 指定区域截图（x, y, width, height 均为屏幕坐标）
    public Mat CaptureRegion(double x, double y, double width, double height)
    {
        // 安全边界检查
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
}
