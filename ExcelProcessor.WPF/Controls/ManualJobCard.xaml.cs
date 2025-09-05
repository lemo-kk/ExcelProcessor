using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ExcelProcessor.Core.Services;
using ExcelProcessor.Models;

namespace ExcelProcessor.WPF.Controls
{
    /// <summary>
    /// 手动执行作业卡片控件
    /// </summary>
    public partial class ManualJobCard : UserControl
    {
        private JobConfig _jobConfig;
        private IJobService _jobService;

        public event EventHandler<JobConfig> JobExecuted;

        public ManualJobCard()
        {
            InitializeComponent();
        }

        /// <summary>
        /// 设置作业数据
        /// </summary>
        /// <param name="job">作业配置</param>
        /// <param name="jobService">作业服务</param>
        public void SetJobData(JobConfig job, IJobService jobService)
        {
            _jobConfig = job;
            _jobService = jobService;

            // 设置作业信息
            JobNameText.Text = job.Name;
            JobDescriptionText.Text = job.Description ?? "无描述";
            JobTypeText.Text = job.Type ?? "未知类型";
            JobPriorityText.Text = job.Priority.ToString();

            // 检查是否包含Excel导入步骤，并设置图标颜色
            SetIconColor();
        }

        /// <summary>
        /// 设置图标颜色
        /// </summary>
        private void SetIconColor()
        {
            try
            {
                if (_jobConfig?.Steps != null)
                {
                    // 检查是否包含Excel导入步骤
                    bool hasExcelImport = _jobConfig.Steps.Any(step => step.Type == StepType.ExcelImport);
                    
                    if (hasExcelImport)
                    {
                        // 如果包含Excel导入步骤，设置为绿色
                        JobIcon.Foreground = new SolidColorBrush(Color.FromRgb(76, 175, 80)); // #4CAF50 绿色
                    }
                    else
                    {
                        // 否则使用默认的紫色
                        JobIcon.Foreground = new SolidColorBrush(Color.FromRgb(156, 39, 176)); // #9C27B0 紫色
                    }
                }
            }
            catch (Exception ex)
            {
                // 如果出错，使用默认颜色
                System.Diagnostics.Debug.WriteLine($"设置图标颜色失败：{ex.Message}");
                JobIcon.Foreground = new SolidColorBrush(Color.FromRgb(156, 39, 176)); // 默认紫色
            }
        }

        /// <summary>
        /// 卡片点击事件
        /// </summary>
        private async void ManualJobCard_Click(object sender, MouseButtonEventArgs e)
        {
            try
            {
                if (_jobConfig == null || _jobService == null)
                {
                    Extensions.MessageBoxExtensions.Show("作业数据未正确加载", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // 所有作业都弹出执行对话框，统一交互方式
                var dialog = new Dialogs.JobExecutionDialog(_jobConfig, _jobService);
                dialog.Owner = Window.GetWindow(this);
                dialog.ShowDialog();
                
                // 触发作业执行事件
                JobExecuted?.Invoke(this, _jobConfig);
            }
            catch (Exception ex)
            {
                Extensions.MessageBoxExtensions.Show($"执行作业时发生错误：{ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
} 