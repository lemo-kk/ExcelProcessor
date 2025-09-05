using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;

namespace ExcelProcessor.WPF.Converters
{
    /// <summary>
    /// 密码强度枚举
    /// </summary>
    public enum PasswordStrength
    {
        Weak,
        Medium,
        Strong,
        VeryStrong
    }
    /// <summary>
    /// 布尔值到可见性转换器
    /// </summary>
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                // 如果参数是"invert"，则反转布尔值
                if (parameter is string param && param.ToLower() == "invert")
                {
                    boolValue = !boolValue;
                }
                return boolValue ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                var result = visibility == Visibility.Visible;
                // 如果参数是"invert"，则反转布尔值
                if (parameter is string param && param.ToLower() == "invert")
                {
                    result = !result;
                }
                return result;
            }
            return false;
        }
    }

    /// <summary>
    /// 布尔值到图标转换器
    /// </summary>
    public class BoolToIconConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? PackIconKind.Check : PackIconKind.Close;
            }
            return PackIconKind.Close;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 布尔值到画刷转换器
    /// </summary>
    public class BoolToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? Brushes.Green : Brushes.Red;
            }
            return Brushes.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 布尔值到状态画刷转换器
    /// </summary>
    public class BoolToStatusBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? new SolidColorBrush(Color.FromRgb(76, 175, 80)) : new SolidColorBrush(Color.FromRgb(244, 67, 54));
            }
            return new SolidColorBrush(Color.FromRgb(158, 158, 158));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 布尔值到状态文本转换器
    /// </summary>
    public class BoolToStatusTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return boolValue ? "启用" : "禁用";
            }
            return "未知";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 状态到画刷转换器
    /// </summary>
    public class StatusToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string status)
            {
                return status switch
                {
                    "Active" => new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                    "Inactive" => new SolidColorBrush(Color.FromRgb(158, 158, 158)),
                    "Locked" => new SolidColorBrush(Color.FromRgb(244, 67, 54)),
                    "活跃" => new SolidColorBrush(Color.FromRgb(76, 175, 80)),
                    "非活跃" => new SolidColorBrush(Color.FromRgb(158, 158, 158)),
                    "锁定" => new SolidColorBrush(Color.FromRgb(244, 67, 54)),
                    _ => new SolidColorBrush(Color.FromRgb(158, 158, 158))
                };
            }
            return new SolidColorBrush(Color.FromRgb(158, 158, 158));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 枚举到布尔值转换器
    /// </summary>
    public class EnumToBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return false;

            string checkValue = value.ToString();
            string targetValue = parameter.ToString();
            return checkValue.Equals(targetValue, StringComparison.InvariantCultureIgnoreCase);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null || parameter == null)
                return null;

            bool useValue = (bool)value;
            if (useValue)
                return Enum.Parse(targetType, parameter.ToString());
            return null;
        }
    }



    /// <summary>
    /// 卡片宽度转换器 - 根据可用宽度动态计算卡片宽度
    /// </summary>
    public class CardWidthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double availableWidth && availableWidth > 0)
            {
                // 计算每行能放置的卡片数量
                // 考虑边距：左右各8px（主窗体）+ 20px（HomePage）+ 6px（卡片间距）
                double totalMargin = 8 + 20 + 6;
                double effectiveWidth = availableWidth - totalMargin;
                
                // 最小卡片宽度240px，最大320px
                double minCardWidth = 240;
                double maxCardWidth = 320;
                
                // 计算每行能放置的卡片数量
                int cardsPerRow = Math.Max(1, (int)(effectiveWidth / (minCardWidth + 6))); // 6px是卡片间距
                
                // 计算实际卡片宽度
                double cardWidth = (effectiveWidth - (cardsPerRow - 1) * 6) / cardsPerRow;
                
                // 确保卡片宽度在合理范围内
                cardWidth = Math.Max(minCardWidth, Math.Min(maxCardWidth, cardWidth));
                
                return cardWidth;
            }
            return 280.0; // 默认宽度
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 文本宽度转换器 - 根据卡片宽度计算文本最大宽度
    /// </summary>
    public class TextWidthConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double cardWidth && cardWidth > 0)
            {
                // 文本宽度 = 卡片宽度 - 左右边距 - 额外安全边距
                // 卡片左右边距：18px
                // 额外安全边距：16px（确保文本不会贴边）
                double textWidth = cardWidth - 18 * 2 - 16;
                return Math.Max(100, textWidth); // 最小文本宽度100px
            }
            return 244.0; // 默认文本宽度
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 作业是否包含Excel导入步骤的转换器
    /// </summary>
    public class JobHasExcelImportStepsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 这里我们需要通过反射或其他方式来检查作业是否包含Excel导入步骤
            // 由于无法直接访问JobConfig类型，我们返回一个默认值
            // 实际使用时，这个转换器应该被替换为更合适的实现
            return false; // 默认返回false，表示不包含Excel导入步骤
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 作业是否包含Excel导入步骤到图标颜色的转换器
    /// </summary>
    public class HasExcelImportStepsToIconColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 通过parameter参数传递是否包含Excel导入步骤的信息
            // parameter应该是一个布尔值或字符串"true"/"false"
            bool hasExcelImport = false;
            
            if (parameter is bool boolParam)
            {
                hasExcelImport = boolParam;
            }
            else if (parameter is string stringParam)
            {
                bool.TryParse(stringParam, out hasExcelImport);
            }
            
            // 如果包含Excel导入步骤，返回绿色；否则返回默认的紫色
            return hasExcelImport 
                ? new SolidColorBrush(Color.FromRgb(76, 175, 80)) // #4CAF50 绿色
                : new SolidColorBrush(Color.FromRgb(156, 39, 176)); // #9C27B0 紫色
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 密码强度到画刷转换器
    /// </summary>
    public class PasswordStrengthToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is PasswordStrength strength)
            {
                return strength switch
                {
                    PasswordStrength.Weak => new SolidColorBrush(Color.FromRgb(244, 67, 54)), // 红色
                    PasswordStrength.Medium => new SolidColorBrush(Color.FromRgb(255, 152, 0)), // 橙色
                    PasswordStrength.Strong => new SolidColorBrush(Color.FromRgb(76, 175, 80)), // 绿色
                    PasswordStrength.VeryStrong => new SolidColorBrush(Color.FromRgb(33, 150, 243)), // 蓝色
                    _ => new SolidColorBrush(Color.FromRgb(158, 158, 158)) // 灰色
                };
            }
            return new SolidColorBrush(Color.FromRgb(158, 158, 158));
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 密码强度到文本转换器
    /// </summary>
    public class PasswordStrengthToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is PasswordStrength strength)
            {
                return strength switch
                {
                    PasswordStrength.Weak => "弱",
                    PasswordStrength.Medium => "中等",
                    PasswordStrength.Strong => "强",
                    PasswordStrength.VeryStrong => "很强",
                    _ => "未知"
                };
            }
            return "未知";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 反向布尔值转换器
    /// </summary>
    public class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return true;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                return !boolValue;
            }
            return false;
        }
    }


    /// <summary>
    /// 布尔值到排序方向转换器
    /// </summary>
    public class BoolToSortDirectionConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool ascending)
            {
                return ascending ? "↑" : "↓";
            }
            return "↓";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

} 