using RvtToNavisConverter.Models;
using RvtToNavisConverter.ViewModels;
using System.Windows;
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
    }
}
