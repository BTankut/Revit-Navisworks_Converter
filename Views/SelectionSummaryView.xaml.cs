using System.Windows;

namespace RvtToNavisConverter.Views
{
    public partial class SelectionSummaryView : Window
    {
        public SelectionSummaryView()
        {
            InitializeComponent();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}