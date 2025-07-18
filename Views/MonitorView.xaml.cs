using System.Windows;
using System.Windows.Controls;

namespace RvtToNavisConverter.Views
{
    public partial class MonitorView : Window
    {
        public MonitorView()
        {
            InitializeComponent();
        }

        private void LogTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Auto-scroll to the bottom when new text is added
            if (sender is TextBox textBox)
            {
                textBox.ScrollToEnd();
            }
        }
    }
}
