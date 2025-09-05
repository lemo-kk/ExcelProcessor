using System;
using System.Globalization;
using System.Windows.Data;

namespace ExcelProcessor.WPF.Converters
{
    /// <summary>
    /// 布尔值到切换文本转换器
    /// </summary>
    public class BoolToToggleTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? "禁用用户" : "启用用户";
            }
            
            return "切换状态";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 