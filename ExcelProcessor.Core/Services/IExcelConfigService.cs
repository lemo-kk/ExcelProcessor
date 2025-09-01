using System.Collections.Generic;
using System.Threading.Tasks;
using ExcelProcessor.Models;

namespace ExcelProcessor.Core.Services
{
    public interface IExcelConfigService
    {
        Task<bool> SaveConfigAsync(ExcelConfig config);
        Task<List<ExcelConfig>> GetAllConfigsAsync();
        Task<ExcelConfig> GetConfigByIdAsync(string id);
        Task<ExcelConfig> GetConfigByNameAsync(string configName);
        Task<bool> DeleteConfigAsync(string configName);
        Task<bool> UpdateConfigAsync(ExcelConfig config);
    }
} 