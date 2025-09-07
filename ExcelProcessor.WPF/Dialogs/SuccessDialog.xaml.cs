using System.ComponentModel;
using System.Windows;

namespace ExcelProcessor.WPF.Dialogs
{
    /// <summary>
    /// SuccessDialog.xaml 的交互逻辑
    /// </summary>
    public partial class SuccessDialog : Window, INotifyPropertyChanged
    {
        private string _title;
        private string _message;
        private string _buttonText;

        public event PropertyChangedEventHandler PropertyChanged;

        public new string Title
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

        public string ButtonText
        {
            get => _buttonText;
            set
            {
                _buttonText = value;
                OnPropertyChanged(nameof(ButtonText));
            }
        }

        public SuccessDialog(string title, string message, string buttonText = "确定")
        {
            InitializeComponent();
            DataContext = this;
            
            Title = title;
            Message = message;
            ButtonText = buttonText;
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
} 