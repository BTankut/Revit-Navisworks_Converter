using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RvtToNavisConverter.Models
{
    public class FileItem : IFileSystemItem, INotifyPropertyChanged
    {
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public bool IsDirectory => false;

        private bool? _isSelectedForDownload = false;
        public bool? IsSelectedForDownload
        {
            get => _isSelectedForDownload;
            set
            {
                if (_isSelectedForDownload != value)
                {
                    _isSelectedForDownload = value;
                    OnPropertyChanged();
                    OnSelectionChanged();
                }
            }
        }

        private bool? _isSelectedForConversion = false;
        public bool? IsSelectedForConversion
        {
            get => _isSelectedForConversion;
            set
            {
                if (_isSelectedForConversion != value)
                {
                    _isSelectedForConversion = value;
                    OnPropertyChanged();
                    OnSelectionChanged();
                }
            }
        }
        public bool IsLocal { get; set; }

        private string _status = "Pending";
        public string Status
        {
            get => _status;
            set
            {
                _status = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public event EventHandler? SelectionChanged;
        protected virtual void OnSelectionChanged()
        {
            SelectionChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
