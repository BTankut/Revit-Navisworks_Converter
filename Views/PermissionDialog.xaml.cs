using System.Collections.Generic;
using System.Linq;
using System.Windows;
using RvtToNavisConverter.Helpers;

namespace RvtToNavisConverter.Views
{
    public partial class PermissionDialog : Window
    {
        private List<PermissionChecker.PermissionResult> _permissionIssues;

        public bool ContinueWithIssues { get; private set; } = false;

        public PermissionDialog(List<PermissionChecker.PermissionResult> permissionIssues)
        {
            InitializeComponent();
            _permissionIssues = permissionIssues;
            
            // Show only failed permissions in a simple format
            var failedPermissions = permissionIssues.Where(p => !p.HasPermission).ToList();
            PermissionIssues.ItemsSource = failedPermissions;
            
            // Update button text based on admin status
            if (!PermissionChecker.IsRunningAsAdministrator())
            {
                BtnSetup.Content = "Request Admin Access";
                ProgressText.Text = "Administrator access required...";
            }
            else
            {
                BtnSetup.Content = "Create Folders";
                ProgressText.Text = "Creating folders...";
            }
        }

        private async void BtnSetup_Click(object sender, RoutedEventArgs e)
        {
            // Show progress
            BtnSetup.IsEnabled = false;
            BtnSkip.IsEnabled = false;
            BtnCancel.IsEnabled = false;
            ProgressPanel.Visibility = Visibility.Visible;
            
            await System.Threading.Tasks.Task.Run(() =>
            {
                System.Threading.Thread.Sleep(500); // Brief pause for UX
                
                // First try admin if not already
                if (!PermissionChecker.IsRunningAsAdministrator())
                {
                    Dispatcher.Invoke(() =>
                    {
                        ProgressText.Text = "Requesting administrator access...";
                    });
                    System.Threading.Thread.Sleep(1000); // Show message longer
                    PermissionChecker.RunAsAdministrator();
                    return;
                }
                
                // Try to fix permissions
                Dispatcher.Invoke(() =>
                {
                    ProgressText.Text = "Creating folders...";
                });
                
                var fixedCount = 0;
                try
                {
                    var settingsService = new RvtToNavisConverter.Services.SettingsService();
                    var settings = settingsService.LoadSettings();

                    var directoriesToCreate = new List<string>();
                    if (!string.IsNullOrEmpty(settings.DefaultDownloadPath))
                        directoriesToCreate.Add(settings.DefaultDownloadPath);
                    if (!string.IsNullOrEmpty(settings.DefaultNwdPath))
                        directoriesToCreate.Add(settings.DefaultNwdPath);

                    foreach (var directory in directoriesToCreate)
                    {
                        if (PermissionChecker.TryCreateDirectoryWithPermissions(directory))
                        {
                            fixedCount++;
                        }
                    }
                }
                catch { }
                
                Dispatcher.Invoke(() =>
                {
                    ProgressPanel.Visibility = Visibility.Collapsed;
                    
                    if (fixedCount > 0)
                    {
                        MessageBox.Show("Setup completed successfully!\n\nThe application is ready to use.", 
                                      "Setup Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                        ContinueWithIssues = true;
                        DialogResult = true;
                        Close();
                    }
                    else
                    {
                        MessageBox.Show("Setup could not be completed automatically.\n\nYou can continue anyway, but some features may not work properly.", 
                                      "Setup Issue", MessageBoxButton.OK, MessageBoxImage.Warning);
                        BtnSetup.IsEnabled = true;
                        BtnSkip.IsEnabled = true;
                        BtnCancel.IsEnabled = true;
                    }
                });
            });
        }

        private void BtnContinue_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                "Continuing with permission issues may cause the application to malfunction.\n\n" +
                "Some features may not work properly:\n" +
                "• File downloads may fail\n" +
                "• Conversion outputs may not be saved\n" +
                "• Network shares may be inaccessible\n\n" +
                "Are you sure you want to continue?",
                "Continue With Issues?", 
                MessageBoxButton.YesNo, 
                MessageBoxImage.Warning);

            if (result == MessageBoxResult.Yes)
            {
                ContinueWithIssues = true;
                DialogResult = true;
                Close();
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}