using System;
using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Input;
using RvtToNavisConverter.Helpers;

namespace RvtToNavisConverter.ViewModels
{
    public class ProgressViewModel : ViewModelBase
    {
        private int _progressPercentage;
        public int ProgressPercentage
        {
            get => _progressPercentage;
            set
            {
                _progressPercentage = value;
                OnPropertyChanged();
            }
        }

        private string _currentAction = string.Empty;
        public string CurrentAction
        {
            get => _currentAction;
            set
            {
                _currentAction = value;
                OnPropertyChanged();
            }
        }

        private string _logMessages = string.Empty;
        public string LogMessages
        {
            get => _logMessages;
            set
            {
                _logMessages = value;
                OnPropertyChanged();
            }
        }

        public ProgressViewModel()
        {
        }

        public void Reset()
        {
            ProgressPercentage = 0;
            CurrentAction = string.Empty;
            LogMessages = string.Empty;
        }

        public void ResetProgress()
        {
            ProgressPercentage = 0;
            CurrentAction = string.Empty;
        }

        public void AddLog(string message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
                LogMessages += $"[{timestamp}] {message}{Environment.NewLine}";
            });
        }

        public void UpdateProgress(string action, int percentage)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                CurrentAction = action;
                ProgressPercentage = percentage;
            });
        }
    }
}
