using System;
using System.Globalization;
using System.Windows.Data;

namespace ExcelProcessor.WPF.Converters
{
    /// <summary>
    /// 布尔值到文本转换器
    /// </summary>
    public class BoolToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                if (parameter != null)
                {
                    // 使用参数中的分隔符来分割真值和假值
                    var parts = parameter.ToString().Split('|');
                    if (parts.Length >= 2)
                    {
                        return boolValue ? parts[0] : parts[1];
                    }
                }
                
                // 默认值
                return boolValue ? "是" : "否";
            }
            
            return "未知";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 