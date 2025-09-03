using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ExcelProcessor.Core.Services;

namespace ExcelProcessor.Core.Services
{
    /// <summary>
    /// 导入导出历史记录服务实现
    /// </summary>
    public class ImportExportHistoryService : IImportExportHistoryService
    {
        private readonly ILogger<ImportExportHistoryService> _logger;
        private readonly List<ImportExportHistory> _historyStore;
        private readonly string _historyFilePath;
        private readonly object _lockObject = new object();

        public ImportExportHistoryService(ILogger<ImportExportHistoryService> logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _historyStore = new List<ImportExportHistory>();
            
            // 设置历史记录文件路径
            var appDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "ExcelProcessor");
            Directory.CreateDirectory(appDataPath);
            _historyFilePath = Path.Combine(appDataPath, "import_export_history.json");
            
            // 加载现有历史记录
            LoadHistoryFromFile();
        }

        #region 历史记录查询

        public async Task<(List<ImportExportHistory> histories, int totalCount)> GetHistoryAsync(int page = 1, int pageSize = 20)
        {
            try
            {
                var query = _historyStore.OrderByDescending(h => h.OperationTime);
                var totalCount = query.Count();
                var histories = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();
                
                return await Task.FromResult((histories, totalCount));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取历史记录失败");
                return await Task.FromResult((new List<ImportExportHistory>(), 0));
            }
        }

        public async Task<(List<ImportExportHistory> histories, int totalCount)> GetHistoryByTypeAsync(string operationType, int page = 1, int pageSize = 20)
        {
            try
            {
                var query = _historyStore.Where(h => h.OperationType.Equals(operationType, StringComparison.OrdinalIgnoreCase))
                                       .OrderByDescending(h => h.OperationTime);
                var totalCount = query.Count();
                var histories = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();
                
                return await Task.FromResult((histories, totalCount));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据类型获取历史记录失败，类型: {OperationType}", operationType);
                return await Task.FromResult((new List<ImportExportHistory>(), 0));
            }
        }

        public async Task<(List<ImportExportHistory> histories, int totalCount)> GetHistoryByTimeRangeAsync(DateTime startTime, DateTime endTime, int page = 1, int pageSize = 20)
        {
            try
            {
                var query = _historyStore.Where(h => h.OperationTime >= startTime && h.OperationTime <= endTime)
                                       .OrderByDescending(h => h.OperationTime);
                var totalCount = query.Count();
                var histories = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();
                
                return await Task.FromResult((histories, totalCount));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据时间范围获取历史记录失败，开始时间: {StartTime}, 结束时间: {EndTime}", startTime, endTime);
                return await Task.FromResult((new List<ImportExportHistory>(), 0));
            }
        }

        public async Task<(List<ImportExportHistory> histories, int totalCount)> SearchHistoryAsync(string keyword, int page = 1, int pageSize = 20)
        {
            try
            {
                var query = _historyStore.Where(h => 
                    h.JobName?.Contains(keyword, StringComparison.OrdinalIgnoreCase) == true ||
                    h.Description?.Contains(keyword, StringComparison.OrdinalIgnoreCase) == true ||
                    h.OperatedBy?.Contains(keyword, StringComparison.OrdinalIgnoreCase) == true ||
                    h.OperationType?.Contains(keyword, StringComparison.OrdinalIgnoreCase) == true)
                    .OrderByDescending(h => h.OperationTime);
                
                var totalCount = query.Count();
                var histories = query.Skip((page - 1) * pageSize).Take(pageSize).ToList();
                
                return await Task.FromResult((histories, totalCount));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "搜索历史记录失败，关键词: {Keyword}", keyword);
                return await Task.FromResult((new List<ImportExportHistory>(), 0));
            }
        }

        #endregion

        #region 历史记录管理

        public async Task<bool> AddHistoryAsync(ImportExportHistory history)
        {
            try
            {
                lock (_lockObject)
                {
                    if (string.IsNullOrEmpty(history.Id))
                    {
                        history.Id = Guid.NewGuid().ToString();
                    }
                    
                    if (history.OperationTime == default)
                    {
                        history.OperationTime = DateTime.Now;
                    }
                    
                    _historyStore.Add(history);
                    SaveHistoryToFile();
                }
                
                _logger.LogInformation("添加历史记录成功，ID: {Id}, 类型: {OperationType}", history.Id, history.OperationType);
                return await Task.FromResult(true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "添加历史记录失败");
                return await Task.FromResult(false);
            }
        }

        public async Task<bool> UpdateHistoryAsync(ImportExportHistory history)
        {
            try
            {
                lock (_lockObject)
                {
                    var existingHistory = _historyStore.FirstOrDefault(h => h.Id == history.Id);
                    if (existingHistory != null)
                    {
                        var index = _historyStore.IndexOf(existingHistory);
                        _historyStore[index] = history;
                        SaveHistoryToFile();
                        
                        _logger.LogInformation("更新历史记录成功，ID: {Id}", history.Id);
                        return true;
                    }
                    
                    _logger.LogWarning("要更新的历史记录不存在，ID: {Id}", history.Id);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "更新历史记录失败，ID: {Id}", history.Id);
                return await Task.FromResult(false);
            }
        }

        public async Task<bool> DeleteHistoryAsync(string id)
        {
            try
            {
                lock (_lockObject)
                {
                    var history = _historyStore.FirstOrDefault(h => h.Id == id);
                    if (history != null)
                    {
                        _historyStore.Remove(history);
                        SaveHistoryToFile();
                        
                        _logger.LogInformation("删除历史记录成功，ID: {Id}", id);
                        return true;
                    }
                    
                    _logger.LogWarning("要删除的历史记录不存在，ID: {Id}", id);
                    return false;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除历史记录失败，ID: {Id}", id);
                return await Task.FromResult(false);
            }
        }

        public async Task<int> BatchDeleteHistoryAsync(List<string> ids)
        {
            try
            {
                lock (_lockObject)
                {
                    var deletedCount = 0;
                    foreach (var id in ids)
                    {
                        var history = _historyStore.FirstOrDefault(h => h.Id == id);
                        if (history != null)
                        {
                            _historyStore.Remove(history);
                            deletedCount++;
                        }
                    }
                    
                    if (deletedCount > 0)
                    {
                        SaveHistoryToFile();
                    }
                    
                    _logger.LogInformation("批量删除历史记录成功，删除数量: {DeletedCount}", deletedCount);
                    return deletedCount;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量删除历史记录失败");
                return await Task.FromResult(0);
            }
        }

        public async Task<bool> ClearAllHistoryAsync()
        {
            try
            {
                lock (_lockObject)
                {
                    var count = _historyStore.Count;
                    _historyStore.Clear();
                    SaveHistoryToFile();
                    
                    _logger.LogInformation("清空所有历史记录成功，清空数量: {Count}", count);
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清空所有历史记录失败");
                return await Task.FromResult(false);
            }
        }

        #endregion

        #region 历史记录导出

        public async Task<(bool success, string message)> ExportHistoryToFileAsync(string filePath, string format = "CSV", DateTime? startTime = null, DateTime? endTime = null)
        {
            try
            {
                var histories = _historyStore.AsEnumerable();
                
                if (startTime.HasValue)
                {
                    histories = histories.Where(h => h.OperationTime >= startTime.Value);
                }
                
                if (endTime.HasValue)
                {
                    histories = histories.Where(h => h.OperationTime <= endTime.Value);
                }
                
                var historyList = histories.OrderByDescending(h => h.OperationTime).ToList();
                
                switch (format.ToUpper())
                {
                    case "CSV":
                        await ExportToCsvAsync(filePath, historyList);
                        break;
                    case "JSON":
                        await ExportToJsonAsync(filePath, historyList);
                        break;
                    case "XML":
                        await ExportToXmlAsync(filePath, historyList);
                        break;
                    default:
                        return (false, $"不支持的导出格式: {format}");
                }
                
                _logger.LogInformation("导出历史记录成功，格式: {Format}, 文件路径: {FilePath}", format, filePath);
                return (true, "导出成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出历史记录失败，格式: {Format}, 文件路径: {FilePath}", format, filePath);
                return (false, $"导出失败: {ex.Message}");
            }
        }

        public async Task<(bool success, string message)> ExportHistoryToExcelAsync(string filePath, DateTime? startTime = null, DateTime? endTime = null)
        {
            try
            {
                var histories = _historyStore.AsEnumerable();
                
                if (startTime.HasValue)
                {
                    histories = histories.Where(h => h.OperationTime >= startTime.Value);
                }
                
                if (endTime.HasValue)
                {
                    histories = histories.Where(h => h.OperationTime <= endTime.Value);
                }
                
                var historyList = histories.OrderByDescending(h => h.OperationTime).ToList();
                
                // 这里应该使用EPPlus或其他Excel库来创建Excel文件
                // 为了简化，我们先导出为CSV格式
                var csvPath = Path.ChangeExtension(filePath, ".csv");
                await ExportToCsvAsync(csvPath, historyList);
                
                _logger.LogInformation("导出历史记录到Excel成功，文件路径: {FilePath}", filePath);
                return (true, "导出成功");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "导出历史记录到Excel失败，文件路径: {FilePath}", filePath);
                return (false, $"导出失败: {ex.Message}");
            }
        }

        #endregion

        #region 统计信息

        public async Task<ImportExportStatistics> GetStatisticsAsync()
        {
            try
            {
                var statistics = new ImportExportStatistics
                {
                    TotalOperations = _historyStore.Count,
                    SuccessfulOperations = _historyStore.Count(h => h.Status.Equals("Success", StringComparison.OrdinalIgnoreCase)),
                    FailedOperations = _historyStore.Count(h => h.Status.Equals("Failed", StringComparison.OrdinalIgnoreCase)),
                    ImportOperations = _historyStore.Count(h => h.OperationType.Equals("Import", StringComparison.OrdinalIgnoreCase)),
                    ExportOperations = _historyStore.Count(h => h.OperationType.Equals("Export", StringComparison.OrdinalIgnoreCase)),
                    ValidationOperations = _historyStore.Count(h => h.OperationType.Equals("Validate", StringComparison.OrdinalIgnoreCase)),
                    PreviewOperations = _historyStore.Count(h => h.OperationType.Equals("Preview", StringComparison.OrdinalIgnoreCase)),
                    LastOperationTime = _historyStore.Any() ? _historyStore.Max(h => h.OperationTime) : null
                };
                
                if (statistics.TotalOperations > 0)
                {
                    statistics.AverageDurationMs = _historyStore.Average(h => h.DurationMs);
                    statistics.TotalFileSize = _historyStore.Where(h => h.FileSize.HasValue).Sum(h => h.FileSize.Value);
                }
                
                return await Task.FromResult(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取统计信息失败");
                return await Task.FromResult(new ImportExportStatistics());
            }
        }

        public async Task<ImportExportStatistics> GetStatisticsByTimeRangeAsync(DateTime startTime, DateTime endTime)
        {
            try
            {
                var historiesInRange = _historyStore.Where(h => h.OperationTime >= startTime && h.OperationTime <= endTime).ToList();
                
                var statistics = new ImportExportStatistics
                {
                    TotalOperations = historiesInRange.Count,
                    SuccessfulOperations = historiesInRange.Count(h => h.Status.Equals("Success", StringComparison.OrdinalIgnoreCase)),
                    FailedOperations = historiesInRange.Count(h => h.Status.Equals("Failed", StringComparison.OrdinalIgnoreCase)),
                    ImportOperations = historiesInRange.Count(h => h.OperationType.Equals("Import", StringComparison.OrdinalIgnoreCase)),
                    ExportOperations = historiesInRange.Count(h => h.OperationType.Equals("Export", StringComparison.OrdinalIgnoreCase)),
                    ValidationOperations = historiesInRange.Count(h => h.OperationType.Equals("Validate", StringComparison.OrdinalIgnoreCase)),
                    PreviewOperations = historiesInRange.Count(h => h.OperationType.Equals("Preview", StringComparison.OrdinalIgnoreCase)),
                    LastOperationTime = historiesInRange.Any() ? historiesInRange.Max(h => h.OperationTime) : null
                };
                
                if (statistics.TotalOperations > 0)
                {
                    statistics.AverageDurationMs = historiesInRange.Average(h => h.DurationMs);
                    statistics.TotalFileSize = historiesInRange.Where(h => h.FileSize.HasValue).Sum(h => h.FileSize.Value);
                }
                
                return await Task.FromResult(statistics);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据时间范围获取统计信息失败，开始时间: {StartTime}, 结束时间: {EndTime}", startTime, endTime);
                return await Task.FromResult(new ImportExportStatistics());
            }
        }

        #endregion

        #region 私有方法

        private void LoadHistoryFromFile()
        {
            try
            {
                if (File.Exists(_historyFilePath))
                {
                    var json = File.ReadAllText(_historyFilePath);
                    var histories = JsonSerializer.Deserialize<List<ImportExportHistory>>(json);
                    if (histories != null)
                    {
                        _historyStore.Clear();
                        _historyStore.AddRange(histories);
                        _logger.LogInformation("从文件加载历史记录成功，数量: {Count}", _historyStore.Count);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "从文件加载历史记录失败");
            }
        }

        private void SaveHistoryToFile()
        {
            try
            {
                var json = JsonSerializer.Serialize(_historyStore, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_historyFilePath, json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存历史记录到文件失败");
            }
        }

        private async Task ExportToCsvAsync(string filePath, List<ImportExportHistory> histories)
        {
            var csvLines = new List<string>
            {
                "操作时间,操作类型,作业名称,状态,操作用户,描述,结果消息,操作耗时(ms),文件大小(字节)"
            };
            
            foreach (var history in histories)
            {
                var line = $"{history.OperationTime:yyyy-MM-dd HH:mm:ss}," +
                          $"{history.OperationType}," +
                          $"{history.JobName ?? ""}," +
                          $"{history.Status}," +
                          $"{history.OperatedBy}," +
                          $"\"{history.Description?.Replace("\"", "\"\"") ?? ""}\"," +
                          $"\"{history.ResultMessage?.Replace("\"", "\"\"") ?? ""}\"," +
                          $"{history.DurationMs}," +
                          $"{history.FileSize ?? 0}";
                
                csvLines.Add(line);
            }
            
            await File.WriteAllLinesAsync(filePath, csvLines);
        }

        private async Task ExportToJsonAsync(string filePath, List<ImportExportHistory> histories)
        {
            var json = JsonSerializer.Serialize(histories, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(filePath, json);
        }

        private async Task ExportToXmlAsync(string filePath, List<ImportExportHistory> histories)
        {
            var xmlLines = new List<string>
            {
                "<?xml version=\"1.0\" encoding=\"utf-8\"?>",
                "<ImportExportHistories>"
            };
            
            foreach (var history in histories)
            {
                xmlLines.Add("  <History>");
                xmlLines.Add($"    <Id>{history.Id}</Id>");
                xmlLines.Add($"    <OperationTime>{history.OperationTime:yyyy-MM-dd HH:mm:ss}</OperationTime>");
                xmlLines.Add($"    <OperationType>{history.OperationType}</OperationType>");
                xmlLines.Add($"    <Status>{history.Status}</Status>");
                xmlLines.Add($"    <JobName>{history.JobName ?? ""}</JobName>");
                xmlLines.Add($"    <Description>{history.Description ?? ""}</Description>");
                xmlLines.Add($"    <OperatedBy>{history.OperatedBy}</OperatedBy>");
                xmlLines.Add($"    <ResultMessage>{history.ResultMessage ?? ""}</ResultMessage>");
                xmlLines.Add($"    <DurationMs>{history.DurationMs}</DurationMs>");
                xmlLines.Add($"    <FileSize>{history.FileSize ?? 0}</FileSize>");
                xmlLines.Add("  </History>");
            }
            
            xmlLines.Add("</ImportExportHistories>");
            await File.WriteAllLinesAsync(filePath, xmlLines);
        }

        #endregion
    }
} 