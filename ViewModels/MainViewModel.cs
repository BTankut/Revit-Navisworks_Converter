using RvtToNavisConverter.Helpers;
using RvtToNavisConverter.Models;
using RvtToNavisConverter.Services;
using RvtToNavisConverter.Views;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace RvtToNavisConverter.ViewModels
{
    public class MainViewModel : ViewModelBase
    {
        private readonly ISettingsService _settingsService;
        private readonly IRevitServerService _revitServerService;
        private readonly ILocalFileService _localFileService;
        private readonly IFileDownloadService _fileDownloadService;
        private readonly INavisworksConversionService _navisworksConversionService;
        private readonly IFileStatusService _fileStatusService;
        private readonly SettingsViewModel _settingsViewModel;
        private readonly ProgressViewModel _progressViewModel;
        private readonly MonitorViewModel _monitorViewModel;
        private readonly IServiceProvider _serviceProvider;
        private ProgressView? _progressWindow;

        private ObservableCollection<IFileSystemItem> _fileSystemItems = new ObservableCollection<IFileSystemItem>();
        public ObservableCollection<IFileSystemItem> FileSystemItems
        {
            get => _fileSystemItems;
            set { _fileSystemItems = value; OnPropertyChanged(); }
        }

        private string _currentPath = string.Empty;
        public string CurrentPath
        {
            get => _currentPath;
            set { _currentPath = value; OnPropertyChanged(); }
        }

        private string _statusMessage = string.Empty;
        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        private bool _isLoading;
        public bool IsLoading
        {
            get => _isLoading;
            set { _isLoading = value; OnPropertyChanged(); }
        }

        public ICommand ConnectCommand { get; }
        public ICommand BrowseLocalCommand { get; }
        public ICommand GoUpCommand { get; }
        public ICommand NavigateToFolderCommand { get; }
        public ICommand StartProcessingCommand { get; }
        public ICommand OpenSettingsCommand { get; }
        public ICommand OpenMonitorCommand { get; }
        public ICommand CancelCommand { get; }

        private CancellationTokenSource? _cancellationTokenSource;

        public MainViewModel(IServiceProvider serviceProvider, ISettingsService settingsService, IRevitServerService revitServerService, ILocalFileService localFileService, IFileDownloadService fileDownloadService, INavisworksConversionService navisworksConversionService, IFileStatusService fileStatusService, SettingsViewModel settingsViewModel, ProgressViewModel progressViewModel, MonitorViewModel monitorViewModel, PowerShellHelper powerShellHelper)
        {
            _serviceProvider = serviceProvider;
            _settingsService = settingsService;
            _revitServerService = revitServerService;
            _localFileService = localFileService;
            _fileDownloadService = fileDownloadService;
            _navisworksConversionService = navisworksConversionService;
            _fileStatusService = fileStatusService;
            _settingsViewModel = settingsViewModel;
            _progressViewModel = progressViewModel;
            _monitorViewModel = monitorViewModel;

            // Subscribe to the PowerShell log event
            powerShellHelper.CommandLog += _monitorViewModel.AddLogEntry;

            ConnectCommand = new RelayCommand(async _ => await ConnectToServerAsync(), _ => !IsLoading);
            BrowseLocalCommand = new RelayCommand(async _ => await BrowseLocalAsync(), _ => !IsLoading);
            GoUpCommand = new RelayCommand(async _ => await GoUpAsync(), _ => !IsLoading && !string.IsNullOrEmpty(CurrentPath) && IsNotInRoot(CurrentPath));
            NavigateToFolderCommand = new RelayCommand(async item => await NavigateToPathAsync((item as IFileSystemItem)?.Path, FileSystemItems.OfType<FileItem>().Any(f => f.IsLocal)), item => item is IFileSystemItem fsItem && fsItem.IsDirectory && !IsLoading);
            StartProcessingCommand = new RelayCommand(async _ => await ProcessFilesAsync(), _ => FileSystemItems.OfType<FileItem>().Any(f => f.IsSelectedForDownload || f.IsSelectedForConversion) && !IsLoading);
            OpenSettingsCommand = new RelayCommand(_ => OpenSettings(), _ => !IsLoading);
            OpenMonitorCommand = new RelayCommand(_ => OpenMonitor(), _ => !IsLoading);
            CancelCommand = new RelayCommand(_ => CancelOperation(), _ => IsLoading);
        }

        private void OpenSettings()
        {
            var settingsWindow = new SettingsView { DataContext = _settingsViewModel };
            settingsWindow.ShowDialog();
        }

        private void OpenMonitor()
        {
            // Use the service provider to create a new instance of the monitor window
            var monitorWindow = _serviceProvider.GetService<MonitorView>();
            if (monitorWindow != null)
            {
                monitorWindow.DataContext = _monitorViewModel;
                monitorWindow.Show();
            }
        }

        private void CancelOperation()
        {
            _cancellationTokenSource?.Cancel();
        }

        private async Task ConnectToServerAsync()
        {
            var settings = _settingsService.LoadSettings();
            var rootPath = $@"\\{settings.RevitServerIp}\Revit Server 2021\Projects";
            await NavigateToPathAsync(rootPath, isLocal: false);
        }

        private bool IsNotInRoot(string path)
        {
            var settings = _settingsService.LoadSettings();
            var rootPath = $@"\\{settings.RevitServerIp}\Revit Server 2021\Projects";
            return !string.Equals(path.TrimEnd('\\'), rootPath.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase);
        }

        private async Task BrowseLocalAsync()
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            System.Windows.Forms.DialogResult result = dialog.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                await NavigateToPathAsync(dialog.SelectedPath, isLocal: true);
            }
        }

        private async Task NavigateToPathAsync(string? path, bool isLocal)
        {
            if (string.IsNullOrEmpty(path)) return;

            IsLoading = true;
            StatusMessage = $"Loading contents of {path}...";
            _cancellationTokenSource = new CancellationTokenSource();

            try
            {
                IEnumerable<IFileSystemItem> items;
                if (isLocal)
                {
                    items = await Task.Run(() => _localFileService.GetDirectoryContentsAsync(path, _cancellationTokenSource.Token), _cancellationTokenSource.Token);
                }
                else
                {
                    items = await Task.Run(() => _revitServerService.GetDirectoryContentsAsync(path, _cancellationTokenSource.Token), _cancellationTokenSource.Token);
                }

                FileSystemItems = new ObservableCollection<IFileSystemItem>(items);
                CurrentPath = path;
                StatusMessage = $"Currently viewing: {CurrentPath}";
            }
            catch (OperationCanceledException)
            {
                StatusMessage = "Operation cancelled.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error: {ex.Message}";
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;
            }
        }

        private async Task GoUpAsync()
        {
            var parent = Directory.GetParent(CurrentPath);
            if (parent != null)
            {
                // Determine if the current path is local or server
                var isLocal = FileSystemItems.OfType<FileItem>().Any(f => f.IsLocal);
                await NavigateToPathAsync(parent.FullName, isLocal);
            }
        }

        private async Task ProcessFilesAsync()
        {
            var filesToDownload = FileSystemItems.OfType<FileItem>().Where(f => f.IsSelectedForDownload).ToList();
            var filesToConvert = FileSystemItems.OfType<FileItem>().Where(f => f.IsSelectedForConversion).ToList();
            var filesToProcess = filesToDownload.Union(filesToConvert).Distinct().ToList();

            if (!filesToProcess.Any())
            {
                StatusMessage = "No files selected for processing.";
                return;
            }

            IsLoading = true;
            _cancellationTokenSource = new CancellationTokenSource();

            if (_progressWindow == null || !_progressWindow.IsVisible)
            {
                _progressViewModel.Reset();
                _progressWindow = new ProgressView { DataContext = _progressViewModel };
                _progressWindow.Closed += (s, e) => _progressWindow = null;
                _progressWindow.Show();
            }
            else
            {
                _progressViewModel.ResetProgress();
            }


            try
            {
                await Task.Run(async () =>
                {
                    var settings = _settingsService.LoadSettings();
                    var successfullyDownloadedFiles = new List<FileItem>();
                    int totalFiles = filesToProcess.Count;
                    int filesCompleted = 0;

                    // --- Download Phase ---
                    if (filesToDownload.Any())
                    {
                        _progressViewModel.UpdateProgress("Starting download phase...", 0);
                        foreach (var file in filesToDownload)
                        {
                            if (_cancellationTokenSource.Token.IsCancellationRequested) break;
                            UpdateFileStatus(file, "Downloading...");
                            var success = await _fileDownloadService.DownloadFileAsync(file, settings);
                            if (success)
                            {
                                UpdateFileStatus(file, "Downloaded");
                                _progressViewModel.AddLog($"{file.Name} - Downloaded");
                                successfullyDownloadedFiles.Add(file);
                            }
                            else
                            {
                                UpdateFileStatus(file, "Error during download");
                                _progressViewModel.AddLog($"{file.Name} - Download Error");
                            }
                            filesCompleted++;
                            var percentage = (int)((double)filesCompleted / totalFiles * 100);
                            _progressViewModel.UpdateProgress($"Downloading... ({filesCompleted}/{totalFiles})", percentage);
                        }
                    }

                    if (_cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        _progressViewModel.AddLog("Operation cancelled by user.");
                        return;
                    }

                    // --- Conversion Phase ---
                    var finalConversionList = filesToConvert
                        .Where(f => !f.IsSelectedForDownload || successfullyDownloadedFiles.Contains(f))
                        .ToList();

                    if (finalConversionList.Any())
                    {
                        _progressViewModel.UpdateProgress($"Converting... (0/{finalConversionList.Count})", 50);
                        var conversionTask = new ConversionTask
                        {
                            FilesToProcess = finalConversionList,
                            OutputNwdFile = $"Consolidated_{DateTime.Now:yyyyMMddHHmmss}.nwd",
                            LogFilePath = Path.Combine(settings.DefaultNwdPath, "conversion.log"),
                            OverwriteExisting = true
                        };
                        var success = await _navisworksConversionService.ConvertFilesAsync(conversionTask, settings);
                        var status = success ? "Completed" : "Error";
                        var finalPercentage = 100;
                        foreach (var file in finalConversionList)
                        {
                            if (_cancellationTokenSource.Token.IsCancellationRequested) break;
                            UpdateFileStatus(file, $"Conversion {status}");
                             _progressViewModel.AddLog($"{file.Name} - Conversion {status}");
                        }
                         _progressViewModel.UpdateProgress("All processing complete.", finalPercentage);
                    }
                    else
                    {
                        _progressViewModel.UpdateProgress("All processing complete.", 100);
                    }
                    StatusMessage = "All processing finished.";

                }, _cancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                _progressViewModel.AddLog("Operation was cancelled.");
                StatusMessage = "Processing cancelled.";
            }
            catch (Exception ex)
            {
                _progressViewModel.AddLog($"FATAL ERROR: {ex.Message}");
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
                _cancellationTokenSource.Dispose();
                _cancellationTokenSource = null;
                // progressWindow.Close(); // Optional: close window when done
            }
        }

        private void UpdateFileStatus(FileItem file, string status)
        {
            Application.Current.Dispatcher.Invoke(() => file.Status = status);
            _fileStatusService.SetStatus(file.Path, status);
        }
    }
}
