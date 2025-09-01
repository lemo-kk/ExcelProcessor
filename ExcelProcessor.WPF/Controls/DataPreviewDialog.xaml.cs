using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ExcelProcessor.WPF.Controls
{
    public partial class DataPreviewDialog : Window
    {
        private List<Dictionary<string, object>> _previewData;
        private int _headerRowNumber;

        public DataPreviewDialog(List<Dictionary<string, object>> previewData, int headerRowNumber = 1)
        {
            InitializeComponent();
            _previewData = previewData;
            _headerRowNumber = headerRowNumber;
            LoadPreviewData();
        }

        private void LoadPreviewData()
        {
            if (_previewData == null || !_previewData.Any())
            {
                MessageBox.Show("没有数据可预览", "提示", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // 更新标题
            Title = $"数据预览 - 标题行：第{_headerRowNumber}行";
            
            // 更新信息标签
            if (InfoTextBlock != null)
            {
                InfoTextBlock.Text = $"显示第{_headerRowNumber}行（标题行）及之后共{_previewData.Count}行数据";
            }

            // 创建包含行号的数据
            var dataWithRowNumbers = _previewData.Select((row, index) => 
            {
                var newRow = new Dictionary<string, object>();
                
                // 使用原始行号，如果没有则使用计算的行号
                if (row.ContainsKey("原始行号"))
                {
                    newRow["行号"] = row["原始行号"];
                }
                else
                {
                    newRow["行号"] = _headerRowNumber + index;
                }
                
                foreach (var kvp in row)
                {
                    // 跳过原始行号，因为我们已经处理了
                    if (kvp.Key != "原始行号")
                    {
                        newRow[kvp.Key] = kvp.Value;
                    }
                }
                
                return newRow;
            }).ToList();

            // 获取所有列名
            var allColumns = dataWithRowNumbers.First().Keys.ToList();

            // 创建列
            PreviewDataGrid.Columns.Clear();
            
            foreach (var columnName in allColumns)
            {
                var column = new DataGridTextColumn
                {
                    Header = columnName,
                    Binding = new System.Windows.Data.Binding($"[{columnName}]"),
                    Width = columnName == "行号" ? 60 : 120,
                    IsReadOnly = true
                };
                
                // 行号列居中显示
                if (columnName == "行号")
                {
                    column.ElementStyle = new Style(typeof(TextBlock));
                    column.ElementStyle.Setters.Add(new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Center));
                }
                
                PreviewDataGrid.Columns.Add(column);
            }

            // 设置数据源
            PreviewDataGrid.ItemsSource = dataWithRowNumbers;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void CloseDialogButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadPreviewData();
        }
    }
} 