using System.Windows;
using System.Windows.Threading;

namespace ExcelProcessor.WPF.Controls
{
    /// <summary>
    /// 导入进度对话框
    /// </summary>
    public partial class ImportProgressDialog : Window
    {
        public ImportProgressDialog(string title = "正在导入Excel数据...")
        {
            InitializeComponent();
            TitleText.Text = title;
        }

        /// <summary>
        /// 设置标题
        /// </summary>
        public void SetTitle(string title)
        {
            Dispatcher.Invoke(() => TitleText.Text = title);
        }

        /// <summary>
        /// 设置进度文本
        /// </summary>
        public void SetProgressText(string text)
        {
            Dispatcher.Invoke(() => ProgressText.Text = text);
        }

        /// <summary>
        /// 设置进度（0-100）
        /// </summary>
        public void SetProgress(double progress)
        {
            Dispatcher.Invoke(() =>
            {
                ProgressBar.IsIndeterminate = false;
                ProgressBar.Value = progress;
                PercentText.Text = $"{progress:0}%";
            });
        }

        /// <summary>
        /// 设置总行数
        /// </summary>
        public void SetTotalRows(int totalRows)
        {
            Dispatcher.Invoke(() => TotalRowsText.Text = totalRows.ToString());
        }

        /// <summary>
        /// 设置已处理行数
        /// </summary>
        public void SetProcessedRows(int processedRows)
        {
            Dispatcher.Invoke(() => ProcessedRowsText.Text = processedRows.ToString());
        }

        /// <summary>
        /// 设置成功行数
        /// </summary>
        public void SetSuccessRows(int successRows)
        {
            Dispatcher.Invoke(() => SuccessRowsText.Text = successRows.ToString());
        }

        /// <summary>
        /// 设置失败行数
        /// </summary>
        public void SetFailedRows(int failedRows)
        {
            Dispatcher.Invoke(() => FailedRowsText.Text = failedRows.ToString());
        }

        /// <summary>
        /// 设置当前处理行信息
        /// </summary>
        public void SetCurrentRow(string currentRowInfo)
        {
            Dispatcher.Invoke(() => CurrentRowText.Text = currentRowInfo);
        }

        /// <summary>
        /// 设置批次信息
        /// </summary>
        public void SetBatchInfo(string batchInfo)
        {
            Dispatcher.Invoke(() => BatchInfoText.Text = batchInfo);
        }

        /// <summary>
        /// 设置状态信息
        /// </summary>
        public void SetStatus(string status)
        {
            Dispatcher.Invoke(() => StatusText.Text = status);
        }

        /// <summary>
        /// 更新所有统计信息
        /// </summary>
        public void UpdateStatistics(int totalRows, int processedRows, int successRows, int failedRows)
        {
            Dispatcher.Invoke(() =>
            {
                TotalRowsText.Text = totalRows.ToString();
                ProcessedRowsText.Text = processedRows.ToString();
                SuccessRowsText.Text = successRows.ToString();
                FailedRowsText.Text = failedRows.ToString();
            });
        }

        /// <summary>
        /// 显示错误信息（保持在进度视图上）
        /// </summary>
        public void ShowError(string message)
        {
            Dispatcher.Invoke(() =>
            {
                ProgressBar.IsIndeterminate = false;
                StatusText.Text = message;
                StatusText.Foreground = System.Windows.Media.Brushes.IndianRed;
            });
        }
    }
} 