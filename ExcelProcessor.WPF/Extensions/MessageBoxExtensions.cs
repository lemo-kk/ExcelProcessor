using System;
using System.Windows;
using ExcelProcessor.WPF.Controls;

namespace ExcelProcessor.WPF.Extensions
{
    /// <summary>
    /// MessageBox扩展方法，提供与系统MessageBox兼容的接口
    /// </summary>
    public static class MessageBoxExtensions
    {
        /// <summary>
        /// 显示信息消息框
        /// </summary>
        public static MessageBoxResult Show(string message, string title = "消息")
        {
            var result = CustomMessageBox.Show(message, title, CustomMessageBox.MessageBoxType.Information, CustomMessageBox.MessageBoxButton.OK);
            return ConvertResult(result);
        }

        /// <summary>
        /// 显示信息消息框
        /// </summary>
        public static MessageBoxResult Show(string message, string title, MessageBoxButton button)
        {
            var customButton = ConvertButton(button);
            var result = CustomMessageBox.Show(message, title, CustomMessageBox.MessageBoxType.Information, customButton);
            return ConvertResult(result);
        }

        /// <summary>
        /// 显示信息消息框
        /// </summary>
        public static MessageBoxResult Show(string message, string title, MessageBoxButton button, MessageBoxImage icon)
        {
            var customButton = ConvertButton(button);
            var customType = ConvertIcon(icon);
            var result = CustomMessageBox.Show(message, title, customType, customButton);
            return ConvertResult(result);
        }

        /// <summary>
        /// 显示信息消息框
        /// </summary>
        public static MessageBoxResult Show(string message, string title, MessageBoxButton button, MessageBoxImage icon, MessageBoxResult defaultResult)
        {
            var customButton = ConvertButton(button);
            var customType = ConvertIcon(icon);
            var result = CustomMessageBox.Show(message, title, customType, customButton);
            return ConvertResult(result);
        }

        /// <summary>
        /// 显示信息消息框
        /// </summary>
        public static MessageBoxResult Show(string message, string title, MessageBoxButton button, MessageBoxImage icon, MessageBoxResult defaultResult, MessageBoxOptions options)
        {
            var customButton = ConvertButton(button);
            var customType = ConvertIcon(icon);
            var result = CustomMessageBox.Show(message, title, customType, customButton);
            return ConvertResult(result);
        }

        /// <summary>
        /// 显示信息消息框
        /// </summary>
        public static MessageBoxResult Show(Window owner, string message, string title = "消息")
        {
            var result = CustomMessageBox.Show(message, title, CustomMessageBox.MessageBoxType.Information, CustomMessageBox.MessageBoxButton.OK);
            return ConvertResult(result);
        }

        /// <summary>
        /// 显示信息消息框
        /// </summary>
        public static MessageBoxResult Show(Window owner, string message, string title, MessageBoxButton button)
        {
            var customButton = ConvertButton(button);
            var result = CustomMessageBox.Show(message, title, CustomMessageBox.MessageBoxType.Information, customButton);
            return ConvertResult(result);
        }

        /// <summary>
        /// 显示信息消息框
        /// </summary>
        public static MessageBoxResult Show(Window owner, string message, string title, MessageBoxButton button, MessageBoxImage icon)
        {
            var customButton = ConvertButton(button);
            var customType = ConvertIcon(icon);
            var result = CustomMessageBox.Show(message, title, customType, customButton);
            return ConvertResult(result);
        }

        /// <summary>
        /// 显示信息消息框
        /// </summary>
        public static MessageBoxResult Show(Window owner, string message, string title, MessageBoxButton button, MessageBoxImage icon, MessageBoxResult defaultResult)
        {
            var customButton = ConvertButton(button);
            var customType = ConvertIcon(icon);
            var result = CustomMessageBox.Show(message, title, customType, customButton);
            return ConvertResult(result);
        }

        /// <summary>
        /// 显示信息消息框
        /// </summary>
        public static MessageBoxResult Show(Window owner, string message, string title, MessageBoxButton button, MessageBoxImage icon, MessageBoxResult defaultResult, MessageBoxOptions options)
        {
            var customButton = ConvertButton(button);
            var customType = ConvertIcon(icon);
            var result = CustomMessageBox.Show(message, title, customType, customButton);
            return ConvertResult(result);
        }

        /// <summary>
        /// 转换按钮类型
        /// </summary>
        private static CustomMessageBox.MessageBoxButton ConvertButton(MessageBoxButton button)
        {
            return button switch
            {
                MessageBoxButton.OK => CustomMessageBox.MessageBoxButton.OK,
                MessageBoxButton.OKCancel => CustomMessageBox.MessageBoxButton.OKCancel,
                MessageBoxButton.YesNo => CustomMessageBox.MessageBoxButton.YesNo,
                MessageBoxButton.YesNoCancel => CustomMessageBox.MessageBoxButton.YesNoCancel,
                _ => CustomMessageBox.MessageBoxButton.OK
            };
        }

        /// <summary>
        /// 转换图标类型
        /// </summary>
        private static CustomMessageBox.MessageBoxType ConvertIcon(MessageBoxImage icon)
        {
            return icon switch
            {
                MessageBoxImage.Information => CustomMessageBox.MessageBoxType.Information,
                MessageBoxImage.Warning => CustomMessageBox.MessageBoxType.Warning,
                MessageBoxImage.Error => CustomMessageBox.MessageBoxType.Error,
                MessageBoxImage.Question => CustomMessageBox.MessageBoxType.Question,
                _ => CustomMessageBox.MessageBoxType.Information
            };
        }

        /// <summary>
        /// 转换结果类型
        /// </summary>
        private static MessageBoxResult ConvertResult(CustomMessageBox.MessageBoxResult result)
        {
            return result switch
            {
                CustomMessageBox.MessageBoxResult.OK => MessageBoxResult.OK,
                CustomMessageBox.MessageBoxResult.Cancel => MessageBoxResult.Cancel,
                CustomMessageBox.MessageBoxResult.Yes => MessageBoxResult.Yes,
                CustomMessageBox.MessageBoxResult.No => MessageBoxResult.No,
                _ => MessageBoxResult.None
            };
        }
    }
} 