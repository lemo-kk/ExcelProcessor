using System.ComponentModel;
using System.Windows;

namespace ExcelProcessor.WPF.Dialogs
{
    /// <summary>
    /// ProgressDialog.xaml 的交互逻辑
    /// </summary>
    public partial class ProgressDialog : Window, INotifyPropertyChanged
    {
        private string _title;
        private string _message;
        private bool _showCancelButton;

        public event PropertyChangedEventHandler PropertyChanged;

        public string Title
        {
            get => _title;
            set
            {
                _title = value;
                OnPropertyChanged(nameof(Title));
            }
        }

        public string Message
        {
            get => _message;
            set
            {
                _message = value;
                OnPropertyChanged(nameof(Message));
            }
        }

        public bool ShowCancelButton
        {
            get => _showCancelButton;
            set
            {
                _showCancelButton = value;
                OnPropertyChanged(nameof(ShowCancelButton));
            }
        }

        public ProgressDialog(string title, string message, bool showCancelButton = false)
        {
            InitializeComponent();
            DataContext = this;
            
            Title = title;
            Message = message;
            ShowCancelButton = showCancelButton;
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
} 