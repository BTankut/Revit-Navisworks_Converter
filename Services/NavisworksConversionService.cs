using RvtToNavisConverter.Helpers;
using RvtToNavisConverter.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RvtToNavisConverter.Services
{
    public class ConversionResult
    {
        public bool OverallSuccess { get; set; }
        public Dictionary<string, bool> FileResults { get; set; } = new Dictionary<string, bool>();
        public List<string> FailedFiles { get; set; } = new List<string>();
    }

    public interface INavisworksConversionService
    {
        Task<ConversionResult> ConvertFilesAsync(ConversionTask task, AppSettings settings);
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

        public async Task<ConversionResult> ConvertFilesAsync(ConversionTask task, AppSettings settings)
        {
            var nwdDirectory = settings.DefaultNwdPath;
            if (!Directory.Exists(nwdDirectory))
            {
                Directory.CreateDirectory(nwdDirectory);
            }
            var tempFileListPath = Path.Combine(Path.GetTempPath(), "revit_files_to_convert.txt");

            // The Navisworks utility expects the path to the .rvt container folder for server files, 
            // and the direct file path for local files.
            var filePaths = task.FilesToProcess.Select(f => f.IsLocal ? f.Path : Path.Combine(settings.DefaultDownloadPath, f.Name)).ToList();
            
            // Validate that all files exist before attempting conversion
            var missingFiles = new List<string>();
            foreach (var filePath in filePaths)
            {
                if (!File.Exists(filePath))
                {
                    missingFiles.Add(filePath);
                    FileLogger.LogError($"File not found for conversion: {filePath}");
                }
            }
            
            if (missingFiles.Any())
            {
                FileLogger.LogError($"Conversion aborted: {missingFiles.Count} files are missing");
                foreach (var missing in missingFiles)
                {
                    FileLogger.LogError($"  Missing: {missing}");
                }
                
                var failedResult = new ConversionResult { OverallSuccess = false };
                foreach (var file in task.FilesToProcess)
                {
                    failedResult.FileResults[file.Name] = false;
                    failedResult.FailedFiles.Add(file.Name);
                }
                return failedResult;
            }
            
            File.WriteAllLines(tempFileListPath, filePaths, Encoding.UTF8);

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
            
            // Add timeout parameter for large files
            arguments.Add("/timeout 1200"); // 20 minutes timeout per file - increased for complex parking models

            var argumentString = string.Join(" ", arguments);
            var scriptString = $@"& ""{settings.NavisworksToolPath}"" {argumentString}";
            FileLogger.Log($"Executing Navisworks script: {scriptString}");

            var result = await _psHelper.RunScriptStringAsync(scriptString);
            FileLogger.Log($"Navisworks script result: {result}");

            // Do not delete the temp file immediately, the external process might still need it.
            // File.Delete(tempFileListPath);

            // Parse conversion log to determine success/failure for each file
            var conversionResult = new ConversionResult();
            conversionResult.OverallSuccess = !result.Contains("An error occurred");
            
            // Read the conversion log file to check individual file status
            if (!string.IsNullOrEmpty(task.LogFilePath) && File.Exists(task.LogFilePath))
            {
                await Task.Delay(1000); // Give the log file time to be written
                var logContent = File.ReadAllText(task.LogFilePath);
                
                foreach (var file in task.FilesToProcess)
                {
                    var fileName = Path.GetFileNameWithoutExtension(file.Name);
                    
                    // Check if file was loaded successfully or had an error
                    if (logContent.Contains($"Loading {Path.Combine(settings.DefaultDownloadPath, file.Name)}"))
                    {
                        // Check if there's an error for this specific file
                        if (logContent.Contains($"Load of {file.Name} was canceled") || 
                            logContent.Contains($"An error occurred: Load of {file.Name} was canceled"))
                        {
                            conversionResult.FileResults[file.Name] = false;
                            conversionResult.FailedFiles.Add(file.Name);
                            FileLogger.LogError($"Conversion failed for {file.Name}: Load was canceled");
                        }
                        else
                        {
                            conversionResult.FileResults[file.Name] = true;
                        }
                    }
                    else
                    {
                        // If file is not mentioned in log at all, assume it failed
                        conversionResult.FileResults[file.Name] = false;
                        conversionResult.FailedFiles.Add(file.Name);
                        FileLogger.LogError($"Conversion failed for {file.Name}: Not found in conversion log");
                    }
                }
                
                // Update overall success based on individual files
                conversionResult.OverallSuccess = conversionResult.FailedFiles.Count == 0;
                
                // If there are failed files and ProcessIndividually is enabled, retry them one by one
                if (task.ProcessIndividually && conversionResult.FailedFiles.Any())
                {
                    FileLogger.Log($"Retrying {conversionResult.FailedFiles.Count} failed files individually...");
                    FileLogger.Log("Note: Failed files appear to be complex models (parking) that may require special handling");
                    
                    var failedFiles = task.FilesToProcess.Where(f => conversionResult.FailedFiles.Contains(f.Name)).ToList();
                    foreach (var file in failedFiles)
                    {
                        FileLogger.Log($"Retrying individual conversion for {file.Name}");
                        
                        // Create a temporary file list with just this one file
                        var singleFilePath = Path.Combine(Path.GetTempPath(), $"single_file_{Guid.NewGuid()}.txt");
                        var singleFileFullPath = file.IsLocal ? file.Path : Path.Combine(settings.DefaultDownloadPath, file.Name);
                        File.WriteAllLines(singleFilePath, new[] { singleFileFullPath }, Encoding.UTF8);
                        
                        // Create individual NWC file ONLY (no NWD)
                        // By not specifying /of parameter, only NWC will be created
                        var individualArgs = new List<string>
                        {
                            $@"/i ""{singleFilePath}""",
                            "/timeout 1200", // 20 minutes timeout for individual files - parking models need more time
                            "/osd" // Open and save document - forces file to be fully loaded
                        };
                        
                        var individualScript = $@"& ""{settings.NavisworksToolPath}"" {string.Join(" ", individualArgs)}";
                        FileLogger.Log($"Executing individual conversion: {individualScript}");
                        
                        var individualResult = await _psHelper.RunScriptStringAsync(individualScript);
                        
                        var nwcPath = Path.ChangeExtension(singleFileFullPath, ".nwc");
                        if (!individualResult.Contains("An error occurred") && File.Exists(nwcPath))
                        {
                            conversionResult.FileResults[file.Name] = true;
                            conversionResult.FailedFiles.Remove(file.Name);
                            FileLogger.Log($"Individual conversion successful for {file.Name}");
                        }
                        else
                        {
                            FileLogger.LogError($"Individual conversion also failed for {file.Name}");
                            
                            // For parking models that continue to fail, try one more time with minimal settings
                            if (file.Name.Contains("_Parking"))
                            {
                                FileLogger.Log($"Attempting minimal conversion for parking model: {file.Name}");
                                
                                var minimalArgs = new List<string>
                                {
                                    $@"/i ""{singleFilePath}""",
                                    "/timeout 1800", // 30 minutes for very complex models
                                    "/version 2021" // Force Revit 2021 version compatibility
                                };
                                
                                var minimalScript = $@"& ""{settings.NavisworksToolPath}"" {string.Join(" ", minimalArgs)}";
                                FileLogger.Log($"Executing minimal conversion: {minimalScript}");
                                
                                var minimalResult = await _psHelper.RunScriptStringAsync(minimalScript);
                                
                                if (!minimalResult.Contains("An error occurred") && File.Exists(nwcPath))
                                {
                                    conversionResult.FileResults[file.Name] = true;
                                    conversionResult.FailedFiles.Remove(file.Name);
                                    FileLogger.Log($"Minimal conversion successful for {file.Name}");
                                }
                                else
                                {
                                    FileLogger.LogError($"All conversion attempts failed for {file.Name}. This file may require manual processing.");
                                }
                            }
                        }
                        
                        // Clean up temp file
                        try { File.Delete(singleFilePath); } catch { }
                    }
                    
                    // Update overall success
                    conversionResult.OverallSuccess = conversionResult.FailedFiles.Count == 0;
                    
                    // After individual retries, if we need to create a consolidated NWD with only successful files
                    // we would need to run FileToolsTaskRunner again with only the successful files
                    // But this is complex because the tool creates both NWC and NWD together
                    // For now, the initial batch run already created the NWD with whatever files succeeded
                    
                    if (conversionResult.FailedFiles.Any())
                    {
                        FileLogger.LogWarning($"Initial NWD file was created but {conversionResult.FailedFiles.Count} files failed to convert");
                        FileLogger.LogWarning($"Failed files: {string.Join(", ", conversionResult.FailedFiles)}");
                    }
                }
            }
            
            return conversionResult;
        }
    }
}
