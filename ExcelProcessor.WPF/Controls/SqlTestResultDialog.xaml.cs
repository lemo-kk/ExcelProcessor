using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using ExcelProcessor.Core.Interfaces;

namespace ExcelProcessor.WPF.Controls
{
    /// <summary>
    /// SQL查询测试结果弹窗
    /// </summary>
    public partial class SqlTestResultDialog : Window
    {
        public SqlTestResultDialog()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 显示SQL测试结果
        /// </summary>
        /// <param name="testResult">测试结果</param>
        /// <param name="owner">父窗口</param>
        public static void ShowResult(SqlTestResult testResult, Window owner = null)
        {
            var dialog = new SqlTestResultDialog();
            dialog.Owner = owner;
            dialog.SetResult(testResult);
            dialog.ShowDialog();
        }

        /// <summary>
        /// 设置测试结果
        /// </summary>
        /// <param name="testResult">测试结果</param>
        private void SetResult(SqlTestResult testResult)
        {
            if (testResult.IsSuccess)
            {
                // 成功状态
                StatusIcon.Text = "✅";
                StatusText.Text = "SQL查询测试成功!";
                StatusText.Foreground = System.Windows.Media.Brushes.Green;
                
                // 基本信息
                RowCountText.Text = $"查询数据行数: {testResult.EstimatedRowCount:N0}行";
                DurationText.Text = $"预估执行时间: {testResult.EstimatedDurationMs}ms";
                TestTimeText.Text = $"测试时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
                
                // 显示查询结果
                if (testResult.SampleData != null && testResult.SampleData.Count > 0)
                {
                    // 创建DataTable来显示数据
                    var dataTable = new DataTable();
                    
                    // 添加列
                    if (testResult.Columns != null)
                    {
                        foreach (var column in testResult.Columns)
                        {
                            dataTable.Columns.Add(column.Name, typeof(string));
                        }
                    }
                    
                    // 添加数据行
                    foreach (var row in testResult.SampleData)
                    {
                        var dataRow = dataTable.NewRow();
                        foreach (var kvp in row)
                        {
                            if (dataTable.Columns.Contains(kvp.Key))
                            {
                                dataRow[kvp.Key] = kvp.Value?.ToString() ?? "";
                            }
                        }
                        dataTable.Rows.Add(dataRow);
                    }
                    
                    ResultDataGrid.ItemsSource = dataTable.DefaultView;
                    
                    // 设置数据统计
                    TotalRowsText.Text = $"总行数: {testResult.SampleData.Count}行";
                    var displayCount = Math.Min(testResult.SampleData.Count, 10);
                    DisplayedRowsText.Text = $"显示行数: {displayCount}行 (前{displayCount}条记录)";
                }
                else
                {
                    // 没有数据的情况
                    var emptyData = new DataTable();
                    emptyData.Columns.Add("提示", typeof(string));
                    emptyData.Rows.Add("查询成功，但没有返回数据");
                    ResultDataGrid.ItemsSource = emptyData.DefaultView;
                    
                    TotalRowsText.Text = "总行数: 0行";
                    DisplayedRowsText.Text = "显示行数: 0行";
                }
            }
            else
            {
                // 失败状态
                StatusIcon.Text = "❌";
                StatusText.Text = "SQL查询测试失败!";
                StatusText.Foreground = System.Windows.Media.Brushes.Red;
                
                // 基本信息
                RowCountText.Text = "查询数据行数: 0行";
                DurationText.Text = "预估执行时间: 0ms";
                TestTimeText.Text = $"测试时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}";
                
                // 显示错误信息
                ErrorPanel.Visibility = Visibility.Visible;
                ErrorText.Text = testResult.ErrorMessage;
                
                // 隐藏结果面板
                ResultPanel.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>
        /// 确定按钮点击事件
        /// </summary>
        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
} 