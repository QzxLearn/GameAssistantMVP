using GameAssistant.Core.Interfaces;
using GameAssistant.Core.Enums;
using OpenCvSharp;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Tesseract;

namespace GameAssistant.Infrastructure.Ocr;

/// <summary>
/// Tesseract OCR 实现
/// </summary>
public class TesseractOcrService : IOcrService
{
    private readonly string _tessDataPath;
    private TesseractEngine? _engine;
    private readonly object _engineLock = new();

    public TesseractOcrService(string tessDataPath = "tessdata")
    {
        _tessDataPath = tessDataPath;
    }

    public string RecognizeFromBytes(byte[] imageBytes, OcrMode mode = OcrMode.Generic)
    {
        if (imageBytes == null || imageBytes.Length == 0)
            throw new ArgumentException("图片数据为空", nameof(imageBytes));

        using var mat = Cv2.ImDecode(imageBytes, ImreadModes.Color);
        if (mat.Empty())
            throw new ArgumentException("图片解码失败", nameof(imageBytes));

        return RecognizeFromMat(mat, mode);
    }

    public string RecognizeFromMat(Mat image, OcrMode mode = OcrMode.Generic)
    {
        using var processed = mode switch
        {
            OcrMode.CardText => ImagePreprocessor.PreprocessForCardText(image),
            _ => ImagePreprocessor.Preprocess(image),
        };

        byte[] pngData = processed.ToBytes(".png");
        using var pix = Pix.LoadFromMemory(pngData);
        pix.XRes = 200;
        pix.YRes = 200;

        string absPath = Path.IsPathFullyQualified(_tessDataPath)
            ? _tessDataPath
            : Path.GetFullPath(_tessDataPath);

        var engine = GetEngine(absPath);
        lock (_engineLock)
        {
            using var page = engine.Process(pix);
            return page.GetText().Trim();
        }
    }

    public Task<string> RecognizeAsync(Mat image, OcrMode mode = OcrMode.Generic, CancellationToken ct = default)
    {
        return Task.Run(() => RecognizeFromMat(image, mode), ct);
    }

    private TesseractEngine GetEngine(string absPath)
    {
        if (_engine == null)
        {
            lock (_engineLock)
            {
                _engine ??= new TesseractEngine(absPath, "eng+chi_sim", EngineMode.Default);
            }
        }
        return _engine;
    }
}
