using RvtToNavisConverter.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RvtToNavisConverter.Services
{
    public interface IRevitServerService
    {
        Task<List<IFileSystemItem>> GetDirectoryContentsAsync(string path, CancellationToken token);
    }

    public class RevitServerService : IRevitServerService
    {
        private readonly IFileStatusService _statusService;

        public RevitServerService(IFileStatusService statusService)
        {
            _statusService = statusService;
        }

        public async Task<List<IFileSystemItem>> GetDirectoryContentsAsync(string path, CancellationToken token)
        {
            var items = new List<IFileSystemItem>();

            try
            {
                if (!Directory.Exists(path))
                {
                    throw new DirectoryNotFoundException($"The path could not be found. Please check the path and your network connection. Path: {path}");
                }

                // Get all subdirectories
                foreach (var dir in Directory.GetDirectories(path))
                {
                    token.ThrowIfCancellationRequested();
                    var dirInfo = new DirectoryInfo(dir);

                    if (dirInfo.Name.EndsWith(".rvt"))
                    {
                        // This is a Revit project folder, treat as a file
                        items.Add(new FileItem
                        {
                            Name = dirInfo.Name,
                            Path = dir,
                            Status = _statusService.GetStatus(dir)
                        });
                    }
                    else
                    {
                        // This is a regular folder
                        items.Add(new FolderItem
                        {
                            Name = dirInfo.Name,
                            Path = dir,
                        });
                    }
                }
            }
            catch (Exception)
            {
                // Rethrow to be handled by the ViewModel
                throw;
            }

            return await Task.FromResult(items.OrderBy(i => !i.IsDirectory).ThenBy(i => i.Name).ToList());
        }
    }
}
