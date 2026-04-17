// GameAssistant\src\csharp\Presentation\WpfClient\Views\TrainingDataCaptureWindow.xaml.cs
using GameAssistant.Core.Models;
using GameAssistant.Infrastructure.Capture;
using GameAssistant.Infrastructure.Storage.Data;
using GameAssistant.WpfClient.Constants;
using GameAssistant.WpfClient.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace GameAssistant.WpfClient.Views
{
    public partial class TrainingDataCaptureWindow : System.Windows.Window
    {
        private readonly ScreenCaptureService _captureService;
        private readonly TrainingDataService _trainingDataService;
        private readonly AppDbContext _dbContext;

        private Mat _currentCardImage;
        private string _currentTimestamp;
        private AnnotationSession _session;
        private System.Windows.Point _drawStartPoint;
        private Rectangle? _currentDrawingRect;
        private bool _isAnnotationMode = false;

        private GameSessionRecord? _selectedSessionRecord;
        private CardAnnotation? _selectedAnnotation;
        private bool _isHistoryEditMode = false;

        public TrainingDataCaptureWindow(AppDbContext dbContext)
        {
            InitializeComponent();
            _captureService = new ScreenCaptureService();
            _trainingDataService = new TrainingDataService();
            _dbContext = dbContext;
            _session = new AnnotationSession();

            UpdateCounter();
            LoadLastSessionStatus();

            ConfidenceSlider.ValueChanged += (s, e) =>
            {
                ConfidenceText.Text = $"{(int)ConfidenceSlider.Value}%";
                if (AnnotationListBox.SelectedItem is TextAnnotation selected)
                {
                    selected.Confidence = (int)ConfidenceSlider.Value;
                }
            };
            // 添加 ESC 键取消标注模式
            KeyDown += (s, e) =>
            {
                if (e.Key == Key.Escape && _isAnnotationMode)
                {
                    CancelAnnotationMode();
                    e.Handled = true;
                }
            };
            Loaded += async (s, e) => await LoadHistoryAsync();
        }

        #region 上一次工作状态显示

        /// <summary>
        /// 加载并显示上一次工作状态
        /// </summary>
        private async void LoadLastSessionStatus()
        {
            try
            {
                var lastRecord = await _dbContext.GameSessions
                    .OrderByDescending(r => r.Timestamp)
                    .FirstOrDefaultAsync();

                if (lastRecord != null)
                {
                    LastSessionTime.Text = $"时间：{lastRecord.Timestamp:yyyy-MM-dd HH:mm:ss}";
                    LastSessionGame.Text = $"游戏：{lastRecord.GameName}";
                    LastSessionOcr.Text = $"OCR：{lastRecord.OcrResult ?? "无"}";
                    LastSessionStatus.Text = $"状态：{(lastRecord.IsReviewed ? "✅ 已审查" : "⏳ 未审查")}";
                    LastSessionStatus.Foreground = lastRecord.IsReviewed ? Brushes.Green : Brushes.Orange;
                }
                else
                {
                    LastSessionTime.Text = "时间：-";
                    LastSessionGame.Text = "游戏：-";
                    LastSessionOcr.Text = "OCR：-";
                    LastSessionStatus.Text = "状态：无历史记录";
                    LastSessionStatus.Foreground = Brushes.Gray;
                }
            }
            catch (Exception ex)
            {
                UpdateStatus($"❌ 加载工作状态失败：{ex.Message}");
            }
        }

        #endregion

        #region 历史记录管理

        /// <summary>
        /// 加载历史记录到 DataGrid
        /// </summary>
        private async Task LoadHistoryAsync()
        {
            try
            {
                var filter = (HistoryGameFilter.SelectedItem as ComboBoxItem)?.Content?.ToString();
                IQueryable<GameSessionRecord> query = _dbContext.GameSessions.OrderByDescending(r => r.Timestamp);

                if (filter != null && filter != "全部游戏")
                {
                    query = query.Where(r => r.GameName == filter);
                }

                var records = await query.Take(100).ToListAsync();
                HistoryDataGrid.ItemsSource = records;
                HistoryCountText.Text = records.Count.ToString();
            }
            catch (Exception ex)
            {
                UpdateStatus($"❌ 加载历史记录失败：{ex.Message}");
            }
        }

        /// <summary>
        /// 历史记录选择变更
        /// </summary>
        private async void HistoryDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (HistoryDataGrid.SelectedItem is GameSessionRecord record)
            {
                _selectedSessionRecord = record;
                _isHistoryEditMode = true;

                // 显示编辑按钮
                SaveHistoryEditButton.Visibility = Visibility.Visible;
                DeleteHistoryButton.Visibility = Visibility.Visible;

                // 加载截图预览
                if (!string.IsNullOrEmpty(record.ScreenshotPath) && File.Exists(record.ScreenshotPath))
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(record.ScreenshotPath);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    PreviewImage.Source = bitmap;
                }

                // 加载 OCR 结果到编辑框
                CardNameTextBox.Text = record.GameName;
                DescriptionTextBox.Text = record.OcrResult ?? record.GameStateJson;
                ConfidenceSlider.Value = record.Confidence;
                IsVerifiedCheckBox.IsChecked = record.IsReviewed;

                UpdateStatus($"📋 已选择记录：{record.Timestamp:yyyy-MM-dd HH:mm:ss}");
            }
        }

        /// <summary>
        /// 保存历史记录编辑
        /// </summary>
        private async void SaveHistoryEdit_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedSessionRecord == null) return;

            try
            {
                _selectedSessionRecord.OcrResult = DescriptionTextBox.Text;
                _selectedSessionRecord.GameName = CardNameTextBox.Text;
                _selectedSessionRecord.Confidence = (int)ConfidenceSlider.Value;
                _selectedSessionRecord.IsReviewed = IsVerifiedCheckBox.IsChecked == true;
                _selectedSessionRecord.ReviewedAt = _selectedSessionRecord.IsReviewed ? DateTime.UtcNow : (DateTime?)null;

                _dbContext.GameSessions.Update(_selectedSessionRecord);
                await _dbContext.SaveChangesAsync();

                UpdateStatus("✅ 保存成功！");
                MessageBox.Show("✅ 记录已更新！", "成功",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                await LoadHistoryAsync();
                LoadLastSessionStatus();
            }
            catch (Exception ex)
            {
                UpdateStatus($"❌ 保存失败：{ex.Message}");
                MessageBox.Show($"❌ 保存失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 删除历史记录
        /// </summary>
        private async void DeleteHistory_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedSessionRecord == null) return;

            var result = MessageBox.Show(
                $"确定要删除这条记录吗？\n\n时间：{_selectedSessionRecord.Timestamp:yyyy-MM-dd HH:mm:ss}\n游戏：{_selectedSessionRecord.GameName}",
                "确认删除",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    // 删除关联截图
                    if (!string.IsNullOrEmpty(_selectedSessionRecord.ScreenshotPath) &&
                        File.Exists(_selectedSessionRecord.ScreenshotPath))
                    {
                        File.Delete(_selectedSessionRecord.ScreenshotPath);
                    }

                    _dbContext.GameSessions.Remove(_selectedSessionRecord);
                    await _dbContext.SaveChangesAsync();

                    UpdateStatus("🗑️ 记录已删除");
                    PreviewImage.Source = null;
                    CardNameTextBox.Text = "";
                    DescriptionTextBox.Text = "";
                    SaveHistoryEditButton.Visibility = Visibility.Collapsed;
                    DeleteHistoryButton.Visibility = Visibility.Collapsed;

                    await LoadHistoryAsync();
                    LoadLastSessionStatus();
                }
                catch (Exception ex)
                {
                    UpdateStatus($"❌ 删除失败：{ex.Message}");
                }
            }
        }

        private void RefreshHistory_Click(object sender, RoutedEventArgs e)
        {
            _ = LoadHistoryAsync();
        }

        private async void ClearHistory_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "⚠️ 确定要清空所有历史记录吗？\n\n此操作不可恢复！",
                "警告",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var records = await _dbContext.GameSessions.ToListAsync();
                    _dbContext.GameSessions.RemoveRange(records);
                    await _dbContext.SaveChangesAsync();

                    UpdateStatus("🗑️ 历史记录已清空");
                    await LoadHistoryAsync();
                    LoadLastSessionStatus();
                }
                catch (Exception ex)
                {
                    UpdateStatus($"❌ 清空失败：{ex.Message}");
                }
            }
        }

        private void HistoryGameFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _ = LoadHistoryAsync();
        }

        #endregion

        #region 已标注数据管理

        private void LoadAnnotationsAsync()
        {
            try
            {
                var files = _trainingDataService.GetAllAnnotationFiles();
                var annotations = new List<CardAnnotation>();

                foreach (var file in files)
                {
                    var timestamp = System.IO.Path.GetFileNameWithoutExtension(file);
                    var annotation = _trainingDataService.LoadCardAnnotation(timestamp);
                    if (annotation != null)
                        annotations.Add(annotation);
                }

                var filter = (AnnotationGameFilter.SelectedItem as ComboBoxItem)?.Content?.ToString();
                if (filter != null && filter != "全部游戏")
                {
                    annotations = annotations.Where(a => a.GameType == filter).ToList();
                }

                AnnotationDataGrid.ItemsSource = annotations.OrderByDescending(a => a.CreatedAt).ToList();
            }
            catch (Exception ex)
            {
                UpdateStatus($"❌ 加载标注数据失败：{ex.Message}");
            }
        }

        private void AnnotationDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AnnotationDataGrid.SelectedItem is CardAnnotation annotation)
            {
                _selectedAnnotation = annotation;
                _isHistoryEditMode = false;

                // 隐藏历史记录编辑按钮
                SaveHistoryEditButton.Visibility = Visibility.Collapsed;
                DeleteHistoryButton.Visibility = Visibility.Collapsed;

                // 加载标注图像
                if (!string.IsNullOrEmpty(annotation.ImagePath) && File.Exists(annotation.ImagePath))
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(annotation.ImagePath);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    PreviewImage.Source = bitmap;
                }

                // 加载标注信息
                CardNameTextBox.Text = annotation.CardName;
                DescriptionTextBox.Text = annotation.Description;
                GameTypeComboBox.Text = annotation.GameType;
                CardTypeComboBox.Text = annotation.CardType;
                ConfidenceSlider.Value = annotation.OverallConfidence;
                IsVerifiedCheckBox.IsChecked = annotation.IsVerified;

                // 加载标注区域
                _session.Annotations = annotation.TextRegions;
                AnnotationListBox.ItemsSource = _session.Annotations;
                RefreshCanvas();

                UpdateStatus($"🏷️ 已加载标注：{annotation.CardName}");
            }
        }

        private void RefreshAnnotations_Click(object sender, RoutedEventArgs e)
        {
            LoadAnnotationsAsync();
        }

        private void AnnotationGameFilter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            LoadAnnotationsAsync();
        }

        private void ExportAnnotations_Click(object sender, RoutedEventArgs e)
        {
            var gameType = (AnnotationGameFilter.SelectedItem as ComboBoxItem)?.Content?.ToString();
            var exportPath = _trainingDataService.ExportToTrainingFormat(
                gameType ?? "All", out int count);
            MessageBox.Show($"导出 {count} 条记录到：{exportPath}");
        }

        #endregion

        private async void CaptureButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectionWin = new ScreenSelectionWindow();
                bool? result = selectionWin.ShowDialog();
                if (!result.HasValue || !selectionWin.SelectedRegion.HasValue)
                {
                    UpdateStatus("❌ 未选择有效区域");
                    return;
                }

                var region = selectionWin.SelectedRegion.Value;
                _currentCardImage?.Dispose();
                _currentCardImage = _captureService.CaptureRegion(
                    region.X, region.Y, region.Width, region.Height);

                _currentTimestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss_fff");
                var rawPath = _trainingDataService.SaveRawCardImage(
                    _currentCardImage,
                    _currentTimestamp,
                    GameTypeComboBox.Text,
                    CardTypeComboBox.Text
                );

                UpdatePreview(_currentCardImage);
                _session.CurrentImagePath = rawPath;
                _session.Annotations.Clear();
                AnnotationListBox.ItemsSource = null;
                AnnotationListBox.ItemsSource = _session.Annotations;

                // 捕获后自动启用标注模式
                _isAnnotationMode = true;
                AnnotationCanvas.Cursor = Cursors.Cross;
                PreviewImage.Cursor = Cursors.Cross;
                UpdateStatus("✅ 捕获成功！请在图像上拖拽绘制标注区域");

                CardNameTextBox.Focus();
                UpdateCounter();
            }
            catch (Exception ex)
            {
                UpdateStatus($"❌ 捕获失败：{ex.Message}");
            }
        }
        private void UpdatePreview(Mat mat)
        {
            if (mat == null || mat.Empty()) return;

            if (!Cv2.ImEncode(".png", mat, out byte[] imageData))
                throw new InvalidOperationException("图像编码失败");

            using var memoryStream = new MemoryStream(imageData);
            memoryStream.Position = 0;

            var bitmapImage = new BitmapImage();
            bitmapImage.BeginInit();
            bitmapImage.StreamSource = memoryStream;
            bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
            bitmapImage.EndInit();
            bitmapImage.Freeze();

            PreviewImage.Source = bitmapImage;

            // 关键：设置 Canvas 的逻辑大小为原始图片像素大小
            // Viewbox 会负责将其缩放到界面显示大小
            AnnotationCanvas.Width = mat.Width;
            AnnotationCanvas.Height = mat.Height;
        }

        #region 坐标转换 (解决 Viewbox 缩放问题)
        /// <summary>
        /// 将鼠标在缩放界面上的坐标转换为原始图片像素坐标
        /// </summary>
        private System.Windows.Point GetOriginalCoordinates(System.Windows.Point scaledPoint)
        {
            if (_currentCardImage == null || _currentCardImage.Empty())
                return scaledPoint;

            // 计算缩放比例：原始宽度 / 当前显示宽度
            // PreviewImage.ActualWidth 是 Viewbox 缩放后的显示宽度
            // AnnotationCanvas.Width 是原始图片宽度
            double scaleX = AnnotationCanvas.Width / PreviewImage.ActualWidth;
            double scaleY = AnnotationCanvas.Height / PreviewImage.ActualHeight;

            return new System.Windows.Point(scaledPoint.X * scaleX, scaledPoint.Y * scaleY);
        }
        #endregion

        #region 标注绘制逻辑

        private void Canvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // 检查是否处于标注模式
            if (!_isAnnotationMode)
            {
                UpdateStatus("💡 请先点击「➕ 添加标注区域」按钮");
                return;
            }

            _drawStartPoint = GetOriginalCoordinates(e.GetPosition(AnnotationCanvas));
            _session.IsDrawing = true;

            _currentDrawingRect = new Rectangle
            {
                Stroke = Brushes.Red,
                StrokeThickness = 2,
                Fill = new SolidColorBrush(Color.FromArgb(50, 255, 0, 0)),
                Width = 0,
                Height = 0
            };

            Canvas.SetLeft(_currentDrawingRect, _drawStartPoint.X);
            Canvas.SetTop(_currentDrawingRect, _drawStartPoint.Y);
            AnnotationCanvas.Children.Add(_currentDrawingRect);

            UpdateStatus("🖱️ 正在绘制标注区域...");
        }

        private void Canvas_MouseMove(object sender, MouseEventArgs e)
        {
            if (!_session.IsDrawing || _currentDrawingRect == null) return;

            var currentPoint = e.GetPosition(AnnotationCanvas);

            var x = Math.Min(_drawStartPoint.X, currentPoint.X);
            var y = Math.Min(_drawStartPoint.Y, currentPoint.Y);
            var width = Math.Abs(currentPoint.X - _drawStartPoint.X);
            var height = Math.Abs(currentPoint.Y - _drawStartPoint.Y);

            Canvas.SetLeft(_currentDrawingRect, _drawStartPoint.X);
            Canvas.SetTop(_currentDrawingRect, _drawStartPoint.Y);
            _currentDrawingRect.Width = width;
            _currentDrawingRect.Height = height;
        }
        private void Canvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (!_session.IsDrawing || _currentDrawingRect == null) return;
            _session.IsDrawing = false;

            if (_currentDrawingRect.Width >= 10 && _currentDrawingRect.Height >= 10)
            {
                var box = new BoundingBox
                {
                    X = (int)Canvas.GetLeft(_currentDrawingRect),
                    Y = (int)Canvas.GetTop(_currentDrawingRect),
                    Width = (int)_currentDrawingRect.Width,
                    Height = (int)_currentDrawingRect.Height
                };

                var annotation = new TextAnnotation
                {
                    Box = box,
                    Text = "",
                    Confidence = (int)ConfidenceSlider.Value,
                    Category = "text",
                    IsSelected = false
                };

                _session.Annotations.Add(annotation);

                // 关键：数据变更后统一刷新 UI
                RefreshCanvas();
                AnnotationListBox.SelectedIndex = _session.Annotations.Count - 1;
                ConfidenceSlider.Value = annotation.Confidence;
                ConfidenceText.Text = $"{annotation.Confidence}%";

                UpdateStatus($"✅ 添加标注区域：{box.Width}x{box.Height}");
            }
            else
            {
                UpdateStatus("⚠️ 标注区域太小，请重新绘制（最小 10x10 像素）");
            }

            // 移除临时绘制矩形
            if (_currentDrawingRect != null)
            {
                AnnotationCanvas.Children.Remove(_currentDrawingRect);
                _currentDrawingRect = null;
            }
        }
        private void AnnotationListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (AnnotationListBox.SelectedItem is TextAnnotation selected)
            {
                SelectAnnotation(selected.Id);
                SelectedAnnotationTextBox.Text = selected.Text;
                ConfidenceSlider.Value = selected.Confidence;
                ConfidenceText.Text = $"{selected.Confidence}%";
            }
        }

        // 处理选中区域文本编辑
        private void SelectedAnnotationTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (AnnotationListBox.SelectedItem is TextAnnotation selected)
            {
                selected.Text = SelectedAnnotationTextBox.Text;
            }
        }

        // 添加取消标注模式的方法
        private void CancelAnnotationMode()
        {
            _isAnnotationMode = false;
            _session.IsDrawing = false;

            // 恢复光标
            AnnotationCanvas.Cursor = Cursors.Arrow;
            PreviewImage.Cursor = Cursors.Arrow;

            // 恢复背景
            AnnotationCanvas.Background = Brushes.Transparent;

            // 清除未完成的绘制
            if (_currentDrawingRect != null)
            {
                AnnotationCanvas.Children.Remove(_currentDrawingRect);
                _currentDrawingRect = null;
            }

            UpdateStatus("✅ 标注模式已取消");
        }

        private void AddAnnotation_Click(object sender, RoutedEventArgs e)
        {
            // 检查是否已加载图像
            if (_currentCardImage == null || _currentCardImage.Empty())
            {
                UpdateStatus("⚠️ 请先捕获卡牌图像");
                MessageBox.Show("请先点击「📸 捕获卡牌」按钮选择屏幕区域！",
                    "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 启用标注模式
            _isAnnotationMode = true;
            UpdateStatus("💡 请在图像上拖拽鼠标绘制标注区域（按 ESC 取消）");

            // 视觉反馈：改变光标
            AnnotationCanvas.Cursor = Cursors.Cross;
            PreviewImage.Cursor = Cursors.Cross;

            // 高亮提示
            AnnotationCanvas.Background = new SolidColorBrush(Color.FromArgb(20, 0, 255, 0));
        }

        private void DeleteAnnotation_Click(object sender, RoutedEventArgs e)
        {
            if (AnnotationListBox.SelectedItem is TextAnnotation selected)
            {
                // 1. 仅修改数据
                _session.Annotations.Remove(selected);

                // 2. 统一刷新 UI
                RefreshCanvas();

                // 3. 清空选中状态
                _session.SelectedAnnotationId = string.Empty;

                UpdateStatus("🗑️ 已删除标注");
            }
            else
            {
                UpdateStatus("⚠️ 请先选择要删除的标注");
            }
        }

        #endregion

        private void CardNameTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // 实时更新标注
            if (!string.IsNullOrWhiteSpace(_currentTimestamp))
            {
                SaveCurrentAnnotation();
            }
        }

        private CardAnnotation SaveCurrentAnnotation()
        {
            if (string.IsNullOrWhiteSpace(_currentTimestamp)) return null;

            // 保存前清除 UI 状态（避免序列化 IsSelected）
            foreach (var ann in _session.Annotations)
            {
                ann.IsSelected = false;
            }

            var annotation = new CardAnnotation
            {
                Timestamp = _currentTimestamp,
                GameType = GameTypeComboBox.Text,
                CardType = CardTypeComboBox.Text,
                CardName = CardNameTextBox.Text,
                Description = DescriptionTextBox.Text,
                OverallConfidence = (int)ConfidenceSlider.Value,
                IsVerified = IsVerifiedCheckBox.IsChecked == true,
                TextRegions = new List<TextAnnotation>(_session.Annotations),
                ImagePath = _session.CurrentImagePath,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _trainingDataService.SaveCardAnnotation(annotation);
            return annotation;
        }


        /// <summary>
        /// 将标注数据同步到数据库 (GameSessions 表)
        /// </summary>
        private async Task SyncSessionToDatabaseAsync(CardAnnotation annotation)
        {
            if (annotation == null) return;

            try
            {
                // 解析时间戳
                DateTime recordTime;
                if (!DateTime.TryParseExact(annotation.Timestamp, "yyyyMMdd_HHmmss_fff",
                    System.Globalization.CultureInfo.InvariantCulture,
                    System.Globalization.DateTimeStyles.AssumeUniversal, out recordTime))
                {
                    recordTime = DateTime.UtcNow;
                }

                // 检查是否存在相同时间戳的记录（避免重复添加）
                var existingRecord = await _dbContext.GameSessions
                    .FirstOrDefaultAsync(r => r.Timestamp == recordTime);

                var record = existingRecord ?? new GameSessionRecord();

                // 映射字段
                record.Timestamp = recordTime;
                record.GameName = annotation.GameType;
                record.CardType = annotation.CardType;
                record.Confidence = annotation.OverallConfidence;
                record.ScreenshotPath = annotation.ImagePath;
                record.OcrResult = $"{annotation.CardName}\n{annotation.Description}";
                record.GameStateJson = JsonSerializer.Serialize(annotation);

                // 修复：同步验证状态
                record.IsReviewed = annotation.IsVerified;
                record.ReviewedAt = annotation.IsVerified ? DateTime.UtcNow : (DateTime?)null;

                if (existingRecord == null)
                {
                    _dbContext.GameSessions.Add(record);
                }
                else
                {
                    _dbContext.GameSessions.Update(record);
                }

                await _dbContext.SaveChangesAsync();
                UpdateStatus("✅ 数据库同步成功");
            }
            catch (Exception ex)
            {
                UpdateStatus($"❌ 数据库同步失败：{ex.Message}");
            }
        }

        private async void SaveAnnotation_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 1. 保存 JSON 并获取标注对象
                var annotation = SaveCurrentAnnotation();
                if (annotation == null)
                {
                    MessageBox.Show("没有可保存的标注数据", "提示", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // 2. 同步到数据库 (修复历史记录为空的问题)
                await SyncSessionToDatabaseAsync(annotation);

                // 3. 退出标注模式
                CancelAnnotationMode();

                UpdateStatus("💾 标注已保存");
                MessageBox.Show("标注保存成功！", "成功",
                    MessageBoxButton.OK, MessageBoxImage.Information);

                // 4. 刷新历史记录列表 (让用户立即看到新记录)
                await LoadHistoryAsync();
                LoadLastSessionStatus();
            }
            catch (Exception ex)
            {
                UpdateStatus($"❌ 保存失败：{ex.Message}");
                MessageBox.Show($"保存失败：{ex.Message}", "错误",
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            var exportPath = _trainingDataService.ExportToTrainingFormat(
                GameTypeComboBox.Text,
                out int exportedCount
            );

            UpdateStatus($"📤 导出完成：{exportedCount} 张卡牌 → {exportPath}");

            MessageBox.Show(
                $"成功导出 {exportedCount} 张训练样本到:\n{exportPath}\n\n" +
                $"WSL 访问路径:\n{_trainingDataService.GetWslAccessiblePath()}",
                "导出成功",
                MessageBoxButton.OK,
                MessageBoxImage.Information
            );
        }

        private void OpenDataFolder_Click(object sender, RoutedEventArgs e)
        {
            var dataDir = _trainingDataService.GetTrainingDataDirectory();
            if (Directory.Exists(dataDir))
                System.Diagnostics.Process.Start("explorer.exe", dataDir);
            else
                Directory.CreateDirectory(dataDir);
        }

        private void UpdateCounter()
        {
            var count = _trainingDataService.GetCapturedCardCount(GameTypeComboBox.Text);
            CountTextBlock.Text = count.ToString();
        }

        private void UpdateStatus(string message)
        {
            StatusTextBlock.Text = $"{DateTime.Now:HH:mm:ss} | {message}";
        }

        protected override void OnClosed(EventArgs e)
        {
            _currentCardImage?.Dispose();
            base.OnClosed(e);
        }

        /// <summary>
        /// 根据数据列表重新渲染 Canvas 上的所有矩形
        /// </summary>
        private void RefreshCanvas()
        {
            // 1. 清空现有 UI 元素
            AnnotationCanvas.Children.Clear();

            // 2. 根据数据重新创建 Rectangle
            foreach (var annotation in _session.Annotations)
            {
                var rect = new Rectangle
                {
                    Width = annotation.Box.Width,
                    Height = annotation.Box.Height,
                    Stroke = annotation.IsSelected ? Brushes.Yellow : Brushes.Red,
                    StrokeThickness = annotation.IsSelected ? 3 : 2,
                    Fill = new SolidColorBrush(
                        Color.FromArgb(50, 255, 0, 0)
                    ),
                    Tag = annotation.Id  // 保留 Tag 用于调试/扩展
                };

                Canvas.SetLeft(rect, annotation.Box.X);
                Canvas.SetTop(rect, annotation.Box.Y);

                // 绑定鼠标事件支持点击选择
                rect.MouseLeftButtonUp += (s, e) =>
                {
                    SelectAnnotation(annotation.Id);
                    e.Handled = true;
                };

                AnnotationCanvas.Children.Add(rect);
            }
        }

        /// <summary>
        /// 选中指定 ID 的标注（更新数据 + 刷新 UI）
        /// </summary>
        private void SelectAnnotation(string annotationId)
        {
            // 1. 更新数据模型
            foreach (var ann in _session.Annotations)
            {
                ann.IsSelected = (ann.Id == annotationId);
            }
            _session.SelectedAnnotationId = annotationId;

            // 2. 同步 ListBox 选中项
            var selectedItem = _session.Annotations.FirstOrDefault(a => a.Id == annotationId);
            if (selectedItem != null)
            {
                AnnotationListBox.SelectedItem = selectedItem;
                // 同步文本框
                SelectedAnnotationTextBox.Text = selectedItem.Text;
                ConfidenceSlider.Value = selectedItem.Confidence;
                ConfidenceText.Text = $"{selectedItem.Confidence}%";
            }

            // 3. 刷新 Canvas 高亮
            RefreshCanvas();
        }

    }
}