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
                // Add timestamp to each log entry
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                LogEntries += $"[{timestamp}] {entry}{Environment.NewLine}";
            });
        }
    }
}
