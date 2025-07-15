namespace RvtToNavisConverter.Models
{
    public interface IFileSystemItem
    {
        string Name { get; set; }
        string Path { get; set; }
        bool IsDirectory { get; }
        bool? IsSelectedForDownload { get; set; }
        bool? IsSelectedForConversion { get; set; }
        bool IsLocal { get; set; }
    }
}
