using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ExcelProcessor.WPF.Converters
{
    /// <summary>
    /// 空值到可见性转换器
    /// </summary>
    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (parameter != null && parameter.ToString().ToLower() == "invert")
            {
                // 反转逻辑：null时显示，非null时隐藏
                return value == null ? Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                // 默认逻辑：null时隐藏，非null时显示
                return value == null ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 