using GameAssistant.Core.Interfaces;
using GameAssistant.Core.Enums;
using GameAssistant.Core.Models;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Text;
using Tesseract;
using static OpenCvSharp.Stitcher;

namespace GameAssistant.Infrastructure.Ocr
{
    /// <summary>
    /// Tesseract OCR 实现
    public class TesseractOcrService : IOcrService
    {
        private readonly string _tessDataPath;

        public TesseractOcrService(string tessDataPath = "tessdata")
        {
            _tessDataPath = tessDataPath;
        }
        public string RecognizeFromBytes(byte[] imageBytes, OcrMode mode = OcrMode.Generic)
        {
            if (imageBytes == null || imageBytes.Length == 0)
                throw new ArgumentException("图像不能为空白", nameof(imageBytes));

            // 1. 解码图像
            using var mat = Cv2.ImDecode(imageBytes, ImreadModes.Color);
            if (mat.Empty()) throw new ArgumentException("图像解码失败");

            using var processed = mode switch
            {
                OcrMode.CardText => ImagePreprocessor.PreprocessForCardText(mat), // ? 专用流水线
            };
            // 2. 预处理：灰度 → Otsu 二值化（反色）→ 2x 放大
            using var gray = new Mat();
            Cv2.CvtColor(mat, gray, ColorConversionCodes.BGR2GRAY);

            using var binary = new Mat();
            Cv2.Threshold(gray, binary, 0, 255, ThresholdTypes.Otsu | ThresholdTypes.BinaryInv);

            using var scaled = new Mat();
            Cv2.Resize(binary, scaled, new OpenCvSharp.Size(), 2.0, 2.0, InterpolationFlags.Lanczos4);

            // 3. 转为 PNG 字节供 Tesseract 处理
            byte[] pngData = scaled.ToBytes(".png");
            using var pix = Pix.LoadFromMemory(pngData);
            pix.XRes = 200;
            pix.YRes = 200;

            // 4. 确保路径绝对化（支持相对路径 "tessdata"）
            string absPath = Path.IsPathFullyQualified(_tessDataPath)
                ? _tessDataPath
                : Path.GetFullPath(_tessDataPath);

            // 5. OCR 识别
            using var engine = new TesseractEngine(absPath, "eng+chi_sim", EngineMode.Default);
            using var page = engine.Process(pix);
            return page.GetText().Trim();
        }
    }
}

