using Newtonsoft.Json;
using RvtToNavisConverter.Models;
using System;
using System.IO;

namespace RvtToNavisConverter.Services
{
    public interface ISettingsService
    {
        AppSettings LoadSettings();
        void SaveSettings(AppSettings settings);
    }

    public class SettingsService : ISettingsService
    {
        private readonly string _settingsFilePath;

        public SettingsService()
        {
            // Assuming the appsettings.json is in the same directory as the executable
            _settingsFilePath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        }

        public AppSettings LoadSettings()
        {
            if (!File.Exists(_settingsFilePath))
            {
                // Create a default settings file if it doesn't exist
                var defaultSettings = new AppSettings
                {
                    RevitServerIp = "192.168.200.115",
                    RevitServerAccelerator = "192.168.90.197",
                    RevitServerToolPath = @"C:\Program Files\Autodesk\Revit Server 2021\Tools\RevitServerToolCommand\RevitServerTool.exe",
                    NavisworksToolPath = @"C:\Program Files\Autodesk\Navisworks Manage 2022\FileToolsTaskRunner.exe",
                    DefaultDownloadPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "RvtToNavisExports", "RVT"),
                    DefaultNwcPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "RvtToNavisExports", "NWC"),
                    DefaultNwdPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "RvtToNavisExports", "NWD")
                };
                SaveSettings(defaultSettings);
                return defaultSettings;
            }

            var json = File.ReadAllText(_settingsFilePath);
            var settingsRoot = JsonConvert.DeserializeObject<SettingsRoot>(json);
            var settings = settingsRoot?.AppSettings ?? new AppSettings();

            if (string.IsNullOrWhiteSpace(settings.DefaultDownloadPath))
            {
                settings.DefaultDownloadPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "RvtToNavisExports", "RVT");
            }
            if (string.IsNullOrWhiteSpace(settings.DefaultNwcPath))
            {
                settings.DefaultNwcPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "RvtToNavisExports", "NWC");
            }
            if (string.IsNullOrWhiteSpace(settings.DefaultNwdPath))
            {
                settings.DefaultNwdPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "RvtToNavisExports", "NWD");
            }

            return settings;
        }

        public void SaveSettings(AppSettings settings)
        {
            var settingsRoot = new SettingsRoot { AppSettings = settings };
            var json = JsonConvert.SerializeObject(settingsRoot, Formatting.Indented);
            File.WriteAllText(_settingsFilePath, json);
        }
    }

    // Helper class to match the JSON structure
    public class SettingsRoot
    {
        public AppSettings AppSettings { get; set; } = new AppSettings();
    }
}
