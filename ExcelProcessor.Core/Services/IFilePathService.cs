using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ExcelProcessor.Core.Services
{
    /// <summary>
    /// 文件路径管理服务接口
    /// </summary>
    public interface IFilePathService
    {
        /// <summary>
        /// 获取应用程序根目录
        /// </summary>
        string GetApplicationRootPath();

        /// <summary>
        /// 获取输入文件目录
        /// </summary>
        string GetInputPath();

        /// <summary>
        /// 获取输出文件目录
        /// </summary>
        string GetOutputPath();

        /// <summary>
        /// 获取模板文件目录
        /// </summary>
        string GetTemplatePath();

        /// <summary>
        /// 获取临时文件目录
        /// </summary>
        string GetTempPath();

        /// <summary>
        /// 获取日志目录
        /// </summary>
        string GetLogPath();

        /// <summary>
        /// 获取配置目录
        /// </summary>
        string GetConfigPath();

        /// <summary>
        /// 将绝对路径转换为相对路径
        /// </summary>
        string ToRelativePath(string absolutePath);

        /// <summary>
        /// 将相对路径转换为绝对路径
        /// </summary>
        string ToAbsolutePath(string relativePath);

        /// <summary>
        /// 确保目录存在
        /// </summary>
        void EnsureDirectoryExists(string path);

        /// <summary>
        /// 获取目录下的所有Excel文件
        /// </summary>
        List<string> GetExcelFilesInDirectory(string directoryPath);

        /// <summary>
        /// 复制文件到输入目录
        /// </summary>
        Task<string> CopyFileToInputDirectoryAsync(string sourceFilePath);

        /// <summary>
        /// 生成唯一的文件名
        /// </summary>
        string GenerateUniqueFileName(string originalFileName);

        /// <summary>
        /// 清理临时文件
        /// </summary>
        Task CleanupTempFilesAsync(int retentionDays = 1);

        /// <summary>
        /// 验证文件路径是否有效
        /// </summary>
        bool IsValidFilePath(string filePath);

        /// <summary>
        /// 获取文件大小（MB）
        /// </summary>
        double GetFileSizeInMB(string filePath);
    }
} 