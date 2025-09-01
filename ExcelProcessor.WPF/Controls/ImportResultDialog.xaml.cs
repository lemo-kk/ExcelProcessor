using System;
using System.Collections.Generic;
using System.Windows;

namespace ExcelProcessor.WPF.Controls
{
    public partial class ImportResultDialog : Window
    {
        private string _targetOpenPath;

        public ImportResultDialog()
        {
            InitializeComponent();
        }

        public void SetResult(bool isSuccess, string targetTable, int totalRows, int successRows, int failedRows, int skippedRows, TimeSpan duration, IList<string> warnings = null, string targetOpenPath = null)
        {
            TitleText.Text = isSuccess ? "✅ 数据导入成功" : "❌ 数据导入失败";

            SummaryText.Text = isSuccess
                ? "数据已成功导入到数据库。"
                : "导入过程中发生错误，部分或全部数据未能导入。";

            TotalRowsText.Text = totalRows.ToString();
            SuccessRowsText.Text = successRows.ToString();
            FailedRowsText.Text = failedRows.ToString();
            SkippedRowsText.Text = skippedRows.ToString();
            TargetTableText.Text = targetTable ?? string.Empty;
            DurationSecondsText.Text = duration.TotalSeconds.ToString("F2");

            _targetOpenPath = targetOpenPath;
            if (!string.IsNullOrWhiteSpace(_targetOpenPath))
            {
                OpenDestinationButton.Visibility = Visibility.Visible;
                OpenDestinationButton.Click += (s, e) =>
                {
                    try
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = _targetOpenPath,
                            UseShellExecute = true
                        });
                    }
                    catch { }
                };
            }

            if (warnings != null && warnings.Count > 0)
            {
                WarningsList.ItemsSource = warnings;
                WarningsList.Visibility = Visibility.Visible;
                WarningsHeader.Visibility = Visibility.Visible;
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
} 