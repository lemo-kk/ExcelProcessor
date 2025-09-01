using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ExcelProcessor.WPF.Converters
{
    public class ConnectionStatusToBackgroundConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isConnected)
            {
                return isConnected 
                    ? new SolidColorBrush(Color.FromRgb(76, 175, 80)) // 绿色 - 已连接
                    : new SolidColorBrush(Color.FromRgb(244, 67, 54)); // 红色 - 未连接
            }
            
            return new SolidColorBrush(Color.FromRgb(158, 158, 158)); // 灰色 - 默认
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 