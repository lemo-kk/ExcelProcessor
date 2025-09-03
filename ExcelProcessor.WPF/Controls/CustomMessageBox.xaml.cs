using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ExcelProcessor.WPF.Controls
{
    /// <summary>
    /// 自定义消息框控件
    /// </summary>
    public partial class CustomMessageBox : UserControl
    {
        public enum MessageBoxType
        {
            Information,
            Warning,
            Error,
            Success,
            Question
        }

        public enum MessageBoxButton
        {
            OK,
            OKCancel,
            YesNo,
            YesNoCancel,
            RetryCancel,
            RetryIgnoreCancel
        }

        public enum MessageBoxResult
        {
            None,
            OK,
            Cancel,
            Yes,
            No,
            Retry,
            Ignore
        }

        private MessageBoxResult _result = MessageBoxResult.None;
        private Action<MessageBoxResult> _callback;

        public CustomMessageBox()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 显示消息框
        /// </summary>
        public static MessageBoxResult Show(string message, string title = "消息", 
            MessageBoxType type = MessageBoxType.Information, 
            MessageBoxButton buttons = MessageBoxButton.OK)
        {
            var messageBox = new CustomMessageBox();
            return messageBox.ShowInternal(message, title, type, buttons);
        }

        /// <summary>
        /// 显示消息框（异步）
        /// </summary>
        public static void ShowAsync(string message, string title, 
            MessageBoxType type, MessageBoxButton buttons, 
            Action<MessageBoxResult> callback)
        {
            var messageBox = new CustomMessageBox();
            messageBox.ShowInternalAsync(message, title, type, buttons, callback);
        }

        /// <summary>
        /// 内部显示方法
        /// </summary>
        private MessageBoxResult ShowInternal(string message, string title, 
            MessageBoxType type, MessageBoxButton buttons)
        {
            SetupMessageBox(message, title, type, buttons);

            var window = new Window
            {
                Title = title,
                Content = this,
                Width = 450,
                Height = 250,
                MinWidth = 400,
                MinHeight = 200,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.CanResize,
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = Brushes.Transparent,
                ShowInTaskbar = false,
                Topmost = true
            };

            window.ShowDialog();
            return _result;
        }

        /// <summary>
        /// 内部异步显示方法
        /// </summary>
        private void ShowInternalAsync(string message, string title, 
            MessageBoxType type, MessageBoxButton buttons, 
            Action<MessageBoxResult> callback)
        {
            _callback = callback;
            SetupMessageBox(message, title, type, buttons);

            var window = new Window
            {
                Title = title,
                Content = this,
                Width = 450,
                Height = 250,
                MinWidth = 400,
                MinHeight = 200,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                ResizeMode = ResizeMode.CanResize,
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Background = Brushes.Transparent,
                ShowInTaskbar = false,
                Topmost = true
            };

            window.Show();
        }

        /// <summary>
        /// 设置消息框内容
        /// </summary>
        private void SetupMessageBox(string message, string title, 
            MessageBoxType type, MessageBoxButton buttons)
        {
            // 设置标题和内容
            TitleText.Text = title;
            ContentText.Text = message;

            // 设置图标
            SetIcon(type);

            // 设置按钮
            SetButtons(buttons);
        }

        /// <summary>
        /// 设置图标
        /// </summary>
        private void SetIcon(MessageBoxType type)
        {
            // 隐藏所有图标
            InfoIcon.Visibility = Visibility.Collapsed;
            WarningIcon.Visibility = Visibility.Collapsed;
            ErrorIcon.Visibility = Visibility.Collapsed;
            SuccessIcon.Visibility = Visibility.Collapsed;
            QuestionIcon.Visibility = Visibility.Collapsed;

            // 显示对应图标
            switch (type)
            {
                case MessageBoxType.Information:
                    InfoIcon.Visibility = Visibility.Visible;
                    break;
                case MessageBoxType.Warning:
                    WarningIcon.Visibility = Visibility.Visible;
                    break;
                case MessageBoxType.Error:
                    ErrorIcon.Visibility = Visibility.Visible;
                    break;
                case MessageBoxType.Success:
                    SuccessIcon.Visibility = Visibility.Visible;
                    break;
                case MessageBoxType.Question:
                    QuestionIcon.Visibility = Visibility.Visible;
                    break;
            }
        }

        /// <summary>
        /// 设置按钮
        /// </summary>
        private void SetButtons(MessageBoxButton buttons)
        {
            // 隐藏所有按钮
            OkButton.Visibility = Visibility.Collapsed;
            YesButton.Visibility = Visibility.Collapsed;
            NoButton.Visibility = Visibility.Collapsed;
            CancelButton.Visibility = Visibility.Collapsed;
            RetryButton.Visibility = Visibility.Collapsed;
            IgnoreButton.Visibility = Visibility.Collapsed;

            // 显示对应按钮
            switch (buttons)
            {
                case MessageBoxButton.OK:
                    OkButton.Visibility = Visibility.Visible;
                    break;
                case MessageBoxButton.OKCancel:
                    OkButton.Visibility = Visibility.Visible;
                    CancelButton.Visibility = Visibility.Visible;
                    break;
                case MessageBoxButton.YesNo:
                    YesButton.Visibility = Visibility.Visible;
                    NoButton.Visibility = Visibility.Visible;
                    break;
                case MessageBoxButton.YesNoCancel:
                    YesButton.Visibility = Visibility.Visible;
                    NoButton.Visibility = Visibility.Visible;
                    CancelButton.Visibility = Visibility.Visible;
                    break;
                case MessageBoxButton.RetryCancel:
                    RetryButton.Visibility = Visibility.Visible;
                    CancelButton.Visibility = Visibility.Visible;
                    break;
                case MessageBoxButton.RetryIgnoreCancel:
                    RetryButton.Visibility = Visibility.Visible;
                    IgnoreButton.Visibility = Visibility.Visible;
                    CancelButton.Visibility = Visibility.Visible;
                    break;
            }
        }

        /// <summary>
        /// 设置结果并关闭窗口
        /// </summary>
        private void SetResultAndClose(MessageBoxResult result)
        {
            _result = result;
            
            if (_callback != null)
            {
                _callback(result);
            }

            // 关闭父窗口
            var window = Window.GetWindow(this);
            if (window != null)
            {
                window.Close();
            }
        }

        // 按钮事件处理
        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            SetResultAndClose(MessageBoxResult.OK);
        }

        private void YesButton_Click(object sender, RoutedEventArgs e)
        {
            SetResultAndClose(MessageBoxResult.Yes);
        }

        private void NoButton_Click(object sender, RoutedEventArgs e)
        {
            SetResultAndClose(MessageBoxResult.No);
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            SetResultAndClose(MessageBoxResult.Cancel);
        }

        private void RetryButton_Click(object sender, RoutedEventArgs e)
        {
            SetResultAndClose(MessageBoxResult.Retry);
        }

        private void IgnoreButton_Click(object sender, RoutedEventArgs e)
        {
            SetResultAndClose(MessageBoxResult.Ignore);
        }
    }
} 