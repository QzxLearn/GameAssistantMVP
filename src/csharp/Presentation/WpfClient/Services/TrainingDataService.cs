// GameAssistant\src\csharp\Presentation\WpfClient\Services\TrainingDataService.cs
using System.Text.Json;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using GameAssistant.Core.Models;
using GameAssistant.WpfClient.Constants;

namespace GameAssistant.WpfClient.Services
{
    public class TrainingDataService
    {
        private readonly string _baseDataDir;
        private readonly object _lock = new();

        public TrainingDataService()
        {
            // 使用统一路径
            _baseDataDir = AppConstants.TrainingDataPath;
            AppConstants.EnsureSharedDirectories();
        }

        /// <summary>
        /// 保存原始卡牌截图
        /// </summary>
        public string SaveRawCardImage(Mat image, string timestamp, string gameType, string cardType)
        {
            lock (_lock)
            {
                var rawDir = Path.Combine(_baseDataDir, "raw", gameType, cardType);
                Directory.CreateDirectory(rawDir);
                var filePath = Path.Combine(rawDir, $"card_{timestamp}.png");
                Cv2.ImWrite(filePath, image);
                return filePath;
            }
        }

        /// <summary>
        /// 保存完整标注数据（支持多个文本区域）
        /// </summary>
        public void SaveCardAnnotation(CardAnnotation annotation)
        {
            lock (_lock)
            {
                annotation.UpdatedAt = DateTime.UtcNow;
                var jsonPath = Path.Combine(_baseDataDir, "labeled", $"{annotation.Timestamp}.json");

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                };

                File.WriteAllText(
                    jsonPath,
                    JsonSerializer.Serialize(annotation, options)
                );
            }
        }

        /// <summary>
        /// 加载标注数据
        /// </summary>
        public CardAnnotation? LoadCardAnnotation(string timestamp)
        {
            var jsonPath = Path.Combine(_baseDataDir, "labeled", $"{timestamp}.json");
            if (!File.Exists(jsonPath)) return null;

            var json = File.ReadAllText(jsonPath);
            return JsonSerializer.Deserialize<CardAnnotation>(json);
        }

        /// <summary>
        /// 获取已采集卡牌数量
        /// </summary>
        public int GetCapturedCardCount(string gameType = null)
        {
            var searchDir = gameType == null
                ? Path.Combine(_baseDataDir, "raw")
                : Path.Combine(_baseDataDir, "raw", gameType);

            if (!Directory.Exists(searchDir)) return 0;

            return Directory.GetFiles(searchDir, "*.png", SearchOption.AllDirectories).Length;
        }

        /// <summary>
        /// 获取所有标注文件列表
        /// </summary>
        public List<string> GetAllAnnotationFiles()
        {
            var labeledDir = Path.Combine(_baseDataDir, "labeled");
            if (!Directory.Exists(labeledDir)) return new List<string>();

            return Directory.GetFiles(labeledDir, "*.json").ToList();
        }

        /// <summary>
        /// 导出为标准训练格式（ICDAR 2015）
        /// </summary>
        public string ExportToTrainingFormat(string gameType, out int exportedCount)
        {
            var exportDir = Path.Combine(
                _baseDataDir,
                "export",
                gameType,
                DateTime.Now.ToString("yyyyMMdd_HHmmss")
            );
            Directory.CreateDirectory(exportDir);

            var rawImages = Directory.GetFiles(
                Path.Combine(_baseDataDir, "raw", gameType),
                "*.png",
                SearchOption.AllDirectories
            );

            exportedCount = 0;

            foreach (var imagePath in rawImages)
            {
                var timestamp = Path.GetFileNameWithoutExtension(imagePath).Replace("card_", "");
                var jsonPath = Path.Combine(_baseDataDir, "labeled", $"{timestamp}.json");

                if (File.Exists(jsonPath))
                {
                    var annotation = JsonSerializer.Deserialize<CardAnnotation>(File.ReadAllText(jsonPath));
                    if (annotation != null && annotation.IsVerified)
                    {
                        // 复制图像
                        var destImage = Path.Combine(exportDir, Path.GetFileName(imagePath));
                        File.Copy(imagePath, destImage, true);

                        // 生成 ICDAR 格式标注文件
                        var labelLines = new List<string>();
                        foreach (var textRegion in annotation.TextRegions)
                        {
                            if (!string.IsNullOrWhiteSpace(textRegion.Text))
                            {
                                var line = $"{textRegion.Box.ToIcdarString()},{textRegion.Text}";
                                labelLines.Add(line);
                            }
                        }

                        if (labelLines.Count > 0)
                        {
                            File.WriteAllText(
                                Path.Combine(exportDir, Path.GetFileNameWithoutExtension(imagePath) + ".txt"),
                                string.Join("\n", labelLines)
                            );
                            exportedCount++;
                        }
                    }
                }
            }

            // 生成数据集清单
            File.WriteAllText(
                Path.Combine(exportDir, "dataset_info.json"),
                JsonSerializer.Serialize(
                    new
                    {
                        game = gameType,
                        total_images = rawImages.Length,
                        verified_images = exportedCount,
                        export_time = DateTime.UtcNow,
                        windows_path = _baseDataDir,
                        wsl_path = AppConstants.GetWslPath(_baseDataDir)
                    },
                    new JsonSerializerOptions { WriteIndented = true }
                )
            );

            return exportDir;
        }

        public string GetTrainingDataDirectory() => _baseDataDir;

        /// <summary>
        /// 获取 WSL 可访问的路径
        /// </summary>
        public string GetWslAccessiblePath() => AppConstants.GetWslPath(_baseDataDir);
    }
}