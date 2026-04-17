using GameAssistant.Core.Enums;
using OpenCvSharp;

namespace GameAssistant.Core.Interfaces;

public interface IOcrService
{
    /// <summary>
    /// 从图像字节数组识别文字
    /// </summary>
    string RecognizeFromBytes(byte[] imageBytes, OcrMode mode = OcrMode.Generic);

    /// <summary>
    /// 从 OpenCvSharp Mat 识别文字（同步）
    /// </summary>
    string RecognizeFromMat(Mat image, OcrMode mode = OcrMode.Generic);

    /// <summary>
    /// 从 OpenCvSharp Mat 识别文字（异步）
    /// </summary>
    Task<string> RecognizeAsync(Mat image, OcrMode mode = OcrMode.Generic, CancellationToken ct = default);
}
