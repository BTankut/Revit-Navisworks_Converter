using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace RvtToNavisConverter.Models
{
    public class FolderItem : IFileSystemItem, INotifyPropertyChanged
    {
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public bool IsDirectory => true;

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

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
