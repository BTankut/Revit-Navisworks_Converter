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
                var mainWindow = _serviceProvider!.GetService<MainWindow>();
                if (mainWindow != null)
                {
                    mainWindow.DataContext = _serviceProvider.GetService<MainViewModel>();
                    mainWindow.Show();
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
