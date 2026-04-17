// GameAssistant\src\csharp\Presentation\WpfClient\Constants\AppConstants.cs
using System.IO;
namespace GameAssistant.WpfClient.Constants
{
    public static class AppConstants
    {
        // Windows 本地路径
        public static readonly string LocalAppDataPath =
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        // 🔄 统一数据目录（Windows 和 WSL 共享）
        // Windows: C:\Users\xxx\AppData\Local\GameAssistant\data
        // WSL: /mnt/c/Users/xxx/AppData/Local/GameAssistant/data
        public static readonly string SharedDataPath =
            Path.Combine(LocalAppDataPath, "GameAssistant", "data");

        // 子目录
        public static readonly string RawDataPath = Path.Combine(SharedDataPath, "raw");
        public static readonly string LabeledDataPath = Path.Combine(SharedDataPath, "labeled");
        public static readonly string ModelsPath = Path.Combine(SharedDataPath, "models");
        public static readonly string TrainingDataPath = Path.Combine(SharedDataPath, "training_cards");

        // 数据库路径
        public static readonly string DbPath =
            Path.Combine(LocalAppDataPath, "game_memory.db");

        // Tesseract 数据路径
        public static readonly string TessDataPath = "tessdata";

        // 🔄 WSL 访问路径（供 Python 使用）
        // 在 WSL 中通过 /mnt/c/Users/xxx/AppData/Local/GameAssistant/data 访问
        public static readonly string WslAccessiblePath = SharedDataPath;

        /// <summary>
        /// 获取 WSL 可访问的路径格式
        /// </summary>
        public static string GetWslPath(string windowsPath)
        {
            // 将 C:\Users\xxx 转换为 /mnt/c/Users/xxx
            if (windowsPath.StartsWith("C:\\", StringComparison.OrdinalIgnoreCase))
            {
                return "/mnt/c/" + windowsPath.Substring(3).Replace("\\", "/");
            }
            return windowsPath;
        }

        /// <summary>
        /// 确保共享目录存在
        /// </summary>
        public static void EnsureSharedDirectories()
        {
            Directory.CreateDirectory(RawDataPath);
            Directory.CreateDirectory(LabeledDataPath);
            Directory.CreateDirectory(ModelsPath);
            Directory.CreateDirectory(TrainingDataPath);
            Directory.CreateDirectory(Path.Combine(TrainingDataPath, "raw"));
            Directory.CreateDirectory(Path.Combine(TrainingDataPath, "labeled"));
            Directory.CreateDirectory(Path.Combine(TrainingDataPath, "export"));
        }
    }
}