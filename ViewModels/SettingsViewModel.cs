using RvtToNavisConverter.Helpers;
using RvtToNavisConverter.Models;
using RvtToNavisConverter.Services;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;

namespace RvtToNavisConverter.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        private readonly ISettingsService _settingsService;
        private readonly IValidationService _validationService;
        private readonly IToolDetectionService _toolDetectionService;
        private AppSettings _appSettings;

        public AppSettings AppSettings
        {
            get => _appSettings;
            set { _appSettings = value; OnPropertyChanged(); }
        }

        private ValidationStatus _revitServerIpStatus;
        public ValidationStatus RevitServerIpStatus { get => _revitServerIpStatus; set { _revitServerIpStatus = value; OnPropertyChanged(); } }

        private ValidationStatus _revitServerAcceleratorStatus;
        public ValidationStatus RevitServerAcceleratorStatus { get => _revitServerAcceleratorStatus; set { _revitServerAcceleratorStatus = value; OnPropertyChanged(); } }

        private ValidationStatus _revitToolPathStatus;
        public ValidationStatus RevitToolPathStatus { get => _revitToolPathStatus; set { _revitToolPathStatus = value; OnPropertyChanged(); } }

        private ValidationStatus _navisworksToolPathStatus;
        public ValidationStatus NavisworksToolPathStatus { get => _navisworksToolPathStatus; set { _navisworksToolPathStatus = value; OnPropertyChanged(); } }

        private ValidationStatus _defaultDownloadPathStatus;
        public ValidationStatus DefaultDownloadPathStatus { get => _defaultDownloadPathStatus; set { _defaultDownloadPathStatus = value; OnPropertyChanged(); } }

        private ValidationStatus _defaultNwcPathStatus;
        public ValidationStatus DefaultNwcPathStatus { get => _defaultNwcPathStatus; set { _defaultNwcPathStatus = value; OnPropertyChanged(); } }

        private ValidationStatus _defaultNwdPathStatus;
        public ValidationStatus DefaultNwdPathStatus { get => _defaultNwdPathStatus; set { _defaultNwdPathStatus = value; OnPropertyChanged(); } }

        // Tool detection properties
        private ObservableCollection<ToolVersion> _detectedRevitServerTools;
        public ObservableCollection<ToolVersion> DetectedRevitServerTools
        {
            get => _detectedRevitServerTools;
            set { _detectedRevitServerTools = value; OnPropertyChanged(); }
        }

        private ObservableCollection<ToolVersion> _detectedNavisworksTools;
        public ObservableCollection<ToolVersion> DetectedNavisworksTools
        {
            get => _detectedNavisworksTools;
            set { _detectedNavisworksTools = value; OnPropertyChanged(); }
        }

        private ToolVersion _selectedRevitServerTool;
        public ToolVersion SelectedRevitServerTool
        {
            get => _selectedRevitServerTool;
            set 
            { 
                _selectedRevitServerTool = value; 
                OnPropertyChanged();
                if (value != null)
                {
                    AppSettings.RevitServerToolPath = value.Path;
                    AppSettings.SelectedRevitServerToolVersion = value.Version;
                }
            }
        }

        private ToolVersion _selectedNavisworksTool;
        public ToolVersion SelectedNavisworksTool
        {
            get => _selectedNavisworksTool;
            set 
            { 
                _selectedNavisworksTool = value; 
                OnPropertyChanged();
                if (value != null)
                {
                    AppSettings.NavisworksToolPath = value.Path;
                    AppSettings.SelectedNavisworksToolVersion = value.Version;
                }
            }
        }

        private bool _isDetectingTools;
        public bool IsDetectingTools
        {
            get => _isDetectingTools;
            set { _isDetectingTools = value; OnPropertyChanged(); }
        }

        public ICommand SaveCommand { get; }
        public ICommand LoadCommand { get; }
        public ICommand ValidateAllCommand { get; }
        public ICommand DetectRevitServerToolsCommand { get; }
        public ICommand DetectNavisworksToolsCommand { get; }
        public ICommand DetectAllToolsCommand { get; }
        public ICommand ValidateRevitServerIpCommand { get; }
        public ICommand ValidateRevitServerAcceleratorCommand { get; }
        public ICommand ValidateRevitToolPathCommand { get; }
        public ICommand ValidateNavisworksToolPathCommand { get; }
        public ICommand ValidateDefaultDownloadPathCommand { get; }
        public ICommand ValidateDefaultNwcPathCommand { get; }
        public ICommand ValidateDefaultNwdPathCommand { get; }
        public ICommand ShowHardwareIdCommand { get; }


        public SettingsViewModel(ISettingsService settingsService, IValidationService validationService, IToolDetectionService toolDetectionService)
        {
            _settingsService = settingsService;
            _validationService = validationService;
            _toolDetectionService = toolDetectionService;
            _appSettings = _settingsService.LoadSettings();

            DetectedRevitServerTools = new ObservableCollection<ToolVersion>();
            DetectedNavisworksTools = new ObservableCollection<ToolVersion>();

            SaveCommand = new RelayCommand(_ => SaveSettings());
            LoadCommand = new RelayCommand(_ => LoadSettings());
            ValidateAllCommand = new RelayCommand(async _ => await ValidateAllAsync());
            DetectRevitServerToolsCommand = new RelayCommand(async _ => await DetectRevitServerToolsAsync());
            DetectNavisworksToolsCommand = new RelayCommand(async _ => await DetectNavisworksToolsAsync());
            DetectAllToolsCommand = new RelayCommand(async _ => await DetectAllToolsAsync());

            ValidateRevitServerIpCommand = new RelayCommand(async _ => RevitServerIpStatus = await _validationService.ValidateIpAddressAsync(AppSettings.RevitServerIp));
            ValidateRevitServerAcceleratorCommand = new RelayCommand(async _ => RevitServerAcceleratorStatus = await _validationService.ValidateIpAddressAsync(AppSettings.RevitServerAccelerator));
            ValidateRevitToolPathCommand = new RelayCommand(_ => RevitToolPathStatus = _validationService.ValidatePath(AppSettings.RevitServerToolPath, true));
            ValidateNavisworksToolPathCommand = new RelayCommand(_ => NavisworksToolPathStatus = _validationService.ValidatePath(AppSettings.NavisworksToolPath, true));
            ValidateDefaultDownloadPathCommand = new RelayCommand(_ => DefaultDownloadPathStatus = _validationService.ValidatePath(AppSettings.DefaultDownloadPath, false));
            ValidateDefaultNwcPathCommand = new RelayCommand(_ => DefaultNwcPathStatus = _validationService.ValidatePath(AppSettings.DefaultNwcPath, false));
            ValidateDefaultNwdPathCommand = new RelayCommand(_ => DefaultNwdPathStatus = _validationService.ValidatePath(AppSettings.DefaultNwdPath, false));
            ShowHardwareIdCommand = new RelayCommand(_ => ShowHardwareId());

            _ = ValidateAllAsync();
            _ = LoadDetectedTools();
        }

        private async Task LoadDetectedTools()
        {
            // Load previously detected tools if any
            if (AppSettings.DetectedRevitServerTools?.Any() == true)
            {
                DetectedRevitServerTools.Clear();
                foreach (var tool in AppSettings.DetectedRevitServerTools)
                {
                    DetectedRevitServerTools.Add(tool);
                }
                
                // Select the previously selected tool
                if (!string.IsNullOrEmpty(AppSettings.SelectedRevitServerToolVersion))
                {
                    SelectedRevitServerTool = DetectedRevitServerTools.FirstOrDefault(t => t.Version == AppSettings.SelectedRevitServerToolVersion);
                }
            }

            if (AppSettings.DetectedNavisworksTools?.Any() == true)
            {
                DetectedNavisworksTools.Clear();
                foreach (var tool in AppSettings.DetectedNavisworksTools)
                {
                    DetectedNavisworksTools.Add(tool);
                }
                
                // Select the previously selected tool
                if (!string.IsNullOrEmpty(AppSettings.SelectedNavisworksToolVersion))
                {
                    SelectedNavisworksTool = DetectedNavisworksTools.FirstOrDefault(t => t.Version == AppSettings.SelectedNavisworksToolVersion);
                }
            }
        }

        private async Task DetectRevitServerToolsAsync()
        {
            try
            {
                IsDetectingTools = true;
                var tools = await _toolDetectionService.DetectRevitServerToolsAsync();
                
                DetectedRevitServerTools.Clear();
                foreach (var tool in tools)
                {
                    DetectedRevitServerTools.Add(tool);
                }

                // Update AppSettings
                AppSettings.DetectedRevitServerTools = tools;

                // Select the first tool if none selected
                if (SelectedRevitServerTool == null && tools.Any())
                {
                    SelectedRevitServerTool = tools.First();
                }

                FileLogger.Log($"Detected {tools.Count} Revit Server Tool versions");
            }
            catch (Exception ex)
            {
                FileLogger.Log($"Error detecting Revit Server Tools: {ex.Message}");
            }
            finally
            {
                IsDetectingTools = false;
            }
        }

        private async Task DetectNavisworksToolsAsync()
        {
            try
            {
                IsDetectingTools = true;
                var tools = await _toolDetectionService.DetectNavisworksToolsAsync();
                
                DetectedNavisworksTools.Clear();
                foreach (var tool in tools)
                {
                    DetectedNavisworksTools.Add(tool);
                }

                // Update AppSettings
                AppSettings.DetectedNavisworksTools = tools;

                // Select the first tool if none selected
                if (SelectedNavisworksTool == null && tools.Any())
                {
                    SelectedNavisworksTool = tools.First();
                }

                FileLogger.Log($"Detected {tools.Count} Navisworks Tool versions");
            }
            catch (Exception ex)
            {
                FileLogger.Log($"Error detecting Navisworks Tools: {ex.Message}");
            }
            finally
            {
                IsDetectingTools = false;
            }
        }

        private async Task DetectAllToolsAsync()
        {
            await DetectRevitServerToolsAsync();
            await DetectNavisworksToolsAsync();
        }

        private async Task ValidateAllAsync()
        {
            RevitServerIpStatus = await _validationService.ValidateIpAddressAsync(AppSettings.RevitServerIp);
            RevitServerAcceleratorStatus = await _validationService.ValidateIpAddressAsync(AppSettings.RevitServerAccelerator);
            RevitToolPathStatus = _validationService.ValidatePath(AppSettings.RevitServerToolPath, true);
            NavisworksToolPathStatus = _validationService.ValidatePath(AppSettings.NavisworksToolPath, true);
            DefaultDownloadPathStatus = _validationService.ValidatePath(AppSettings.DefaultDownloadPath, false);
            DefaultNwcPathStatus = _validationService.ValidatePath(AppSettings.DefaultNwcPath, false);
            DefaultNwdPathStatus = _validationService.ValidatePath(AppSettings.DefaultNwdPath, false);
        }

        private void SaveSettings()
        {
            _settingsService.SaveSettings(AppSettings);
            _ = ValidateAllAsync();
        }

        private void LoadSettings()
        {
            AppSettings = _settingsService.LoadSettings();
            _ = ValidateAllAsync();
            _ = LoadDetectedTools();
        }

        private void ShowHardwareId()
        {
            try
            {
                var app = (App)Application.Current;
                var serviceProvider = app.Services;
                if (serviceProvider != null)
                {
                    var hardwareIdService = serviceProvider.GetService<IHardwareIdService>();
                    if (hardwareIdService != null)
                    {
                        var hardwareId = hardwareIdService.GetHardwareId();
                        var message = $"Your Hardware ID:\n\n{hardwareId}\n\n" +
                                      "Send this ID to the developer when purchasing a license.\n" +
                                      "You can copy this text with Ctrl+C.";
                        
                        MessageBox.Show(message, "Hardware ID", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error getting Hardware ID: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void RefreshValidation()
        {
            _ = ValidateAllAsync();
        }
    }
}
