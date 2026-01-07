using OpenCvSharp;

namespace GameAssistantMVP;

public static class ImagePreprocessor
{
    public static Mat Preprocess(Mat input)
    {
        if (input.Empty())
            throw new ArgumentException("Input image is empty.");

        // 1. 转灰度
        using var gray = new Mat();
        Cv2.CvtColor(input, gray, ColorConversionCodes.BGR2GRAY);

        // 2. Otsu 二值化（反色更利于 OCR）
        using var binary = new Mat();
        Cv2.Threshold(gray, binary, 0, 255, ThresholdTypes.Otsu | ThresholdTypes.BinaryInv);

        // 3. 放大图像（Tesseract 对小字体识别差，2x 是经验值）
        var scaled = new Mat();
        Cv2.Resize(binary, scaled, new OpenCvSharp.Size(), 2.0, 2.0, InterpolationFlags.Lanczos4);

        return scaled; // caller responsible for disposal
    }
}