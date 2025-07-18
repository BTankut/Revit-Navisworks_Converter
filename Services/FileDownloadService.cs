using RvtToNavisConverter.Helpers;
using RvtToNavisConverter.Models;
using System.IO;
using System.Threading.Tasks;

namespace RvtToNavisConverter.Services
{
    public interface IFileDownloadService
    {
        Task<bool> DownloadFileAsync(FileItem file, AppSettings settings);
    }

    public class FileDownloadService : IFileDownloadService
    {
        private readonly PowerShellHelper _psHelper;
        private readonly FileHelper _fileHelper;

        public FileDownloadService(PowerShellHelper psHelper, FileHelper fileHelper)
        {
            _psHelper = psHelper;
            _fileHelper = fileHelper;
        }

        public async Task<bool> DownloadFileAsync(FileItem file, AppSettings settings)
        {
            _fileHelper.EnsureDirectoryExists(settings.DefaultDownloadPath);

            var destinationPath = Path.Combine(settings.DefaultDownloadPath, file.Name);
            
            // The path for createLocalRVT should be relative to the Revit Server's project root.
            // We need to strip the server and root path from the Path.
            var serverRoot = $@"\\{settings.RevitServerIp}\Revit Server 2021\Projects"; // This might need to be configurable
            var relativePath = file.Path.Replace(serverRoot, "").TrimStart('\\');

            // Revert to the simpler call operator '&' which allows for better output redirection.
            var scriptString = $@"& ""{settings.RevitServerToolPath}"" createLocalRVT '{relativePath}' -s ""{settings.RevitServerIp}"" -a ""{settings.RevitServerAccelerator}"" -d ""{destinationPath}"" -o";
            
            FileLogger.Log($"Executing PowerShell script: {scriptString}");

            var result = await _psHelper.RunScriptStringAsync(scriptString);
            
            FileLogger.Log($"PowerShell script result: {result}");

            // Success is determined by whether the file was created.
            var fileExists = File.Exists(destinationPath);
            
            if (!fileExists)
            {
                FileLogger.LogError($"Download verification failed - file not found at: {destinationPath}");
                if (!string.IsNullOrWhiteSpace(result))
                {
                    FileLogger.LogError($"PowerShell output: {result}");
                }
            }
            else
            {
                FileLogger.Log($"Download verified - file exists at: {destinationPath}");
            }
            
            return fileExists;
        }
    }
}
