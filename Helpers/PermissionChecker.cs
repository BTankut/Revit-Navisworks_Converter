using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Windows;
using RvtToNavisConverter.Services;

namespace RvtToNavisConverter.Helpers
{
    public static class PermissionChecker
    {
        public class PermissionResult
        {
            public bool HasPermission { get; set; }
            public string ErrorMessage { get; set; } = string.Empty;
            public string FixSuggestion { get; set; } = string.Empty;
        }

        public static PermissionResult CheckDirectoryAccess(string path, bool requireWrite = true)
        {
            var result = new PermissionResult();

            try
            {
                if (!Directory.Exists(path))
                {
                    result.HasPermission = false;
                    result.ErrorMessage = $"Directory does not exist: {path}";
                    result.FixSuggestion = "Directory will be created automatically if parent permissions allow.";
                    return result;
                }

                // Test read access
                Directory.GetFiles(path);

                if (requireWrite)
                {
                    // Test write access by creating a temporary file
                    string testFile = Path.Combine(path, $"permission_test_{Guid.NewGuid()}.tmp");
                    File.WriteAllText(testFile, "test");
                    File.Delete(testFile);
                }

                result.HasPermission = true;
                return result;
            }
            catch (UnauthorizedAccessException)
            {
                result.HasPermission = false;
                result.ErrorMessage = $"Access denied to directory: {path}";
                result.FixSuggestion = $"Run as Administrator or grant full control permissions to current user for: {path}";
                return result;
            }
            catch (Exception ex)
            {
                result.HasPermission = false;
                result.ErrorMessage = $"Error accessing directory {path}: {ex.Message}";
                result.FixSuggestion = "Check if path exists and is accessible.";
                return result;
            }
        }

        public static PermissionResult CheckNetworkShareAccess(string uncPath)
        {
            var result = new PermissionResult();

            try
            {
                if (!uncPath.StartsWith(@"\\"))
                {
                    result.HasPermission = false;
                    result.ErrorMessage = "Invalid UNC path format";
                    return result;
                }

                // Test network share access
                var dirInfo = new DirectoryInfo(uncPath);
                dirInfo.GetDirectories();

                result.HasPermission = true;
                return result;
            }
            catch (UnauthorizedAccessException)
            {
                result.HasPermission = false;
                result.ErrorMessage = $"Access denied to network share: {uncPath}";
                result.FixSuggestion = "Contact network administrator to grant access or map network drive with credentials.";
                return result;
            }
            catch (DirectoryNotFoundException)
            {
                result.HasPermission = false;
                result.ErrorMessage = $"Network path not found: {uncPath}";
                result.FixSuggestion = "Check network connectivity and verify server is accessible.";
                return result;
            }
            catch (Exception ex)
            {
                result.HasPermission = false;
                result.ErrorMessage = $"Error accessing network share {uncPath}: {ex.Message}";
                result.FixSuggestion = "Check network connectivity and credentials.";
                return result;
            }
        }

        public static bool TryCreateDirectoryWithPermissions(string path)
        {
            try
            {
                if (!Directory.Exists(path))
                {
                    Directory.CreateDirectory(path);
                    
                    // Try to set full control for current user
                    var currentUser = WindowsIdentity.GetCurrent();
                    var directoryInfo = new DirectoryInfo(path);
                    var directorySecurity = directoryInfo.GetAccessControl();
                    
                    var accessRule = new FileSystemAccessRule(
                        currentUser.User,
                        FileSystemRights.FullControl,
                        InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
                        PropagationFlags.None,
                        AccessControlType.Allow);
                    
                    directorySecurity.SetAccessRule(accessRule);
                    directoryInfo.SetAccessControl(directorySecurity);
                }
                return true;
            }
            catch (Exception ex)
            {
                FileLogger.LogError($"Failed to create directory with permissions: {path}. Error: {ex.Message}");
                return false;
            }
        }

        public static void RunAsAdministrator()
        {
            try
            {
                var startInfo = new ProcessStartInfo
                {
                    UseShellExecute = true,
                    WorkingDirectory = Environment.CurrentDirectory,
                    FileName = Process.GetCurrentProcess().MainModule?.FileName,
                    Verb = "runas"
                };

                // Start the new process
                var newProcess = Process.Start(startInfo);
                
                if (newProcess != null)
                {
                    // Force close current application to prevent conflict
                    Environment.Exit(0);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to restart as administrator: {ex.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public static List<PermissionResult> ValidateAllPermissions()
        {
            var results = new List<PermissionResult>();

            // Check application directory (for logging)
            var appDir = AppDomain.CurrentDomain.BaseDirectory;
            results.Add(CheckDirectoryAccess(appDir, true));

            // Check temp directory
            var tempDir = Path.GetTempPath();
            results.Add(CheckDirectoryAccess(tempDir, true));

            // Check default paths from settings
            try
            {
                var settingsService = new SettingsService();
                var settings = settingsService.LoadSettings();
                
                if (!string.IsNullOrEmpty(settings.DefaultDownloadPath))
                {
                    var downloadResult = CheckDirectoryAccess(settings.DefaultDownloadPath, true);
                    downloadResult.ErrorMessage = $"Download Path: {downloadResult.ErrorMessage}";
                    results.Add(downloadResult);
                }

                // NWC files are created automatically in the same folder as RVT files by FileToolsTaskRunner
                // So we don't need to check NWC path permissions

                if (!string.IsNullOrEmpty(settings.DefaultNwdPath))
                {
                    var nwdResult = CheckDirectoryAccess(settings.DefaultNwdPath, true);
                    nwdResult.ErrorMessage = $"NWD Output Path: {nwdResult.ErrorMessage}";
                    results.Add(nwdResult);
                }

                // Check network share access if configured
                if (!string.IsNullOrEmpty(settings.RevitServerIp))
                {
                    var revitServerPath = $@"\\{settings.RevitServerIp}\Revit Server 2021\Projects";
                    var networkResult = CheckNetworkShareAccess(revitServerPath);
                    networkResult.ErrorMessage = $"Revit Server: {networkResult.ErrorMessage}";
                    results.Add(networkResult);
                }
            }
            catch (Exception ex)
            {
                var errorResult = new PermissionResult
                {
                    HasPermission = false,
                    ErrorMessage = $"Failed to load settings: {ex.Message}",
                    FixSuggestion = "Check appsettings.json file exists and is valid."
                };
                results.Add(errorResult);
            }

            return results;
        }

        public static bool IsRunningAsAdministrator()
        {
            try
            {
                var identity = WindowsIdentity.GetCurrent();
                var principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
            catch
            {
                return false;
            }
        }
    }
}