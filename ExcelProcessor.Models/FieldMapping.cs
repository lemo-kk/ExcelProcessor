namespace ExcelProcessor.Models
{
    /// <summary>
    /// 字段映射模型
    /// </summary>
    public class FieldMapping
    {
        /// <summary>
        /// Excel原始列名（如A、B、C）
        /// </summary>
        public string ExcelOriginalColumn { get; set; } = "";

        /// <summary>
        /// Excel列名（如"客户名称"）
        /// </summary>
        public string ExcelColumn { get; set; } = "";

        /// <summary>
        /// 数据库字段名
        /// </summary>
        public string DatabaseField { get; set; } = "";

        /// <summary>
        /// 数据类型
        /// </summary>
        public string DataType { get; set; } = "VARCHAR(50)";

        /// <summary>
        /// 是否必填
        /// </summary>
        public bool IsRequired { get; set; } = false;
    }
} 