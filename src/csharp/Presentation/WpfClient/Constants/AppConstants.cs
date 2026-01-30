using System.IO;

namespace GameAssistant.WpfClient.Constants
{
    public static class AppConstants
    {
        public static readonly string LocalAppDataPath =
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);

        public static readonly string DbPath =
            Path.Combine(LocalAppDataPath, "game_memory.db");

        public static readonly string TessDataPath = "tessdata";
    }
}

