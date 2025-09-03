using System;
using System.Collections;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace ExcelProcessor.WPF.Converters
{
    /// <summary>
    /// 集合数量到可见性转换器
    /// </summary>
    public class CountToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return Visibility.Collapsed;

            int count = 0;

            if (value is ICollection collection)
            {
                count = collection.Count;
            }
            else if (value is int intValue)
            {
                count = intValue;
            }
            else if (int.TryParse(value.ToString(), out int parsedValue))
            {
                count = parsedValue;
            }

            // 检查是否有反转参数
            bool isInverse = parameter != null && parameter.ToString().ToLower() == "inverse";
            
            if (isInverse)
            {
                // 反转逻辑：数量为0时显示，数量大于0时隐藏
                return count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            else
            {
                // 正常逻辑：数量大于0时显示，数量为0时隐藏
                return count > 0 ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 