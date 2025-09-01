namespace ExcelProcessor.Models
{
    /// <summary>
    /// 作业步骤类型
    /// </summary>
    public enum JobStepType
    {
        /// <summary>
        /// Excel导入
        /// </summary>
        ExcelImport = 0,

        /// <summary>
        /// SQL执行
        /// </summary>
        SqlExecution = 1,

        /// <summary>
        /// 文件操作
        /// </summary>
        FileOperation = 2,

        /// <summary>
        /// 数据转换
        /// </summary>
        DataTransformation = 3,

        /// <summary>
        /// 数据验证
        /// </summary>
        DataValidation = 4,

        /// <summary>
        /// 邮件发送
        /// </summary>
        EmailSend = 5,

        /// <summary>
        /// HTTP请求
        /// </summary>
        HttpRequest = 6,

        /// <summary>
        /// 条件判断
        /// </summary>
        ConditionCheck = 7,

        /// <summary>
        /// 循环处理
        /// </summary>
        LoopProcess = 8,

        /// <summary>
        /// 变量设置
        /// </summary>
        VariableSet = 9,

        /// <summary>
        /// 日志记录
        /// </summary>
        LogRecord = 10,

        /// <summary>
        /// 等待
        /// </summary>
        Wait = 11,

        /// <summary>
        /// 自定义脚本
        /// </summary>
        CustomScript = 12,

        /// <summary>
        /// 子作业调用
        /// </summary>
        SubJobCall = 13
    }
} 