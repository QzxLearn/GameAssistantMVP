using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Text;

namespace GameAssistant.Infrastructure.Ocr
{
    public class ImagePreprocessor
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

        public static Mat PreprocessForCardText(Mat input)
        {
            // 1. 转灰度
            using var gray = new Mat();
            Cv2.CvtColor(input, gray, ColorConversionCodes.BGR2GRAY);

            // 2. 高斯模糊去噪（消除粒子特效）
            using var denoised = new Mat();
            Cv2.GaussianBlur(gray, denoised, new Size(3, 3), 0);

            // 3. 自适应阈值（应对光照变化，比 Otsu 更鲁棒）
            using var binary = new Mat();
            Cv2.AdaptiveThreshold(denoised, binary, 255,
                AdaptiveThresholdTypes.GaussianC,
                ThresholdTypes.BinaryInv,  // 反色：白底黑字 → 黑底白字
                11, 3);

            // 4. 形态学闭运算（连接断裂文字）
            using var kernel = Cv2.GetStructuringElement(MorphShapes.Rect, new Size(2, 2));
            using var morphed = new Mat();
            Cv2.MorphologyEx(binary, morphed, MorphTypes.Close, kernel);

            // 5. 放大 3x（小字体识别关键）
            var scaled = new Mat();
            Cv2.Resize(morphed, scaled, new Size(), 3.0, 3.0, InterpolationFlags.Cubic);

            return scaled;
        }

        /// <summary>
        /// 从全屏截图中精准裁剪卡牌文字区域（假设卡牌在底部 20%）
        /// </summary>
        public static Mat CropCardTextRegion(Mat fullScreen)
        {
            int textHeight = (int)(fullScreen.Height * 0.2);
            int y = fullScreen.Height - textHeight;
            return new Mat(fullScreen, new Rect(0, y, fullScreen.Width, textHeight));
        }

    }
}
