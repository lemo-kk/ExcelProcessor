using System;
using System.Collections.Generic;
using System.Windows.Controls;

namespace ExcelProcessor.WPF.Pages
{
    public partial class HomePage : Page
    {
        public HomePage()
        {
            InitializeComponent();
            LoadRecentTasks();
        }

        private void LoadRecentTasks()
        {
            // 模拟最近执行任务数据
            var recentTasks = new List<RecentTask>
            {
                new RecentTask 
                { 
                    JobName = "销售数据导入", 
                    ExecuteTime = DateTime.Now.AddHours(-2).ToString("yyyy-MM-dd HH:mm:ss"),
                    Status = "成功"
                },
                new RecentTask 
                { 
                    JobName = "客户信息处理", 
                    ExecuteTime = DateTime.Now.AddHours(-4).ToString("yyyy-MM-dd HH:mm:ss"),
                    Status = "成功"
                },
                new RecentTask 
                { 
                    JobName = "财务报表生成", 
                    ExecuteTime = DateTime.Now.AddHours(-6).ToString("yyyy-MM-dd HH:mm:ss"),
                    Status = "失败"
                },
                new RecentTask 
                { 
                    JobName = "库存数据同步", 
                    ExecuteTime = DateTime.Now.AddHours(-8).ToString("yyyy-MM-dd HH:mm:ss"),
                    Status = "成功"
                }
            };

            RecentTasksDataGrid.ItemsSource = recentTasks;
        }
    }

    public class RecentTask
    {
        public string JobName { get; set; }
        public string ExecuteTime { get; set; }
        public string Status { get; set; }
    }
} 