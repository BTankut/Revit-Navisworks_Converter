using RvtToNavisConverter.Helpers;
using RvtToNavisConverter.Models;
using RvtToNavisConverter.Services;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;

namespace RvtToNavisConverter.ViewModels
{
    public class SettingsViewModel : ViewModelBase
    {
        private readonly ISettingsService _settingsService;
        private readonly IValidationService _validationService;
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


        public ICommand SaveCommand { get; }
        public ICommand LoadCommand { get; }
        public ICommand ValidateAllCommand { get; }
        public ICommand ValidateRevitServerIpCommand { get; }
        public ICommand ValidateRevitServerAcceleratorCommand { get; }
        public ICommand ValidateRevitToolPathCommand { get; }
        public ICommand ValidateNavisworksToolPathCommand { get; }
        public ICommand ValidateDefaultDownloadPathCommand { get; }
        public ICommand ValidateDefaultNwcPathCommand { get; }
        public ICommand ValidateDefaultNwdPathCommand { get; }


        public SettingsViewModel(ISettingsService settingsService, IValidationService validationService)
        {
            _settingsService = settingsService;
            _validationService = validationService;
            _appSettings = _settingsService.LoadSettings();

            SaveCommand = new RelayCommand(_ => SaveSettings());
            LoadCommand = new RelayCommand(_ => LoadSettings());
            ValidateAllCommand = new RelayCommand(async _ => await ValidateAllAsync());

            ValidateRevitServerIpCommand = new RelayCommand(async _ => RevitServerIpStatus = await _validationService.ValidateIpAddressAsync(AppSettings.RevitServerIp));
            ValidateRevitServerAcceleratorCommand = new RelayCommand(async _ => RevitServerAcceleratorStatus = await _validationService.ValidateIpAddressAsync(AppSettings.RevitServerAccelerator));
            ValidateRevitToolPathCommand = new RelayCommand(_ => RevitToolPathStatus = _validationService.ValidatePath(AppSettings.RevitServerToolPath, true));
            ValidateNavisworksToolPathCommand = new RelayCommand(_ => NavisworksToolPathStatus = _validationService.ValidatePath(AppSettings.NavisworksToolPath, true));
            ValidateDefaultDownloadPathCommand = new RelayCommand(_ => DefaultDownloadPathStatus = _validationService.ValidatePath(AppSettings.DefaultDownloadPath, false));
            ValidateDefaultNwcPathCommand = new RelayCommand(_ => DefaultNwcPathStatus = _validationService.ValidatePath(AppSettings.DefaultNwcPath, false));
            ValidateDefaultNwdPathCommand = new RelayCommand(_ => DefaultNwdPathStatus = _validationService.ValidatePath(AppSettings.DefaultNwdPath, false));

            _ = ValidateAllAsync();
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
        }
    }
}
