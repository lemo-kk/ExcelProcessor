using System.Windows.Controls;

namespace ExcelProcessor.WPF.Helpers
{
    /// <summary>
    /// DataGrid辅助工具类，用于控制DataGrid的列显示
    /// </summary>
    public static class DataGridHelper
    {
        /// <summary>
        /// 确保DataGrid只显示定义的列，防止自动生成额外列
        /// </summary>
        /// <param name="dataGrid">要控制的DataGrid</param>
        public static void EnsureDefinedColumnsOnly(DataGrid dataGrid)
        {
            if (dataGrid == null) return;

            // 禁用自动生成列
            dataGrid.AutoGenerateColumns = false;
            
            // 禁用列重排序
            dataGrid.CanUserReorderColumns = false;
            
            // 确保只显示已定义的列
            dataGrid.HeadersVisibility = DataGridHeadersVisibility.Column;
            
            // 隐藏行头
            dataGrid.RowHeaderWidth = 0;
        }

        /// <summary>
        /// 刷新DataGrid的列显示
        /// </summary>
        /// <param name="dataGrid">要刷新的DataGrid</param>
        public static void RefreshColumns(DataGrid dataGrid)
        {
            if (dataGrid == null) return;

            // 强制刷新列显示
            dataGrid.Columns.Clear();
            
            // 重新应用列定义
            // 注意：这个方法需要在具体的DataGrid中重新定义列
        }

        /// <summary>
        /// 设置DataGrid的基本属性
        /// </summary>
        /// <param name="dataGrid">要设置的DataGrid</param>
        /// <param name="isReadOnly">是否只读</param>
        public static void SetBasicProperties(DataGrid dataGrid, bool isReadOnly = true)
        {
            if (dataGrid == null) return;

            dataGrid.IsReadOnly = isReadOnly;
            dataGrid.CanUserAddRows = false;
            dataGrid.CanUserDeleteRows = false;
            dataGrid.CanUserResizeRows = false;
            dataGrid.CanUserSortColumns = true;
            dataGrid.CanUserResizeColumns = true;
            dataGrid.CanUserReorderColumns = false;
            dataGrid.SelectionMode = DataGridSelectionMode.Extended;
            dataGrid.GridLinesVisibility = DataGridGridLinesVisibility.All;
            dataGrid.HeadersVisibility = DataGridHeadersVisibility.Column;
            dataGrid.RowHeaderWidth = 0;
            dataGrid.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
            dataGrid.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
        }
    }
} 