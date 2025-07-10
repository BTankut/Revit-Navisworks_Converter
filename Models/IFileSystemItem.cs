namespace RvtToNavisConverter.Models
{
    public interface IFileSystemItem
    {
        string Name { get; set; }
        string Path { get; set; }
        bool IsDirectory { get; }
    }
}
