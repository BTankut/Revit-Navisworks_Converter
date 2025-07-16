using RvtToNavisConverter.Models;
using RvtToNavisConverter.ViewModels;
using RvtToNavisConverter.Helpers;
using RvtToNavisConverter.Views;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace RvtToNavisConverter
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            // DataContext will be set from App.xaml.cs using Dependency Injection
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Check permissions on startup
            await System.Threading.Tasks.Task.Run(() =>
            {
                var permissionResults = PermissionChecker.ValidateAllPermissions();
                var hasIssues = permissionResults.Any(r => !r.HasPermission);

                if (hasIssues)
                {
                    Dispatcher.Invoke(() =>
                    {
                        var dialog = new PermissionDialog(permissionResults)
                        {
                            Owner = this
                        };

                        var result = dialog.ShowDialog();
                        
                        if (result != true && !dialog.ContinueWithIssues)
                        {
                            // User chose to exit
                            Application.Current.Shutdown();
                        }
                        else if (dialog.ContinueWithIssues)
                        {
                            // Log warning that we're continuing with issues
                            FileLogger.LogWarning("Application started with unresolved permission issues.");
                        }
                    });
                }
            });
        }

        private void ListViewItem_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.DataContext is IFileSystemItem item)
            {
                if (item.IsDirectory)
                {
                    if (DataContext is MainViewModel vm)
                    {
                        vm.NavigateToFolderCommand.Execute(item);
                    }
                }
            }
        }

        private void DownloadCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.DataContext is IFileSystemItem item)
            {
                if (DataContext is MainViewModel vm)
                {
                    // Sadece true/false arasında geçiş yap, indeterminate durumunu kullanıcı seçemesin
                    bool? currentValue = item.IsSelectedForDownload;
                    bool? newValue;
                    
                    // Eğer null (indeterminate) ise veya false ise, true yap
                    if (currentValue != true)
                        newValue = true;
                    else
                        newValue = false;
                    
                    // SelectionManager'a bildir (UI güncellemesi otomatik olacak)
                    vm.HandleSelectionChange(item, true, newValue);
                }
            }
        }

        private void ConversionCheckBox_Click(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox checkBox && checkBox.DataContext is IFileSystemItem item)
            {
                if (DataContext is MainViewModel vm)
                {
                    // Sadece true/false arasında geçiş yap, indeterminate durumunu kullanıcı seçemesin
                    bool? currentValue = item.IsSelectedForConversion;
                    bool? newValue;
                    
                    // Eğer null (indeterminate) ise veya false ise, true yap
                    if (currentValue != true)
                        newValue = true;
                    else
                        newValue = false;
                    
                    // SelectionManager'a bildir (UI güncellemesi otomatik olacak)
                    vm.HandleSelectionChange(item, false, newValue);
                }
            }
        }
    }
}
