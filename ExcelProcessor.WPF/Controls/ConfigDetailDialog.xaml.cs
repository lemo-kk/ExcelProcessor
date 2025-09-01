using System;
using System.Windows;
using System.Windows.Controls;

namespace ExcelProcessor.WPF.Controls
{
    public partial class ConfigDetailDialog : UserControl
    {
        public event EventHandler SaveClicked;
        public event EventHandler TestClicked;
        public event EventHandler CancelClicked;
        public event EventHandler CloseClicked;

        public ConfigDetailDialog()
        {
            InitializeComponent();
        }

        public string Title
        {
            get => TitleTextBlock.Text;
            set => TitleTextBlock.Text = value;
        }

        public string Subtitle
        {
            get => SubtitleTextBlock.Text;
            set => SubtitleTextBlock.Text = value;
        }

        public new UIElement Content
        {
            get => ContentPresenter.Content as UIElement;
            set => ContentPresenter.Content = value;
        }

        public bool ShowTestButton
        {
            get => TestButton.Visibility == Visibility.Visible;
            set => TestButton.Visibility = value ? Visibility.Visible : Visibility.Collapsed;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            CloseClicked?.Invoke(this, EventArgs.Empty);
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Console.WriteLine("ConfigDetailDialog: 保存按钮被点击");
            SaveClicked?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ConfigDetailDialog.SaveButton_Click 出错: {ex.Message}");
            }
        }

        private void TestButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Console.WriteLine("ConfigDetailDialog: 测试按钮被点击");
            TestClicked?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ConfigDetailDialog.TestButton_Click 出错: {ex.Message}");
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Console.WriteLine("ConfigDetailDialog: 取消按钮被点击");
            CancelClicked?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ConfigDetailDialog.CancelButton_Click 出错: {ex.Message}");
            }
        }
    }
} 