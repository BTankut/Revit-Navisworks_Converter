using RvtToNavisConverter.Helpers;
using RvtToNavisConverter.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RvtToNavisConverter.Services
{
    public interface INavisworksConversionService
    {
        Task<bool> ConvertFilesAsync(ConversionTask task, AppSettings settings);
    }

    public class NavisworksConversionService : INavisworksConversionService
    {
        private readonly PowerShellHelper _psHelper;
        private readonly FileHelper _fileHelper;

        public NavisworksConversionService(PowerShellHelper psHelper, FileHelper fileHelper)
        {
            _psHelper = psHelper;
            _fileHelper = fileHelper;
        }

        public async Task<bool> ConvertFilesAsync(ConversionTask task, AppSettings settings)
        {
            var nwdDirectory = settings.DefaultNwdPath;
            if (!Directory.Exists(nwdDirectory))
            {
                Directory.CreateDirectory(nwdDirectory);
            }
            var tempFileListPath = Path.Combine(Path.GetTempPath(), "revit_files_to_convert.txt");

            // The Navisworks utility expects the path to the .rvt container folder for server files, 
            // and the direct file path for local files.
            var filePaths = task.FilesToProcess.Select(f => f.IsLocal ? f.Path : Path.Combine(settings.DefaultDownloadPath, f.Name));
            await File.WriteAllLinesAsync(tempFileListPath, filePaths, Encoding.UTF8);

            var arguments = new List<string>
            {
                $@"/i ""{tempFileListPath}"""
            };

            if (!string.IsNullOrEmpty(task.OutputNwdFile))
            {
                arguments.Add($@"/of ""{Path.Combine(settings.DefaultNwdPath, task.OutputNwdFile)}""");
            }
            else
            {
                // NWC files will be created in the same directory as the RVT files
                // No need to specify output directory
            }

            // Overwrite is the default behavior, and /inc is not needed for this implementation.
            // The logic for these parameters has been removed.

            if (!string.IsNullOrEmpty(task.LogFilePath))
            {
                arguments.Add($@"/log ""{task.LogFilePath}""");
            }

            if (!string.IsNullOrEmpty(task.OutputVersion))
            {
                arguments.Add($"/version {task.OutputVersion}");
            }

            if (task.OpenAfterConversion)
            {
                arguments.Add("/view");
            }

            var argumentString = string.Join(" ", arguments);
            var scriptString = $@"& ""{settings.NavisworksToolPath}"" {argumentString}";
            FileLogger.Log($"Executing Navisworks script: {scriptString}");

            var result = await _psHelper.RunScriptStringAsync(scriptString);
            FileLogger.Log($"Navisworks script result: {result}");

            // Do not delete the temp file immediately, the external process might still need it.
            // File.Delete(tempFileListPath);

            // Success is determined by the absence of the "An error occurred" message.
            return !result.Contains("An error occurred");
        }
    }
}
