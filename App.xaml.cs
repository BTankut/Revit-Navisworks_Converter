using Microsoft.Extensions.DependencyInjection;
using RvtToNavisConverter.Helpers;
using RvtToNavisConverter.Services;
using RvtToNavisConverter.ViewModels;
using RvtToNavisConverter.Views;
using System;
using System.Windows;
using System.Windows.Threading;

namespace RvtToNavisConverter
{
    public partial class App : Application
    {
        private ServiceProvider? _serviceProvider;
        public IServiceProvider? Services => _serviceProvider;

        public App()
        {
            var services = new ServiceCollection();
            ConfigureServices(services);
            _serviceProvider = services.BuildServiceProvider();
        }

        private void ConfigureServices(IServiceCollection services)
        {
            // Helpers
            services.AddSingleton<PowerShellHelper>();
            services.AddSingleton<FileHelper>();

            // Services
            services.AddSingleton<ISettingsService, SettingsService>();
            services.AddSingleton<IRevitServerService, RevitServerService>();
            services.AddSingleton<ILocalFileService, LocalFileService>();
            services.AddSingleton<IFileDownloadService, FileDownloadService>();
            services.AddSingleton<INavisworksConversionService, NavisworksConversionService>();
            services.AddSingleton<IFileStatusService, FileStatusService>();
            services.AddSingleton<IValidationService, ValidationService>();
            services.AddSingleton<IToolDetectionService, ToolDetectionService>();
            services.AddSingleton<IRevitFileVersionService, RevitFileVersionService>();
            services.AddSingleton<SelectionManager>();
            
            // License Services
            services.AddSingleton<IHardwareIdService, HardwareIdService>();
            services.AddSingleton<ICryptoService, CryptoService>();
            services.AddSingleton<IRsaCryptoService, RsaCryptoService>();
            services.AddSingleton<ILicenseService, LicenseService>();

            // ViewModels
            services.AddSingleton<MainViewModel>();
            services.AddSingleton<SettingsViewModel>();
            services.AddSingleton<ProgressViewModel>();
            services.AddSingleton<MonitorViewModel>();

            // Views
            services.AddSingleton<MainWindow>();
            services.AddTransient<MonitorView>(); // Transient so a new window can be opened if closed

            // Pass the service provider itself to the DI container
            services.AddSingleton<IServiceProvider>(x => _serviceProvider!);
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            DispatcherUnhandledException += App_DispatcherUnhandledException;

            try
            {
#if !DEBUG
                // Validate license only in Release mode
                var licenseService = _serviceProvider!.GetService<ILicenseService>();
                if (licenseService != null)
                {
                    // Check for and import any license files first
                    licenseService.CheckAndImportLicenseFile();
                    
                    var validationResult = licenseService.ValidateLicense();
                    
                    // If no license found, create trial
                    if (validationResult.Status == Models.LicenseStatus.NotFound)
                    {
                        validationResult = licenseService.CreateTrialLicense();
                    }
                    
                    // If license is expired, invalid, or tampered
                    if (validationResult.Status != Models.LicenseStatus.Valid)
                    {
                        var licenseDialog = new LicenseDialog(validationResult.HardwareId ?? "Unknown");
                        licenseDialog.ShowDialog();
                        Current.Shutdown();
                        return;
                    }
                    
                    // Store license validation result for use in MainViewModel
                    Current.Properties["LicenseValidation"] = validationResult;
                }
#else
                // In Debug mode, create a dummy validation result with full trial
                Current.Properties["LicenseValidation"] = new Models.LicenseValidationResult
                {
                    Status = Models.LicenseStatus.Valid,
                    DaysRemaining = 30,
                    Message = "Debug Mode - No License Check"
                };
#endif

                var mainWindow = _serviceProvider!.GetService<MainWindow>();
                if (mainWindow != null)
                {
                    var mainViewModel = _serviceProvider.GetService<MainViewModel>();
                    if (mainViewModel != null)
                    {
                        mainWindow.DataContext = mainViewModel;
                        mainWindow.Show();
                    }
                    else
                    {
                        throw new InvalidOperationException("MainViewModel could not be resolved from the service provider.");
                    }
                }
            }
            catch (Exception ex)
            {
                HandleException(ex);
            }
        }

        private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            HandleException(e.Exception);
            e.Handled = true; // Prevents the app from crashing immediately
        }

        private void HandleException(Exception ex)
        {
            FileLogger.Log(ex);
            MessageBox.Show("An unexpected error occurred. The application will now close. Please check the app_log.txt file for details.", "Fatal Error", MessageBoxButton.OK, MessageBoxImage.Error);
            Current.Shutdown();
        }
    }
}
