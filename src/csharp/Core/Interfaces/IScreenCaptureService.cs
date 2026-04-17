using GameAssistant.Core.Models;
using OpenCvSharp;

namespace GameAssistant.Core.Interfaces;

/// <summary>
/// 屏幕捕获服务接口
/// </summary>
public interface IScreenCaptureService
{
    /// <summary>
    /// 截取全屏
    /// </summary>
    Mat CaptureFullscreen();

    /// <summary>
    /// 截取指定区域（相对比例 0.0-1.0）
    /// </summary>
    Mat CaptureRegion(CaptureRegion region);

    /// <summary>
    /// 截取指定区域（绝对像素坐标）
    /// </summary>
    Mat CaptureRegion(double x, double y, double width, double height);

    /// <summary>
    /// 截取游戏窗口（异步）
    /// </summary>
    Task<Mat?> CaptureAsync(CancellationToken ct = default);
}
