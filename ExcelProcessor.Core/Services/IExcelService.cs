using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ExcelProcessor.Models;

namespace ExcelProcessor.Core.Services
{
    /// <summary>
    /// Excel服务接口
    /// </summary>
    public interface IExcelService
    {
        #region Excel配置管理

        /// <summary>
        /// 创建Excel配置
        /// </summary>
        /// <param name="config">Excel配置</param>
        /// <returns>创建的配置</returns>
        Task<ExcelConfig> CreateExcelConfigAsync(ExcelConfig config);

        /// <summary>
        /// 更新Excel配置
        /// </summary>
        /// <param name="config">Excel配置</param>
        /// <returns>更新后的配置</returns>
        Task<ExcelConfig> UpdateExcelConfigAsync(ExcelConfig config);

        /// <summary>
        /// 删除Excel配置
        /// </summary>
        /// <param name="id">配置ID</param>
        /// <returns>是否删除成功</returns>
        Task<bool> DeleteExcelConfigAsync(int id);

        /// <summary>
        /// 获取Excel配置
        /// </summary>
        /// <param name="id">配置ID</param>
        /// <returns>Excel配置</returns>
        Task<ExcelConfig> GetExcelConfigAsync(string id);

        /// <summary>
        /// 获取所有Excel配置
        /// </summary>
        /// <returns>Excel配置列表</returns>
        Task<IEnumerable<ExcelConfig>> GetAllExcelConfigsAsync();

        /// <summary>
        /// 搜索Excel配置
        /// </summary>
        /// <param name="keyword">搜索关键词</param>
        /// <returns>匹配的配置列表</returns>
        Task<IEnumerable<ExcelConfig>> SearchExcelConfigsAsync(string keyword);

        #endregion

        #region 字段映射管理

        /// <summary>
        /// 创建字段映射
        /// </summary>
        /// <param name="mapping">字段映射</param>
        /// <returns>创建的映射</returns>
        Task<ExcelFieldMapping> CreateFieldMappingAsync(ExcelFieldMapping mapping);

        /// <summary>
        /// 更新字段映射
        /// </summary>
        /// <param name="mapping">字段映射</param>
        /// <returns>更新后的映射</returns>
        Task<ExcelFieldMapping> UpdateFieldMappingAsync(ExcelFieldMapping mapping);

        /// <summary>
        /// 删除字段映射
        /// </summary>
        /// <param name="id">映射ID</param>
        /// <returns>是否删除成功</returns>
        Task<bool> DeleteFieldMappingAsync(int id);

        /// <summary>
        /// 获取配置的所有字段映射
        /// </summary>
        /// <param name="configId">配置ID</param>
        /// <returns>字段映射列表</returns>
        Task<IEnumerable<ExcelFieldMapping>> GetFieldMappingsAsync(string configId);

        /// <summary>
        /// 批量保存字段映射
        /// </summary>
        /// <param name="configId">配置ID</param>
        /// <param name="mappings">字段映射列表</param>
        /// <returns>是否保存成功</returns>
        Task<bool> SaveFieldMappingsAsync(string configId, IEnumerable<ExcelFieldMapping> mappings);

        #endregion

        #region Excel文件处理

        /// <summary>
        /// 读取Excel文件信息
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>Excel文件信息</returns>
        Task<ExcelFileInfo> GetExcelFileInfoAsync(string filePath);

        /// <summary>
        /// 预览Excel数据
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="sheetName">Sheet名称</param>
        /// <param name="startRow">开始行</param>
        /// <param name="maxRows">最大行数</param>
        /// <returns>预览数据</returns>
        Task<ExcelPreviewData> PreviewExcelDataAsync(string filePath, string sheetName, int startRow = 1, int maxRows = 10);

        /// <summary>
        /// 验证Excel文件
        /// </summary>
        /// <param name="config">Excel配置</param>
        /// <returns>验证结果</returns>
        Task<ExcelValidationResult> ValidateExcelFileAsync(ExcelConfig config);

        #endregion

        #region 数据导入

        /// <summary>
        /// 执行Excel数据导入
        /// </summary>
        /// <param name="configId">配置ID</param>
        /// <param name="userId">执行用户ID</param>
        /// <returns>导入结果</returns>
        Task<ExcelImportResult> ImportExcelDataAsync(string configId, int? userId = null);

        /// <summary>
        /// 获取导入结果
        /// </summary>
        /// <param name="id">结果ID</param>
        /// <returns>导入结果</returns>
        Task<ExcelImportResult> GetImportResultAsync(int id);

        /// <summary>
        /// 获取配置的导入历史
        /// </summary>
        /// <param name="configId">配置ID</param>
        /// <param name="limit">限制数量</param>
        /// <returns>导入历史</returns>
        Task<IEnumerable<ExcelImportResult>> GetImportHistoryAsync(string configId, int limit = 10);

        /// <summary>
        /// 取消正在进行的导入
        /// </summary>
        /// <param name="resultId">结果ID</param>
        /// <returns>是否取消成功</returns>
        Task<bool> CancelImportAsync(int resultId);

        #endregion
    }

    /// <summary>
    /// Excel文件信息
    /// </summary>
    public class ExcelFileInfo
    {
        /// <summary>
        /// 文件路径
        /// </summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// 文件大小（字节）
        /// </summary>
        public long FileSize { get; set; }

        /// <summary>
        /// 最后修改时间
        /// </summary>
        public DateTime LastModified { get; set; }

        /// <summary>
        /// Sheet列表
        /// </summary>
        public List<string> SheetNames { get; set; } = new List<string>();

        /// <summary>
        /// 是否有效
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        public string? ErrorMessage { get; set; }
    }

    /// <summary>
    /// Excel预览数据
    /// </summary>
    public class ExcelPreviewData
    {
        /// <summary>
        /// 列标题
        /// </summary>
        public List<string> Headers { get; set; } = new List<string>();

        /// <summary>
        /// 预览行数据
        /// </summary>
        public List<List<string>> Rows { get; set; } = new List<List<string>>();

        /// <summary>
        /// 总行数
        /// </summary>
        public int TotalRows { get; set; }

        /// <summary>
        /// 总列数
        /// </summary>
        public int TotalColumns { get; set; }
    }

    /// <summary>
    /// Excel验证结果
    /// </summary>
    public class ExcelValidationResult
    {
        /// <summary>
        /// 是否有效
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// 验证消息列表
        /// </summary>
        public List<string> Messages { get; set; } = new List<string>();

        /// <summary>
        /// 错误数量
        /// </summary>
        public int ErrorCount { get; set; }

        /// <summary>
        /// 警告数量
        /// </summary>
        public int WarningCount { get; set; }
    }
} 