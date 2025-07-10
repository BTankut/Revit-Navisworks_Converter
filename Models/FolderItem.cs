namespace RvtToNavisConverter.Models
{
    public class FolderItem : IFileSystemItem
    {
        public string Name { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public bool IsDirectory => true;
        public bool IsSelectedForDownload { get; set; }
        public bool IsSelectedForConversion { get; set; }
        public bool IsLocal { get; set; }
    }
}
