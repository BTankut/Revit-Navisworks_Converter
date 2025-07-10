using RvtToNavisConverter.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RvtToNavisConverter.Services
{
    public interface ILocalFileService
    {
        Task<IEnumerable<IFileSystemItem>> GetDirectoryContentsAsync(string path, CancellationToken cancellationToken);
    }

    public class LocalFileService : ILocalFileService
    {
        public Task<IEnumerable<IFileSystemItem>> GetDirectoryContentsAsync(string path, CancellationToken cancellationToken)
        {
            return Task.Run<IEnumerable<IFileSystemItem>>(() =>
            {
                var items = new List<IFileSystemItem>();
                var directoryInfo = new DirectoryInfo(path);

                // Add subdirectories
                foreach (var dir in directoryInfo.GetDirectories())
                {
                    if (cancellationToken.IsCancellationRequested) cancellationToken.ThrowIfCancellationRequested();
                    items.Add(new FolderItem { Name = dir.Name, Path = dir.FullName });
                }

                // Add .rvt files
                foreach (var file in directoryInfo.GetFiles("*.rvt"))
                {
                    if (cancellationToken.IsCancellationRequested) cancellationToken.ThrowIfCancellationRequested();
                    items.Add(new FileItem
                    {
                        Name = file.Name,
                        Path = file.FullName,
                        IsLocal = true,
                        IsSelectedForDownload = false // Cannot be downloaded
                    });
                }
                return items.OrderBy(i => i.IsDirectory ? 0 : 1).ThenBy(i => i.Name);
            }, cancellationToken);
        }
    }
}
