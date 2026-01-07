using OpenCvSharp;
using System.IO;
using System;
using System.Collections.Generic;
using System.Text;
using Tesseract;

namespace GameAssistantMVP.Services
{
    public class OcrService : IOcrService
    {
        public string RecognizeText(Mat image, string dataPath = "./tessdata")
        {
            const int dpi = 200;
            // 将 Mat 编码为 PNG 字节数组
            byte[] pngData = image.ToBytes(".png"); // OpenCvSharp 内置方法

            // 从内存创建 Pix（唯一可靠方式）
            using var pix = Pix.LoadFromMemory(pngData);
            pix.XRes = dpi;
            pix.YRes = dpi;
            // 初始化引擎（确保 tessdata 路径正确）
            using var engine = new TesseractEngine(
                datapath: Path.GetFullPath("./tessdata"),
                language: "eng",
                engineMode: EngineMode.Default
            );

            using var page = engine.Process(pix); // ✅ 只接受 Pix
            return page.GetText();
        }
    }
}
