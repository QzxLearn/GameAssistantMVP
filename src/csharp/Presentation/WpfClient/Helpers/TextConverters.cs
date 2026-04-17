// GameAssistant\src\csharp\Presentation\WpfClient\Helpers\TextConverters.cs
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace GameAssistant.WpfClient.Helpers
{
    /// <summary>
    /// 判断字符串是否为空或 null
    /// </summary>
    public class IsNullOrEmptyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return string.IsNullOrWhiteSpace(value as string);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 空字符串时显示默认文本
    /// </summary>
    public class EmptyToDefaultConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var text = value as string;
            return string.IsNullOrWhiteSpace(text) ? parameter?.ToString() ?? "未命名区域" : text;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}