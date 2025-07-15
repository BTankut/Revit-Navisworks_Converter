using RvtToNavisConverter.Models;
using RvtToNavisConverter.ViewModels;
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
