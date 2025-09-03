using System.Windows;
using ExcelProcessor.WPF.ViewModels;

namespace ExcelProcessor.WPF.Dialogs
{
    /// <summary>
    /// 作业配置包预览对话框
    /// </summary>
    public partial class JobPackagePreviewDialog : Window
    {
        public JobPackagePreviewDialog(JobPackagePreviewInfo previewInfo)
        {
            InitializeComponent();
            DataContext = previewInfo;
        }

        /// <summary>
        /// 关闭按钮点击事件
        /// </summary>
        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
} 