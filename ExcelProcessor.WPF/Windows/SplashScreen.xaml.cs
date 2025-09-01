using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using MaterialDesignThemes.Wpf;

namespace ExcelProcessor.WPF.Windows
{
    public partial class SplashScreen : Window
    {
        public SplashScreen()
        {
            InitializeComponent();
            SetupWindow();
        }

        private void SetupWindow()
        {
            Title = "Excel Processor - 正在启动";
            Width = 400;
            Height = 300;
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            WindowStyle = WindowStyle.None;
            ResizeMode = ResizeMode.NoResize;
            AllowsTransparency = true;
            Background = new SolidColorBrush(Color.FromRgb(42, 42, 42));
            Topmost = true;
        }

        public void UpdateProgress(string message)
        {
            if (ProgressText != null)
            {
                ProgressText.Text = message;
            }
        }
    }
} 