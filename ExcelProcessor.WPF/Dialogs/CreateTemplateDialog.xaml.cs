using System;
using System.Windows;

namespace ExcelProcessor.WPF.Dialogs
{
    public partial class CreateTemplateDialog : Window
    {
        public string TemplateName { get; private set; }
        public string TemplateDescription { get; private set; }
        public string ContentType { get; private set; }
        public bool IncludePasswords { get; private set; }
        public bool CompressTemplate { get; private set; }
        public bool AddTimestamp { get; private set; }

        public CreateTemplateDialog()
        {
            InitializeComponent();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void CreateButton_Click(object sender, RoutedEventArgs e)
        {
            // 验证输入
            if (string.IsNullOrWhiteSpace(TemplateNameTextBox.Text))
            {
                MessageBox.Show("请输入模板名称。", "验证错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                TemplateNameTextBox.Focus();
                return;
            }

            // 获取选择的内容类型
            ContentType = GetSelectedContentType();
            if (string.IsNullOrEmpty(ContentType))
            {
                MessageBox.Show("请选择模板包含的内容类型。", "验证错误", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // 设置属性值
            TemplateName = TemplateNameTextBox.Text.Trim();
            TemplateDescription = TemplateDescriptionTextBox.Text.Trim();
            IncludePasswords = IncludePasswordsCheckBox.IsChecked == true;
            CompressTemplate = CompressTemplateCheckBox.IsChecked == true;
            AddTimestamp = AddTimestampCheckBox.IsChecked == true;

            DialogResult = true;
            Close();
        }

        private string GetSelectedContentType()
        {
            if (DataSourceOnlyRadio.IsChecked == true)
                return "数据源";
            else if (JobConfigOnlyRadio.IsChecked == true)
                return "作业配置";
            else if (ImportConfigOnlyRadio.IsChecked == true)
                return "导入配置";
            else if (FullConfigRadio.IsChecked == true)
                return "全部配置";
            
            return string.Empty;
        }

        // 窗口拖拽功能
        private void Window_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (e.ButtonState == System.Windows.Input.MouseButtonState.Pressed)
            {
                DragMove();
            }
        }
    }
} 