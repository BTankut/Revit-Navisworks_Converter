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
            var tempFileListPath = Path.Combine(Path.GetTempPath(), $"revit_files_to_convert_{Guid.NewGuid()}.txt");

            // The Navisworks utility expects the path to the .rvt container folder for server files, 
            // and the direct file path for local files.
            var filePaths = task.FilesToProcess.Select(f => f.IsLocal ? f.Path : Path.Combine(settings.DefaultDownloadPath, f.Name)).ToList();
            
            // Validate that all files exist before attempting conversion
            var missingFiles = new List<string>();
            var lockedFiles = new List<string>();
            
            foreach (var filePath in filePaths)
            {
                if (!File.Exists(filePath))
                {
                    missingFiles.Add(filePath);
                    FileLogger.LogError($"File not found for conversion: {filePath}");
                }
                else if (FileLockChecker.IsFileLocked(filePath))
                {
                    FileLogger.Log($"File is locked, waiting for it to be available: {filePath}");
                    if (!FileLockChecker.WaitForFile(filePath, 30))
                    {
                        lockedFiles.Add(filePath);
                        FileLogger.LogError($"File remains locked after 30 seconds: {filePath}");
                    }
                }
            }
            
            if (missingFiles.Any() || lockedFiles.Any())
            {
                if (missingFiles.Any())
                {
                    FileLogger.LogError($"Conversion aborted: {missingFiles.Count} files are missing");
                    foreach (var missing in missingFiles)
                    {
                        FileLogger.LogError($"  Missing: {missing}");
                    }
                }
                
                if (lockedFiles.Any())
                {
                    FileLogger.LogError($"Conversion aborted: {lockedFiles.Count} files are locked");
                    foreach (var locked in lockedFiles)
                    {
                        FileLogger.LogError($"  Locked: {locked}");
                    }
                }
                
                var failedResult = new ConversionResult { OverallSuccess = false };
                foreach (var file in task.FilesToProcess)
                {
                    var filePath = file.IsLocal ? file.Path : Path.Combine(settings.DefaultDownloadPath, file.Name);
                    if (missingFiles.Contains(filePath) || lockedFiles.Contains(filePath))
                    {
                        failedResult.FileResults[file.Name] = false;
                        failedResult.FailedFiles.Add(file.Name);
                    }
                }
                return failedResult;
            }
            
            var conversionResult = new ConversionResult();
            
            try
            {
                File.WriteAllLines(tempFileListPath, filePaths, Encoding.UTF8);
                // Small delay to ensure file is written and accessible
                await Task.Delay(100);

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

                // Delete temp file after a delay to ensure FileToolsTaskRunner is done with it
                await Task.Delay(5000);
                try { File.Delete(tempFileListPath); } catch { /* Ignore cleanup errors */ }

            // Parse conversion result
            conversionResult.OverallSuccess = !result.Contains("Usage:") && !result.Contains("An error occurred");
            
            // Check conversion log for errors
            await Task.Delay(2000); // Give the conversion process time to complete
            
            if (!string.IsNullOrEmpty(task.LogFilePath) && File.Exists(task.LogFilePath))
            {
                try
                {
                    var logContent = File.ReadAllText(task.LogFilePath);
                    FileLogger.Log($"Conversion log content preview: {logContent.Substring(0, Math.Min(500, logContent.Length))}");
                    
                    // Check for any errors in the log
                    if (logContent.Contains("An error occurred:") || logContent.Contains("was canceled"))
                    {
                        conversionResult.OverallSuccess = false;
                        FileLogger.LogError("Errors found in conversion log");
                        
                        // Parse which files failed
                        var lines = logContent.Split('\n');
                        foreach (var line in lines)
                        {
                            if (line.Contains("was canceled") || line.Contains("An error occurred:"))
                            {
                                foreach (var file in task.FilesToProcess)
                                {
                                    if (line.Contains(file.Name) || line.Contains(Path.GetFileNameWithoutExtension(file.Name)))
                                    {
                                        conversionResult.FileResults[file.Name] = false;
                                        if (!conversionResult.FailedFiles.Contains(file.Name))
                                        {
                                            conversionResult.FailedFiles.Add(file.Name);
                                        }
                                        FileLogger.LogError($"File failed during conversion: {file.Name} - {line.Trim()}");
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    FileLogger.LogError($"Error reading conversion log: {ex.Message}");
                }
            }
                
                foreach (var file in task.FilesToProcess)
                {
                    // Skip if already marked as failed from log parsing
                    if (conversionResult.FileResults.ContainsKey(file.Name) && !conversionResult.FileResults[file.Name])
                    {
                        continue;
                    }
                    
                    var filePath = file.IsLocal ? file.Path : Path.Combine(settings.DefaultDownloadPath, file.Name);
                    var nwcPath = Path.ChangeExtension(filePath, ".nwc");
                    var nwdPath = Path.ChangeExtension(filePath, ".nwd");
                    
                    // Check if either NWC or NWD file was created
                    bool fileConverted = false;
                    
                    if (!string.IsNullOrEmpty(task.OutputNwdFile))
                    {
                        // When creating a single NWD file, check if it exists
                        var outputPath = Path.Combine(settings.DefaultNwdPath, task.OutputNwdFile);
                        fileConverted = File.Exists(outputPath);
                    }
                    else
                    {
                        // When creating individual NWC files, check if the NWC file exists
                        fileConverted = File.Exists(nwcPath);
                    }
                    
                    if (fileConverted && !conversionResult.FileResults.ContainsKey(file.Name))
                    {
                        conversionResult.FileResults[file.Name] = true;
                        FileLogger.Log($"Conversion successful for {file.Name}");
                    }
                    else if (!fileConverted && !conversionResult.FileResults.ContainsKey(file.Name))
                    {
                        conversionResult.FileResults[file.Name] = false;
                        if (!conversionResult.FailedFiles.Contains(file.Name))
                        {
                            conversionResult.FailedFiles.Add(file.Name);
                        }
                        FileLogger.LogError($"Conversion failed for {file.Name}: Output file not found");
                    }
                }
                
                // Update overall success based on individual files
                conversionResult.OverallSuccess = conversionResult.FailedFiles.Count == 0;
            
            // If there are failed files and ProcessIndividually is enabled, retry them one by one
            if (task.ProcessIndividually && conversionResult.FailedFiles.Any())
            {
                FileLogger.Log($"Retrying {conversionResult.FailedFiles.Count} failed files individually...");
                FileLogger.Log("Note: 'Load was canceled' errors can be caused by file locking, memory issues, or file access problems");
                
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
                        "/osd" // Output to same directory
                    };
                    
                    var individualScript = $@"& ""{settings.NavisworksToolPath}"" {string.Join(" ", individualArgs)}";
                    FileLogger.Log($"Executing individual conversion: {individualScript}");
                    
                    var individualResult = await _psHelper.RunScriptStringAsync(individualScript);
                    
                    var nwcPath = Path.ChangeExtension(singleFileFullPath, ".nwc");
                    if (!individualResult.Contains("Usage:") && !individualResult.Contains("An error occurred") && File.Exists(nwcPath))
                    {
                        conversionResult.FileResults[file.Name] = true;
                        conversionResult.FailedFiles.Remove(file.Name);
                        FileLogger.Log($"Individual conversion successful for {file.Name}");
                    }
                    else
                    {
                        FileLogger.LogError($"Individual conversion also failed for {file.Name}");
                        
                        // Based on Autodesk forums, "Load was canceled" can be due to:
                        // 1. File locking issues - add delay before retry
                        // 2. Memory issues - try with minimal memory footprint
                        // 3. File access method - some files work better with different approaches
                        
                        FileLogger.Log($"Waiting 5 seconds before final retry attempt for {file.Name}");
                        await Task.Delay(5000); // Wait 5 seconds to ensure file locks are released
                        
                        // Final attempt with different approach based on community solutions
                        var finalArgs = new List<string>
                        {
                            $@"/i ""{singleFilePath}""",
                            "/osd" // Output to same directory
                        };
                        
                        var finalScript = $@"& ""{settings.NavisworksToolPath}"" {string.Join(" ", finalArgs)}";
                        FileLogger.Log($"Executing final conversion attempt: {finalScript}");
                        
                        var finalResult = await _psHelper.RunScriptStringAsync(finalScript);
                        
                        if (!finalResult.Contains("Usage:") && !finalResult.Contains("An error occurred") && File.Exists(nwcPath))
                        {
                            conversionResult.FileResults[file.Name] = true;
                            conversionResult.FailedFiles.Remove(file.Name);
                            FileLogger.Log($"Final conversion attempt successful for {file.Name}");
                        }
                        else
                        {
                            FileLogger.LogError($"All conversion attempts failed for {file.Name}.");
                            FileLogger.LogError($"Possible causes: file locking, insufficient memory, or corrupted source file.");
                            FileLogger.LogError($"Recommended actions: 1) Close all Revit instances, 2) Audit the source file, 3) Try manual conversion");
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
            finally
            {
                // Additional cleanup attempt in case of early exit
                await Task.Delay(1000);
                try { if (File.Exists(tempFileListPath)) File.Delete(tempFileListPath); } catch { }
            }
            
            return conversionResult;
        }
    }
}
