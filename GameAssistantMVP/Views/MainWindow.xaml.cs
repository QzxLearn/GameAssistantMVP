using GameAssistantMVP.Services;
using GameAssistantMVP.Views;
using OpenCvSharp;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GameAssistantMVP;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : System.Windows.Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    private void CaptureButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            // 1. 显示区域选择窗口
            var selectionWin = new ScreenSelectionWindow();
            bool? result = selectionWin.ShowDialog();

            if (!result.HasValue || !selectionWin.SelectedRegion.HasValue)
            {
                ResultBox.Text = "❌ 未选择有效区域";
                return;
            }

            var region = selectionWin.SelectedRegion.Value;

            // 2. 截图（指定区域）
            var captureService = new ScreenCaptureService();
            using var originalMat = captureService.CaptureRegion(region.X, region.Y, region.Width, region.Height);
            Cv2.ImWrite("screenshot_original.png", originalMat);

            // 3. 预处理
            using var processedMat = ImagePreprocessor.Preprocess(originalMat);
            Cv2.ImWrite("screenshot_processed.png", processedMat);

            // 4. OCR
            var ocrService = new OcrService();
            var text = ocrService.RecognizeText(processedMat);
            File.WriteAllText("ocr_result.txt", text.Trim());

            // 5. 显示结果
            ResultBox.Text = string.IsNullOrWhiteSpace(text)
                ? "⚠️ 未识别到文字"
                : text.Trim();
        }
        catch (System.Exception ex)
        {
            ResultBox.Text = "❌ 错误: " + ex.Message;
        }
    }
}