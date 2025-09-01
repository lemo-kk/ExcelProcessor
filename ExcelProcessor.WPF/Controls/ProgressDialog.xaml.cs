using System.Windows;

namespace ExcelProcessor.WPF.Controls
{
    /// <summary>
    /// 进度对话框
    /// </summary>
    public partial class ProgressDialog : Window
    {
        public ProgressDialog(string message = "正在处理...")
        {
            InitializeComponent();
            MessageText.Text = message;
        }

        /// <summary>
        /// 设置消息
        /// </summary>
        public void SetMessage(string message)
        {
            MessageText.Text = message;
        }

        /// <summary>
        /// 设置进度（0-100）
        /// </summary>
        public void SetProgress(double progress)
        {
            ProgressBar.IsIndeterminate = false;
            ProgressBar.Value = progress;
        }
    }
} 