using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace ExcelProcessor.WPF.Models
{
    public class PermissionTreeNode : INotifyPropertyChanged
    {
        private bool _isExpanded;
        private bool _isSelected;

        public string Name { get; set; }
        public string Description { get; set; }
        public string PermissionCode { get; set; }
        public string Icon { get; set; }
        public bool IsExpanded
        {
            get => _isExpanded;
            set
            {
                _isExpanded = value;
                OnPropertyChanged(nameof(IsExpanded));
            }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                OnPropertyChanged(nameof(IsSelected));
            }
        }

        public ObservableCollection<PermissionTreeNode> Children { get; set; } = new ObservableCollection<PermissionTreeNode>();

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}