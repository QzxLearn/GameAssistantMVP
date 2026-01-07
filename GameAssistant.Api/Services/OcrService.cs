using Microsoft.Extensions.Options;
using OpenCvSharp;
using System.Text;
using Tesseract;

namespace GameAssistant.Api.Services
{
    public class OcrService
    {
        private readonly string _tessDataPath;

        public OcrService(IOptions<Dictionary<string, string>> config)
        {
            // 从 appsettings.json 读取 tessdata 路径
            _tessDataPath = config.Value.GetValueOrDefault("TessDataPath", "tessdata");
            if (!Path.IsPathFullyQualified(_tessDataPath))
            {
                _tessDataPath = Path.GetFullPath(_tessDataPath);
            }
        }

        public string RecognizeFromBytes(byte[] imageBytes)
        {
            using var mat = Cv2.ImDecode(imageBytes, ImreadModes.Color);
            if (mat.Empty())
                throw new ArgumentException("Invalid image data");

            using var gray = new Mat();
            Cv2.CvtColor(mat, gray, ColorConversionCodes.BGR2GRAY);

            using var binary = new Mat();
            Cv2.Threshold(gray, binary, 0, 255, ThresholdTypes.Otsu | ThresholdTypes.BinaryInv);

            using var scaled = new Mat();
            Cv2.Resize(binary, scaled, new OpenCvSharp.Size(), 2.0, 2.0, InterpolationFlags.Lanczos4);

            byte[] pngData = scaled.ToBytes(".png");
            using var pix = Pix.LoadFromMemory(pngData);
            pix.XRes = 200;
            pix.YRes = 200;

            using var engine = new TesseractEngine(_tessDataPath, "eng");
            using var page = engine.Process(pix);
            return page.GetText().Trim();
        }
    }
}
