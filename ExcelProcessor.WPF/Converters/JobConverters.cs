using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using ExcelProcessor.Models;

namespace ExcelProcessor.WPF.Converters
{
    /// <summary>
    /// 作业状态到颜色的转换器
    /// </summary>
    public class JobStatusToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is JobStatus status)
            {
                return status switch
                {
                    JobStatus.Running => new SolidColorBrush(Color.FromRgb(33, 150, 243)), // #2196F3
                    JobStatus.Completed => new SolidColorBrush(Color.FromRgb(76, 175, 80)), // #4CAF50
                    JobStatus.Failed => new SolidColorBrush(Color.FromRgb(255, 107, 107)), // #FF6B6B
                    JobStatus.Paused => new SolidColorBrush(Color.FromRgb(158, 158, 158)), // #9E9E9E
                    JobStatus.Pending => new SolidColorBrush(Color.FromRgb(255, 193, 7)), // #FFC107
                    _ => new SolidColorBrush(Color.FromRgb(158, 158, 158)) // #9E9E9E
                };
            }
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 作业状态到文本的转换器
    /// </summary>
    public class JobStatusToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is JobStatus status)
            {
                return status switch
                {
                    JobStatus.Running => "运行中",
                    JobStatus.Completed => "已完成",
                    JobStatus.Failed => "失败",
                    JobStatus.Paused => "暂停",
                    JobStatus.Pending => "待执行",
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
    /// 作业优先级到文本的转换器
    /// </summary>
    public class JobPriorityToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is JobPriority priority)
            {
                return priority switch
                {
                    JobPriority.Low => "低",
                    JobPriority.Normal => "普通",
                    JobPriority.High => "高",
                    JobPriority.Urgent => "紧急",
                    _ => "普通"
                };
            }
            return "普通";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 作业优先级到颜色的转换器
    /// </summary>
    public class JobPriorityToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is JobPriority priority)
            {
                return priority switch
                {
                    JobPriority.Low => new SolidColorBrush(Color.FromRgb(158, 158, 158)), // #9E9E9E
                    JobPriority.Normal => new SolidColorBrush(Color.FromRgb(76, 175, 80)), // #4CAF50
                    JobPriority.High => new SolidColorBrush(Color.FromRgb(255, 152, 0)), // #FF9800
                    JobPriority.Urgent => new SolidColorBrush(Color.FromRgb(244, 67, 54)), // #F44336
                    _ => new SolidColorBrush(Color.FromRgb(76, 175, 80)) // #4CAF50
                };
            }
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 作业状态到进度条颜色的转换器
    /// </summary>
    public class JobStatusToProgressColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is JobStatus status)
            {
                return status switch
                {
                    JobStatus.Running => new SolidColorBrush(Color.FromRgb(33, 150, 243)), // #2196F3
                    JobStatus.Completed => new SolidColorBrush(Color.FromRgb(76, 175, 80)), // #4CAF50
                    JobStatus.Failed => new SolidColorBrush(Color.FromRgb(255, 107, 107)), // #FF6B6B
                    JobStatus.Paused => new SolidColorBrush(Color.FromRgb(158, 158, 158)), // #9E9E9E
                    JobStatus.Pending => new SolidColorBrush(Color.FromRgb(255, 193, 7)), // #FFC107
                    _ => new SolidColorBrush(Color.FromRgb(158, 158, 158)) // #9E9E9E
                };
            }
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 作业状态到运行按钮可见性的转换器
    /// </summary>
    public class JobStatusToRunButtonVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is JobStatus status)
            {
                return status switch
                {
                    JobStatus.Pending => Visibility.Visible,
                    JobStatus.Completed => Visibility.Visible,
                    JobStatus.Failed => Visibility.Visible,
                    JobStatus.Paused => Visibility.Visible,
                    _ => Visibility.Collapsed
                };
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 作业状态到暂停按钮可见性的转换器
    /// </summary>
    public class JobStatusToPauseButtonVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is JobStatus status)
            {
                return status == JobStatus.Running ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 作业状态到恢复按钮可见性的转换器
    /// </summary>
    public class JobStatusToResumeButtonVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is JobStatus status)
            {
                return status == JobStatus.Paused ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 作业状态到重试按钮可见性的转换器
    /// </summary>
    public class JobStatusToRetryButtonVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is JobStatus status)
            {
                return status == JobStatus.Failed ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 布尔值到启用状态文本的转换器
    /// </summary>
    public class BoolToEnabledTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isEnabled)
            {
                return isEnabled ? "已启用" : "已禁用";
            }
            return "未知";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 布尔值到启用状态颜色的转换器
    /// </summary>
    public class BoolToEnabledColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isEnabled)
            {
                return isEnabled 
                    ? new SolidColorBrush(Color.FromRgb(76, 175, 80)) // #4CAF50
                    : new SolidColorBrush(Color.FromRgb(158, 158, 158)); // #9E9E9E
            }
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 执行模式到文本的转换器
    /// </summary>
    public class ExecutionModeToTextConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ExecutionMode executionMode)
            {
                return executionMode switch
                {
                    ExecutionMode.Manual => "手动执行",
                    ExecutionMode.Scheduled => "调度执行",
                    ExecutionMode.Triggered => "触发执行",
                    ExecutionMode.EventDriven => "事件驱动",
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
    /// 手动执行模式到可见性的转换器
    /// </summary>
    public class ManualExecutionModeToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is ExecutionMode executionMode)
            {
                return executionMode == ExecutionMode.Manual ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// 简单模式手动执行到可见性的转换器
    /// 只有当执行模式是手动执行且频次模式是简单模式时才显示
    /// </summary>
    public class SimpleManualExecutionToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is JobConfig jobConfig)
            {
                // 只有当执行模式是手动执行且频次模式是简单模式时才显示
                return (jobConfig.ExecutionMode == ExecutionMode.Manual && 
                       jobConfig.FrequencyMode == ExecutionFrequencyMode.Simple) 
                       ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
} 
 