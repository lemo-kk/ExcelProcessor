using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ExcelProcessor.Core.Services;
using ExcelProcessor.Models;
using Microsoft.Extensions.Logging;

namespace ExcelProcessor.Data.Services
{
    /// <summary>
    /// 文件路径管理服务实现
    /// </summary>
    public class FilePathService : IFilePathService
    {
        private readonly ILogger<FilePathService> _logger;
        private readonly ISystemConfigService _systemConfigService;

        public FilePathService(ILogger<FilePathService> logger, ISystemConfigService systemConfigService)
        {
            _logger = logger;
            _systemConfigService = systemConfigService;
        }

        /// <summary>
        /// 获取应用程序根目录
        /// </summary>
        public string GetApplicationRootPath()
        {
            return AppDomain.CurrentDomain.BaseDirectory;
        }

        /// <summary>
        /// 获取输入文件目录
        /// </summary>
        public string GetInputPath()
        {
            var defaultPath = Path.Combine(GetApplicationRootPath(), "data", "input");
            var configPath = _systemConfigService.GetConfigValueAsync(SystemConfigKeys.DefaultInputPath).Result;
            
            if (!string.IsNullOrEmpty(configPath))
            {
                // 如果是相对路径，转换为绝对路径
                if (!Path.IsPathRooted(configPath))
                {
                    configPath = Path.Combine(GetApplicationRootPath(), configPath);
                }
                return configPath;
            }
            
            return defaultPath;
        }

        /// <summary>
        /// 获取输出文件目录
        /// </summary>
        public string GetOutputPath()
        {
            var defaultPath = Path.Combine(GetApplicationRootPath(), "data", "output");
            var configPath = _systemConfigService.GetConfigValueAsync(SystemConfigKeys.DefaultOutputPath).Result;
            
            if (!string.IsNullOrEmpty(configPath))
            {
                // 如果是相对路径，转换为绝对路径
                if (!Path.IsPathRooted(configPath))
                {
                    configPath = Path.Combine(GetApplicationRootPath(), configPath);
                }
                return configPath;
            }
            
            return defaultPath;
        }

        /// <summary>
        /// 获取模板文件目录
        /// </summary>
        public string GetTemplatePath()
        {
            var defaultPath = Path.Combine(GetApplicationRootPath(), "data", "templates");
            var configPath = _systemConfigService.GetConfigValueAsync(SystemConfigKeys.ExcelTemplatePath).Result;
            
            if (!string.IsNullOrEmpty(configPath))
            {
                // 如果是相对路径，转换为绝对路径
                if (!Path.IsPathRooted(configPath))
                {
                    configPath = Path.Combine(GetApplicationRootPath(), configPath);
                }
                return configPath;
            }
            
            return defaultPath;
        }

        /// <summary>
        /// 获取临时文件目录
        /// </summary>
        public string GetTempPath()
        {
            var defaultPath = Path.Combine(GetApplicationRootPath(), "data", "temp");
            var configPath = _systemConfigService.GetConfigValueAsync(SystemConfigKeys.TempFilePath).Result;
            
            if (!string.IsNullOrEmpty(configPath))
            {
                // 如果是相对路径，转换为绝对路径
                if (!Path.IsPathRooted(configPath))
                {
                    configPath = Path.Combine(GetApplicationRootPath(), configPath);
                }
                return configPath;
            }
            
            return defaultPath;
        }

        /// <summary>
        /// 获取日志目录
        /// </summary>
        public string GetLogPath()
        {
            return Path.Combine(GetApplicationRootPath(), "logs");
        }

        /// <summary>
        /// 获取配置目录
        /// </summary>
        public string GetConfigPath()
        {
            return Path.Combine(GetApplicationRootPath(), "config");
        }

        /// <summary>
        /// 将绝对路径转换为相对路径
        /// </summary>
        public string ToRelativePath(string absolutePath)
        {
            if (string.IsNullOrEmpty(absolutePath))
                return string.Empty;

            try
            {
                var rootPath = GetApplicationRootPath();
                if (absolutePath.StartsWith(rootPath, StringComparison.OrdinalIgnoreCase))
                {
                    var relativePath = absolutePath.Substring(rootPath.Length);
                    return relativePath.TrimStart('\\', '/');
                }
                return absolutePath;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "转换相对路径失败: {Path}", absolutePath);
                return absolutePath;
            }
        }

        /// <summary>
        /// 将相对路径转换为绝对路径
        /// </summary>
        public string ToAbsolutePath(string relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
                return string.Empty;

            try
            {
                if (Path.IsPathRooted(relativePath))
                    return relativePath;

                return Path.Combine(GetApplicationRootPath(), relativePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "转换绝对路径失败: {Path}", relativePath);
                return relativePath;
            }
        }

        /// <summary>
        /// 确保目录存在
        /// </summary>
        public void EnsureDirectoryExists(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                    _logger.LogInformation("创建目录: {Path}", path);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "创建目录失败: {Path}", path);
                throw;
            }
        }

        /// <summary>
        /// 获取目录下的所有Excel文件
        /// </summary>
        public List<string> GetExcelFilesInDirectory(string directoryPath)
        {
            var excelFiles = new List<string>();
            
            try
            {
                if (!Directory.Exists(directoryPath))
                    return excelFiles;

                var extensions = new[] { "*.xlsx", "*.xls", "*.csv" };
                foreach (var extension in extensions)
                {
                    var files = Directory.GetFiles(directoryPath, extension, SearchOption.TopDirectoryOnly);
                    excelFiles.AddRange(files);
                }

                return excelFiles.OrderBy(f => f).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取Excel文件列表失败: {Path}", directoryPath);
                return excelFiles;
            }
        }

        /// <summary>
        /// 复制文件到输入目录
        /// </summary>
        public async Task<string> CopyFileToInputDirectoryAsync(string sourceFilePath)
        {
            try
            {
                if (!File.Exists(sourceFilePath))
                    throw new FileNotFoundException($"源文件不存在: {sourceFilePath}");

                var inputPath = GetInputPath();
                EnsureDirectoryExists(inputPath);

                var fileName = Path.GetFileName(sourceFilePath);
                var uniqueFileName = GenerateUniqueFileName(fileName);
                var targetPath = Path.Combine(inputPath, uniqueFileName);

                await Task.Run(() => File.Copy(sourceFilePath, targetPath, true));
                
                _logger.LogInformation("文件已复制到输入目录: {Source} -> {Target}", sourceFilePath, targetPath);
                
                return ToRelativePath(targetPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "复制文件到输入目录失败: {Path}", sourceFilePath);
                throw;
            }
        }

        /// <summary>
        /// 生成唯一的文件名
        /// </summary>
        public string GenerateUniqueFileName(string originalFileName)
        {
            try
            {
                var fileNameWithoutExt = Path.GetFileNameWithoutExtension(originalFileName);
                var extension = Path.GetExtension(originalFileName);
                var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                var random = new Random().Next(1000, 9999);
                
                return $"{fileNameWithoutExt}_{timestamp}_{random}{extension}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "生成唯一文件名失败: {FileName}", originalFileName);
                return $"{DateTime.Now:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}{Path.GetExtension(originalFileName)}";
            }
        }

        /// <summary>
        /// 清理临时文件
        /// </summary>
        public async Task CleanupTempFilesAsync(int retentionDays = 1)
        {
            try
            {
                var tempPath = GetTempPath();
                if (!Directory.Exists(tempPath))
                    return;

                var cutoffDate = DateTime.Now.AddDays(-retentionDays);
                var filesToDelete = new List<string>();

                foreach (var file in Directory.GetFiles(tempPath, "*", SearchOption.AllDirectories))
                {
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.CreationTime < cutoffDate)
                    {
                        filesToDelete.Add(file);
                    }
                }

                foreach (var file in filesToDelete)
                {
                    try
                    {
                        File.Delete(file);
                        _logger.LogDebug("删除临时文件: {File}", file);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "删除临时文件失败: {File}", file);
                    }
                }

                await Task.CompletedTask;
                _logger.LogInformation("清理临时文件完成，删除了 {Count} 个文件", filesToDelete.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "清理临时文件失败");
            }
        }

        /// <summary>
        /// 验证文件路径是否有效
        /// </summary>
        public bool IsValidFilePath(string filePath)
        {
            try
            {
                if (string.IsNullOrEmpty(filePath))
                    return false;

                // 检查路径格式
                var fullPath = Path.GetFullPath(filePath);
                
                // 检查文件是否存在
                return File.Exists(fullPath);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 获取文件大小（MB）
        /// </summary>
        public double GetFileSizeInMB(string filePath)
        {
            try
            {
                if (!File.Exists(filePath))
                    return 0;

                var fileInfo = new FileInfo(filePath);
                return Math.Round(fileInfo.Length / (1024.0 * 1024.0), 2);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取文件大小失败: {Path}", filePath);
                return 0;
            }
        }
    }
} 