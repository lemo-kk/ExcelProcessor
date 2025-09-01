using System;
using System.ComponentModel.DataAnnotations;

namespace ExcelProcessor.Models
{
    /// <summary>
    /// 作业参数模型
    /// </summary>
    public class JobParameter
    {
        /// <summary>
        /// 参数ID
        /// </summary>
        public string Id { get; set; } = Guid.NewGuid().ToString();

        /// <summary>
        /// 参数名称
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 参数描述
        /// </summary>
        [MaxLength(500)]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 参数类型
        /// </summary>
        [Required]
        public ParameterType Type { get; set; }

        /// <summary>
        /// 默认值
        /// </summary>
        [MaxLength(1000)]
        public string DefaultValue { get; set; } = string.Empty;

        /// <summary>
        /// 是否必填
        /// </summary>
        public bool IsRequired { get; set; } = false;

        /// <summary>
        /// 是否加密
        /// </summary>
        public bool IsEncrypted { get; set; } = false;

        /// <summary>
        /// 验证规则（正则表达式）
        /// </summary>
        [MaxLength(500)]
        public string ValidationRule { get; set; } = string.Empty;

        /// <summary>
        /// 参数顺序
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// 备注
        /// </summary>
        [MaxLength(1000)]
        public string Remarks { get; set; } = string.Empty;

        /// <summary>
        /// 参数值（运行时设置）
        /// </summary>
        public string? Value { get; set; }
    }

    /// <summary>
    /// 参数类型枚举
    /// </summary>
    public enum ParameterType
    {
        /// <summary>
        /// 字符串
        /// </summary>
        String = 0,

        /// <summary>
        /// 整数
        /// </summary>
        Integer = 1,

        /// <summary>
        /// 小数
        /// </summary>
        Decimal = 2,

        /// <summary>
        /// 布尔值
        /// </summary>
        Boolean = 3,

        /// <summary>
        /// 日期时间
        /// </summary>
        DateTime = 4,

        /// <summary>
        /// 日期
        /// </summary>
        Date = 5,

        /// <summary>
        /// 时间
        /// </summary>
        Time = 6,

        /// <summary>
        /// 文件路径
        /// </summary>
        FilePath = 7,

        /// <summary>
        /// 目录路径
        /// </summary>
        DirectoryPath = 8,

        /// <summary>
        /// 邮箱地址
        /// </summary>
        Email = 9,

        /// <summary>
        /// URL地址
        /// </summary>
        Url = 10,

        /// <summary>
        /// JSON对象
        /// </summary>
        Json = 11,

        /// <summary>
        /// XML内容
        /// </summary>
        Xml = 12,

        /// <summary>
        /// 密码
        /// </summary>
        Password = 13,

        /// <summary>
        /// 选择列表
        /// </summary>
        Select = 14,

        /// <summary>
        /// 多选列表
        /// </summary>
        MultiSelect = 15
    }
} 