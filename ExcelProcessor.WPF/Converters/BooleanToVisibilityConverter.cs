using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ExcelProcessor.WPF.Converters
{
    /// <summary>
    /// 布尔值到可见性转换器
    /// </summary>
    public class BooleanToVisibilityConverter : IValueConverter
    {
        /// <summary>
        /// 单例实例
        /// </summary>
        public static readonly BooleanToVisibilityConverter Instance = new BooleanToVisibilityConverter();

        /// <summary>
        /// 将布尔值转换为可见性
        /// </summary>
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? Visibility.Visible : Visibility.Collapsed;
            }
            
            return Visibility.Collapsed;
        }

        /// <summary>
        /// 将可见性转换为布尔值
        /// </summary>
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                return visibility == Visibility.Visible;
            }
            
            return false;
        }
    }
} 