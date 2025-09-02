using System.Windows;

namespace ExcelProcessor.WPF.Dialogs
{
    /// <summary>
    /// ErrorDialog.xaml 的交互逻辑
    /// </summary>
    public partial class ErrorDialog : Window
    {
        public string Title { get; set; }
        public string Message { get; set; }
        public string ButtonText { get; set; }

        public ErrorDialog(string title, string message, string buttonText = "确定")
        {
            InitializeComponent();
            Title = title;
            Message = message;
            ButtonText = buttonText;
            DataContext = this;
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
} 