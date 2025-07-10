using System;
using System.Windows;

namespace RvtToNavisConverter.ViewModels
{
    public class MonitorViewModel : ViewModelBase
    {
        private string _logEntries = string.Empty;
        public string LogEntries
        {
            get => _logEntries;
            private set
            {
                _logEntries = value;
                OnPropertyChanged();
            }
        }

        public void AddLogEntry(string entry)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                LogEntries += entry + Environment.NewLine;
            });
        }
    }
}
