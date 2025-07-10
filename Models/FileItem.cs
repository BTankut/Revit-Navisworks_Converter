using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RvtToNavisConverter.Models
{
    public class FileItem : IFileSystemItem, INotifyPropertyChanged
    {
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public bool IsDirectory => false;

        // Additional properties specific to files
        private bool _isSelectedForDownload;
        public bool IsSelectedForDownload
        {
            get => _isSelectedForDownload;
            set
            {
                _isSelectedForDownload = value;
                OnPropertyChanged();
            }
        }

        private bool _isSelectedForConversion;
        public bool IsSelectedForConversion
        {
            get => _isSelectedForConversion;
            set
            {
                _isSelectedForConversion = value;
                OnPropertyChanged();
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
    }
}
