using ExcelProcessor.Models;
using ExcelProcessor.Data.Repositories;

namespace ExcelProcessor.Core.Repositories
{
    /// <summary>
    /// Excel字段映射仓储接口
    /// </summary>
    public interface IExcelFieldMappingRepository : IRepository<ExcelFieldMapping>
    {
        /// <summary>
        /// 根据Excel配置ID获取字段映射
        /// </summary>
        /// <param name="excelConfigId">Excel配置ID</param>
        /// <returns>字段映射列表</returns>
        Task<IEnumerable<ExcelFieldMapping>> GetByExcelConfigIdAsync(int excelConfigId);

        /// <summary>
        /// 根据Excel配置ID获取字段映射（字符串ID版本）
        /// </summary>
        /// <param name="excelConfigId">Excel配置ID</param>
        /// <returns>字段映射列表</returns>
        Task<IEnumerable<ExcelFieldMapping>> GetByExcelConfigIdAsync(string excelConfigId);

        /// <summary>
        /// 删除Excel配置的所有字段映射
        /// </summary>
        /// <param name="excelConfigId">Excel配置ID</param>
        /// <returns>是否成功</returns>
        Task<bool> DeleteByExcelConfigIdAsync(int excelConfigId);

        /// <summary>
        /// 删除Excel配置的所有字段映射（字符串ID版本）
        /// </summary>
        /// <param name="excelConfigId">Excel配置ID</param>
        /// <returns>是否成功</returns>
        Task<bool> DeleteByExcelConfigIdAsync(string excelConfigId);

        /// <summary>
        /// 批量保存字段映射
        /// </summary>
        /// <param name="mappings">字段映射列表</param>
        /// <returns>是否成功</returns>
        Task<bool> SaveMappingsAsync(IEnumerable<ExcelFieldMapping> mappings);
    }
} 