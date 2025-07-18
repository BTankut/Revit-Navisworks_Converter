using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;

namespace RvtToNavisConverter.Views
{
    public partial class LicenseDialog : Window
    {
        public string MachineId { get; set; }

        public LicenseDialog(string machineId)
        {
            InitializeComponent();
            MachineId = machineId;
            MachineIdTextBox.Text = machineId;
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = e.Uri.AbsoluteUri,
                    UseShellExecute = true
                });
                e.Handled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open email client: {ex.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CopyMachineId_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Clipboard.SetText(MachineId);
                MessageBox.Show("Machine ID copied to clipboard!", 
                    "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to copy: {ex.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ContactForLicense_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var subject = Uri.EscapeDataString("RVT to Navisworks Converter License Request");
                var body = Uri.EscapeDataString($"Hello Baris Tankut,\n\nI would like to purchase a license for RVT to Navisworks Converter.\n\nMachine ID: {MachineId}\n\nBest regards,");
                var mailto = $"mailto:baristankut@gmail.com?subject={subject}&body={body}";
                
                Process.Start(new ProcessStartInfo
                {
                    FileName = mailto,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to open email client. Please send an email to baristankut@gmail.com with your Machine ID: {MachineId}\n\nError: {ex.Message}", 
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ExitApplication_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}