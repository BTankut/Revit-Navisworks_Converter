using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using RvtToNavisConverter.Helpers;

namespace RvtToNavisConverter.Services
{
    public class RevitFileVersionService : IRevitFileVersionService
    {
        // Revit version mapping based on the file format version
        private readonly Dictionary<string, string> _versionMap = new Dictionary<string, string>
        {
            { "Autodesk Revit 2025", "2025" },
            { "Autodesk Revit 2024", "2024" },
            { "Autodesk Revit 2023", "2023" },
            { "Autodesk Revit 2022", "2022" },
            { "Autodesk Revit 2021", "2021" },
            { "Autodesk Revit 2020", "2020" },
            { "Autodesk Revit 2019", "2019" },
            { "Autodesk Revit 2018", "2018" },
            { "Autodesk Revit 2017", "2017" },
            { "Autodesk Revit 2016", "2016" },
            { "Autodesk Revit 2015", "2015" }
        };

        public async Task<string> GetRevitFileVersionAsync(string filePath)
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (!File.Exists(filePath))
                    {
                        FileLogger.Log($"File not found: {filePath}");
                        return string.Empty;
                    }

                    // Revit files are OLE Structured Storage files
                    // The version info is typically in the BasicFileInfo stream
                    // For simplicity, we'll read the file header to detect version
                    
                    using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        // Read first 1KB of the file
                        byte[] buffer = new byte[1024];
                        int bytesRead = fileStream.Read(buffer, 0, buffer.Length);
                        
                        if (bytesRead < 100)
                        {
                            FileLogger.Log($"File too small to be a valid Revit file: {filePath}");
                            return string.Empty;
                        }

                        // Convert to string to search for version patterns
                        string content = Encoding.ASCII.GetString(buffer);
                        
                        // Look for Autodesk Revit version strings
                        foreach (var versionEntry in _versionMap)
                        {
                            if (content.Contains(versionEntry.Key))
                            {
                                FileLogger.Log($"Detected Revit version {versionEntry.Value} for file: {filePath}");
                                return versionEntry.Value;
                            }
                        }

                        // Alternative method: Check for Unicode version strings
                        string unicodeContent = Encoding.Unicode.GetString(buffer);
                        foreach (var versionEntry in _versionMap)
                        {
                            if (unicodeContent.Contains(versionEntry.Key))
                            {
                                FileLogger.Log($"Detected Revit version {versionEntry.Value} (Unicode) for file: {filePath}");
                                return versionEntry.Value;
                            }
                        }

                        // If no version found in header, try a deeper scan
                        fileStream.Seek(0, SeekOrigin.Begin);
                        buffer = new byte[4096]; // Read more bytes
                        bytesRead = fileStream.Read(buffer, 0, buffer.Length);
                        
                        content = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                        unicodeContent = Encoding.Unicode.GetString(buffer, 0, bytesRead);
                        
                        // Search for year patterns in the content
                        var yearPattern = System.Text.RegularExpressions.Regex.Match(content + unicodeContent, @"20[1-2][0-9]");
                        if (yearPattern.Success)
                        {
                            string detectedYear = yearPattern.Value;
                            if (int.TryParse(detectedYear, out int year) && year >= 2015 && year <= 2025)
                            {
                                FileLogger.Log($"Detected Revit version {detectedYear} (pattern match) for file: {filePath}");
                                return detectedYear;
                            }
                        }
                    }

                    // Try to extract version from filename as last resort
                    string fileName = System.IO.Path.GetFileNameWithoutExtension(filePath);
                    
                    // Check for R20, R21, R22 patterns in filename
                    var filenameMatch = System.Text.RegularExpressions.Regex.Match(fileName, @"R(\d{2})");
                    if (filenameMatch.Success)
                    {
                        string yearSuffix = filenameMatch.Groups[1].Value;
                        string fullYear = "20" + yearSuffix;
                        FileLogger.Log($"Detected Revit version {fullYear} from filename pattern for file: {filePath}");
                        return fullYear;
                    }
                    
                    // Check for 2020, 2021, 2022 patterns in filename
                    var yearMatch = System.Text.RegularExpressions.Regex.Match(fileName, @"(20[1-2][0-9])");
                    if (yearMatch.Success)
                    {
                        string year = yearMatch.Groups[1].Value;
                        FileLogger.Log($"Detected Revit version {year} from filename for file: {filePath}");
                        return year;
                    }
                    
                    FileLogger.Log($"Could not detect Revit version for file: {filePath}");
                    return string.Empty;
                }
                catch (Exception ex)
                {
                    FileLogger.Log($"Error reading Revit file version from {filePath}: {ex.Message}");
                    return string.Empty;
                }
            });
        }

        public string GetRevitVersionYear(string version)
        {
            // Extract just the year from version string
            if (string.IsNullOrEmpty(version))
                return string.Empty;

            // If it's already a year, return it
            if (version.Length == 4 && int.TryParse(version, out int year))
                return version;

            // Extract year from longer version strings
            var match = System.Text.RegularExpressions.Regex.Match(version, @"20\d{2}");
            return match.Success ? match.Value : version;
        }

        public bool IsVersionCompatible(string fileVersion, string toolVersion)
        {
            if (string.IsNullOrEmpty(fileVersion) || string.IsNullOrEmpty(toolVersion))
                return false;

            string fileYear = GetRevitVersionYear(fileVersion);
            string toolYear = GetRevitVersionYear(toolVersion);

            // For Revit, file version must match tool version exactly
            return fileYear == toolYear;
        }

        public string GetRevitVersionFromServerPath(string serverPath)
        {
            try
            {
                if (string.IsNullOrEmpty(serverPath))
                    return string.Empty;

                // Look for "Revit Server YYYY" pattern in path
                var match = System.Text.RegularExpressions.Regex.Match(serverPath, @"\\Revit Server\s+(\d{4})\\", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    string year = match.Groups[1].Value;
                    FileLogger.Log($"Extracted Revit version {year} from server path: {serverPath}");
                    return year;
                }

                // Alternative pattern without space
                match = System.Text.RegularExpressions.Regex.Match(serverPath, @"\\RevitServer(\d{4})\\", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                if (match.Success)
                {
                    string year = match.Groups[1].Value;
                    FileLogger.Log($"Extracted Revit version {year} from server path (no space): {serverPath}");
                    return year;
                }

                return string.Empty;
            }
            catch (Exception ex)
            {
                FileLogger.Log($"Error extracting version from server path {serverPath}: {ex.Message}");
                return string.Empty;
            }
        }
    }
}