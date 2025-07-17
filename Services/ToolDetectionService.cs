using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using RvtToNavisConverter.Helpers;
using RvtToNavisConverter.Models;

namespace RvtToNavisConverter.Services
{
    public class ToolDetectionService : IToolDetectionService
    {
        private readonly string _programFilesPath = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        private readonly string _programFilesX86Path = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86);

        public async Task<List<ToolVersion>> DetectRevitServerToolsAsync()
        {
            return await Task.Run(() =>
            {
                var tools = new List<ToolVersion>();
                var searchPaths = new[] { _programFilesPath, _programFilesX86Path };

                foreach (var basePath in searchPaths)
                {
                    if (!Directory.Exists(basePath)) continue;

                    var autodeskPath = Path.Combine(basePath, "Autodesk");
                    if (!Directory.Exists(autodeskPath)) continue;

                    // Search for Revit Server folders
                    var revitServerDirs = Directory.GetDirectories(autodeskPath, "Revit Server*", SearchOption.TopDirectoryOnly);
                    
                    foreach (var dir in revitServerDirs)
                    {
                        var toolPath = Path.Combine(dir, "Tools", "RevitServerToolCommand", "RevitServerTool.exe");
                        if (File.Exists(toolPath))
                        {
                            var version = ExtractVersionFromPath(dir, ToolType.RevitServerTool);
                            if (!string.IsNullOrEmpty(version))
                            {
                                tools.Add(new ToolVersion
                                {
                                    Name = "Revit Server Tool",
                                    Version = version,
                                    Path = toolPath,
                                    Type = ToolType.RevitServerTool,
                                    DetectedAt = DateTime.Now
                                });
                            }
                        }
                    }
                }

                return tools.OrderByDescending(t => t.Version).ToList();
            });
        }

        public async Task<List<ToolVersion>> DetectNavisworksToolsAsync()
        {
            return await Task.Run(() =>
            {
                var tools = new List<ToolVersion>();
                var searchPaths = new[] { _programFilesPath, _programFilesX86Path };

                foreach (var basePath in searchPaths)
                {
                    if (!Directory.Exists(basePath)) continue;

                    var autodeskPath = Path.Combine(basePath, "Autodesk");
                    if (!Directory.Exists(autodeskPath)) continue;

                    // Search for Navisworks folders
                    var navisworksDirs = Directory.GetDirectories(autodeskPath, "Navisworks*", SearchOption.TopDirectoryOnly);
                    
                    foreach (var dir in navisworksDirs)
                    {
                        var toolPath = Path.Combine(dir, "FileToolsTaskRunner.exe");
                        if (File.Exists(toolPath))
                        {
                            var version = ExtractVersionFromPath(dir, ToolType.NavisworksFileToolsTaskRunner);
                            if (!string.IsNullOrEmpty(version))
                            {
                                tools.Add(new ToolVersion
                                {
                                    Name = "Navisworks File Tools",
                                    Version = version,
                                    Path = toolPath,
                                    Type = ToolType.NavisworksFileToolsTaskRunner,
                                    DetectedAt = DateTime.Now
                                });
                            }
                        }
                    }
                }

                return tools.OrderByDescending(t => t.Version).ToList();
            });
        }

        public async Task<List<ToolVersion>> DetectAllToolsAsync()
        {
            var revitTools = await DetectRevitServerToolsAsync();
            var navisworksTools = await DetectNavisworksToolsAsync();
            
            var allTools = new List<ToolVersion>();
            allTools.AddRange(revitTools);
            allTools.AddRange(navisworksTools);
            
            return allTools;
        }

        public string ExtractVersionFromPath(string path, ToolType toolType)
        {
            try
            {
                var dirName = Path.GetFileName(path);
                
                // Extract year version from folder name
                var yearMatch = Regex.Match(dirName, @"\b(20\d{2})\b");
                if (yearMatch.Success)
                {
                    return yearMatch.Groups[1].Value;
                }

                // Try to extract version with format like "2021.1", "2022.2", etc.
                var versionMatch = Regex.Match(dirName, @"\b(20\d{2}(?:\.\d+)?)\b");
                if (versionMatch.Success)
                {
                    return versionMatch.Groups[1].Value;
                }

                FileLogger.Log($"Could not extract version from path: {path}");
                return string.Empty;
            }
            catch (Exception ex)
            {
                FileLogger.Log($"Error extracting version from path {path}: {ex.Message}");
                return string.Empty;
            }
        }
    }
}