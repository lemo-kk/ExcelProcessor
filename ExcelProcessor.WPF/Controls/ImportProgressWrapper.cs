using ExcelProcessor.Core.Interfaces;

namespace ExcelProcessor.WPF.Controls
{
    /// <summary>
    /// 导入进度包装器，将进度回调接口与进度对话框连接
    /// </summary>
    public class ImportProgressWrapper : IImportProgressCallback
    {
        private readonly ImportProgressDialog _progressDialog;

        public ImportProgressWrapper(ImportProgressDialog progressDialog)
        {
            _progressDialog = progressDialog;
        }

        public void UpdateProgress(double progress, string message)
        {
            _progressDialog.SetProgress(progress);
            _progressDialog.SetProgressText(message);
        }

        public void UpdateStatistics(int totalRows, int processedRows, int successRows, int failedRows)
        {
            _progressDialog.UpdateStatistics(totalRows, processedRows, successRows, failedRows);
        }

        public void UpdateCurrentRow(int currentRow, int totalRows)
        {
            _progressDialog.SetCurrentRow($"当前处理: 第 {currentRow} 行 / 共 {totalRows} 行");
        }

        public void UpdateBatchInfo(int batchNumber, int batchSize, int totalBatches)
        {
            _progressDialog.SetBatchInfo($"批次: {batchNumber} / {totalBatches} (每批 {batchSize} 行)");
        }

        public void SetStatus(string status)
        {
            _progressDialog.SetStatus(status);
        }
    }
} 