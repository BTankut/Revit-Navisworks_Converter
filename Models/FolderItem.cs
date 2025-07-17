using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RvtToNavisConverter.Models
{
    public class FolderItem : IFileSystemItem, INotifyPropertyChanged
    {
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public bool IsDirectory => true;

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
        
        // RevitVersion is not applicable for folders, but required by interface
        public string RevitVersion { get; set; } = string.Empty;

        private bool _isPartiallySelectedForDownload;
        public bool IsPartiallySelectedForDownload
        {
            get => _isPartiallySelectedForDownload;
            set
            {
                if (_isPartiallySelectedForDownload != value)
                {
                    _isPartiallySelectedForDownload = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _isPartiallySelectedForConversion;
        public bool IsPartiallySelectedForConversion
        {
            get => _isPartiallySelectedForConversion;
            set
            {
                if (_isPartiallySelectedForConversion != value)
                {
                    _isPartiallySelectedForConversion = value;
                    OnPropertyChanged();
                }
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
