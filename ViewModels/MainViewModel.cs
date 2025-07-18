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
        private readonly IRevitFileVersionService _revitFileVersionService;
        private readonly SettingsViewModel _settingsViewModel;
        private readonly ProgressViewModel _progressViewModel;
        private readonly MonitorViewModel _monitorViewModel;
        private readonly IServiceProvider _serviceProvider;
        private readonly SelectionManager _selectionManager;
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

        private int _trialDaysRemaining;
        public int TrialDaysRemaining
        {
            get => _trialDaysRemaining;
            set 
            { 
                _trialDaysRemaining = value; 
                OnPropertyChanged(); 
                OnPropertyChanged(nameof(TrialBackground)); 
                OnPropertyChanged(nameof(TrialDisplayText));
            }
        }

        private string _hardwareId = string.Empty;
        public string HardwareId
        {
            get => _hardwareId;
            set 
            { 
                _hardwareId = value; 
                OnPropertyChanged(); 
            }
        }

        public System.Windows.Media.Brush TrialBackground
        {
            get
            {
#if DEBUG
                return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(33, 150, 243)); // Blue for debug
#else
                if (TrialDaysRemaining <= 3)
                    return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(244, 67, 54)); // Red
                else if (TrialDaysRemaining <= 7)
                    return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(255, 193, 7)); // Amber
                else
                    return new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(76, 175, 80)); // Green
#endif
            }
        }

        public string TrialDisplayText
        {
            get
            {
#if DEBUG
                return "DEBUG MODE - No License Check";
#else
                return $"Trial Version - {TrialDaysRemaining} days remaining";
#endif
            }
        }

        public ICommand ConnectCommand { get; }
        public ICommand BrowseLocalCommand { get; }
        public ICommand GoUpCommand { get; }
        public ICommand NavigateToFolderCommand { get; }
        public ICommand StartProcessingCommand { get; }
        public ICommand OpenSettingsCommand { get; }
        public ICommand OpenMonitorCommand { get; }
        public ICommand OpenAboutCommand { get; }
        public ICommand CancelCommand { get; }
        public ICommand ClearAllSelectionsCommand { get; }
        public ICommand ToggleDownloadCommand { get; }
        public ICommand ToggleConversionCommand { get; }

        private CancellationTokenSource? _cancellationTokenSource;
        // Removed _selectedItems - all selections are now managed through SelectionManager

        public MainViewModel(IServiceProvider serviceProvider, ISettingsService settingsService, IRevitServerService revitServerService, ILocalFileService localFileService, IFileDownloadService fileDownloadService, INavisworksConversionService navisworksConversionService, IFileStatusService fileStatusService, IRevitFileVersionService revitFileVersionService, IHardwareIdService hardwareIdService, SettingsViewModel settingsViewModel, ProgressViewModel progressViewModel, MonitorViewModel monitorViewModel, PowerShellHelper powerShellHelper, SelectionManager selectionManager)
        {
            _serviceProvider = serviceProvider;
            _settingsService = settingsService;
            _revitServerService = revitServerService;
            _localFileService = localFileService;
            _fileDownloadService = fileDownloadService;
            _navisworksConversionService = navisworksConversionService;
            _fileStatusService = fileStatusService;
            _revitFileVersionService = revitFileVersionService;
            _settingsViewModel = settingsViewModel;
            _progressViewModel = progressViewModel;
            _monitorViewModel = monitorViewModel;
            
            _selectionManager = selectionManager;
            _selectionManager.SelectionChanged += SelectionManager_SelectionChanged;

            // Subscribe to the PowerShell log event
            powerShellHelper.CommandLog += _monitorViewModel.AddLogEntry;
            
            // Initialize trial days from license validation
            var licenseValidation = Application.Current.Properties["LicenseValidation"] as LicenseValidationResult;
            if (licenseValidation != null)
            {
                TrialDaysRemaining = licenseValidation.DaysRemaining;
            }
            
            // Initialize Hardware ID
            HardwareId = hardwareIdService.GetHardwareId();

            ConnectCommand = new RelayCommand(async _ => await ConnectToServerAsync(), _ => !IsLoading);
            BrowseLocalCommand = new RelayCommand(async _ => await BrowseLocalAsync(), _ => !IsLoading);
            GoUpCommand = new RelayCommand(async _ => await GoUpAsync(), _ => !IsLoading && !string.IsNullOrEmpty(CurrentPath) && IsNotInRoot(CurrentPath));
NavigateToFolderCommand = new RelayCommand(
    async (object? item) => 
    {
        var fileSystemItem = item as IFileSystemItem;
        if (fileSystemItem != null && fileSystemItem.IsDirectory)
        {
            await NavigateToPathAsync(fileSystemItem.Path, FileSystemItems.OfType<FileItem>().Any(f => f.IsLocal));
        }
    }, 
    item => item is IFileSystemItem fsItem && fsItem.IsDirectory && !IsLoading
);
StartProcessingCommand = new RelayCommand(
    async _ => 
    {
        FileLogger.Log("StartProcessingCommand.Execute called - attempting to start processing");
        await ProcessFilesAsync();
    }, 
    _ => 
    {
        var selections = _selectionManager.GetAllSelections();
        var hasDownloadSelection = selections.Any(kvp => kvp.Value.IsSelectedForDownload == true);
        var hasConversionSelection = selections.Any(kvp => kvp.Value.IsSelectedForConversion == true);
        var isNotLoading = !IsLoading;
        
        FileLogger.Log($"StartProcessingCommand.CanExecute: Total selections={selections.Count}, " +
                      $"HasDownload={hasDownloadSelection}, HasConversion={hasConversionSelection}, " +
                      $"IsLoading={IsLoading}, Result={((hasDownloadSelection || hasConversionSelection) && isNotLoading)}");
        
        return (hasDownloadSelection || hasConversionSelection) && isNotLoading;
    }
);
            OpenSettingsCommand = new RelayCommand(_ => OpenSettings(), _ => !IsLoading);
            OpenMonitorCommand = new RelayCommand(_ => OpenMonitor(), _ => !IsLoading);
            OpenAboutCommand = new RelayCommand(_ => OpenAbout(), _ => !IsLoading);
            CancelCommand = new RelayCommand(_ => CancelOperation(), _ => IsLoading);
            ClearAllSelectionsCommand = new RelayCommand(_ => ClearAllSelections(), _ => !IsLoading);
ToggleDownloadCommand = new RelayCommand(
    item => 
    {
        var fileSystemItem = item as IFileSystemItem;
        if (fileSystemItem != null)
        {
            ToggleSelection(fileSystemItem, true);
        }
    }, 
    _ => !IsLoading);
            ToggleConversionCommand = new RelayCommand(
    item => 
    {
        var fileSystemItem = item as IFileSystemItem;
        if (fileSystemItem != null)
        {
            ToggleSelection(fileSystemItem, false);
        }
    }, 
    _ => !IsLoading);
        }

        private void SelectionManager_SelectionChanged(object? sender, SelectionChangedEventArgs e)
        {
            FileLogger.Log($"SelectionManager_SelectionChanged: Path={e.Path}, IsDownload={e.IsDownload}, Value={e.Value}, IsMarker={e.IsMarker}");
            
            // When selection changes in SelectionManager, update UI
            if (e.Path == "*") // Special case for ClearAllSelections
            {
                foreach (var fileSystemItem in FileSystemItems)
                {
                    if (e.IsDownload)
                        fileSystemItem.IsSelectedForDownload = false;
                    else
                        fileSystemItem.IsSelectedForConversion = false;
                }
                return;
            }

            // Eğer bu bir marker ise (klasör işaretleyicisi), UI'da gösterilmez ama alt öğeleri güncelle
            if (e.IsMarker)
            {
                FileLogger.Log($"Marker değişikliği: {e.Path}, IsDownload: {e.IsDownload}, Value: {e.Value}");
                
                // Marker'ın klasör yolunu al
                string folderPath = e.Path.Substring(0, e.Path.Length - 2); // "\*" kısmını çıkar
                
                FileLogger.Log($"  Marker klasör yolu: {folderPath}");
                FileLogger.Log($"  Mevcut UI öğe sayısı: {FileSystemItems.Count}");
                
                // Bu klasörün altındaki tüm UI öğelerini güncelle
                foreach (var uiItem in FileSystemItems)
                {
                    bool shouldUpdate = false;
                    
                    // Klasörün kendisi mi?
                    if (uiItem.Path.Equals(folderPath, StringComparison.OrdinalIgnoreCase))
                    {
                        shouldUpdate = true;
                        FileLogger.Log($"  Klasörün kendisi bulundu: {uiItem.Path}");
                    }
                    // Alt öğesi mi? (klasör yolu ile başlıyor ve daha uzun)
                    else if (uiItem.Path.StartsWith(folderPath, StringComparison.OrdinalIgnoreCase))
                    {
                        // Tam olarak alt öğe olduğundan emin ol (sadece prefix değil)
                        string remainingPath = uiItem.Path.Substring(folderPath.Length);
                        if (remainingPath.StartsWith("\\") || remainingPath.StartsWith("/"))
                        {
                            shouldUpdate = true;
                            FileLogger.Log($"  Alt öğe bulundu: {uiItem.Path}");
                        }
                    }
                    
                    if (shouldUpdate)
                    {
                        FileLogger.Log($"  Marker nedeniyle UI öğesi güncelleniyor: {uiItem.Path}, Value: {e.Value}");
                        
                        if (e.IsDownload)
                        {
                            uiItem.IsSelectedForDownload = e.Value;
                            
                            // Tri-state durumunu da güncelle
                            if (uiItem is FolderItem folder)
                            {
                                folder.IsPartiallySelectedForDownload = false;
                            }
                        }
                        else
                        {
                            uiItem.IsSelectedForConversion = e.Value;
                            
                            // Tri-state durumunu da güncelle
                            if (uiItem is FolderItem folder)
                            {
                                folder.IsPartiallySelectedForConversion = false;
                            }
                        }
                    }
                }
                return;
            }

            var item = FileSystemItems.FirstOrDefault(i => i.Path.Equals(e.Path, StringComparison.OrdinalIgnoreCase));
            if (item != null)
            {
                FileLogger.Log($"UI güncelleniyor: {e.Path}, IsDownload: {e.IsDownload}, Value: {e.Value}");
                
                // UI'daki değeri güncelle
                if (e.IsDownload)
                {
                    item.IsSelectedForDownload = e.Value;
                    
                    // Eğer bu bir klasör ise ve tri-state özellikler varsa güncelle
                    if (item is FolderItem folder)
                    {
                        folder.IsPartiallySelectedForDownload = e.Value == null;
                    }
                }
                else
                {
                    item.IsSelectedForConversion = e.Value;
                    
                    // Eğer bu bir klasör ise ve tri-state özellikler varsa güncelle
                    if (item is FolderItem folder)
                    {
                        folder.IsPartiallySelectedForConversion = e.Value == null;
                    }
                }
            }
            else
            {
                FileLogger.Log($"UI öğesi bulunamadı (bu normal, üst klasör olabilir): {e.Path}");
            }
            
            // Update command states when selection changes
            FileLogger.Log("Calling CommandManager.InvalidateRequerySuggested() to update command states");
            CommandManager.InvalidateRequerySuggested();
        }

        public void HandleSelectionChange(IFileSystemItem item, bool isDownload, bool? newValue)
        {
            if (item == null) return;
            
            FileLogger.Log($"HandleSelectionChange called: Path={item.Path}, IsDirectory={item.IsDirectory}, " +
                          $"IsDownload={isDownload}, NewValue={newValue}");

            if (item.IsDirectory)
            {
                // Klasör için recursive işlem yap
                if (newValue.HasValue)
                {
                    _selectionManager.SelectFolderRecursively(item.Path, isDownload, newValue.Value);
                }
                else
                {
                    // Null değer için klasörü indeterminate duruma getir
                    _selectionManager.SetSelection(item.Path, isDownload, null);
                }
            }
            else
            {
                // Dosya için sadece kendi seçimini ayarla
                _selectionManager.SetSelection(item.Path, isDownload, newValue);
            }
            
            // Her durumda üst klasörlerin durumunu güncelle - mevcut UI öğelerini de geç
            _selectionManager.UpdateParentStates(item.Path, isDownload, FileSystemItems);
        }

private void ToggleSelection(IFileSystemItem item, bool isDownload)
        {
            if (item == null) return;

            bool? currentValue = isDownload ? item.IsSelectedForDownload : item.IsSelectedForConversion;
            bool? newValue;

            // Implement three-state toggle: null -> false -> true -> null
            if (currentValue == null)
                newValue = false;
            else if (currentValue == false)
                newValue = true;
            else
                newValue = null;

            HandleSelectionChange(item, isDownload, newValue);
        }

        private void ClearAllSelections()
        {
            _selectionManager.ClearAllSelections();
        }

        private void OpenSettings()
        {
            _settingsViewModel.RefreshValidation();
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

        private void OpenAbout()
        {
            var aboutDialog = new AboutDialog { DataContext = this, Owner = Application.Current.MainWindow };
            aboutDialog.ShowDialog();
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

            // No need to save selections - SelectionManager handles all selections

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

            // Mevcut klasörün içeriğini SelectionManager'a kaydet (hem dosyalar hem klasörler)
            var currentFolderItems = items.Select(i => i.Path).ToList();
            if (currentFolderItems.Any())
            {
                _selectionManager.SetFolderContents(path, currentFolderItems);
                var fileCount = items.Count(i => !i.IsDirectory);
                var folderCount = items.Count(i => i.IsDirectory);
                FileLogger.Log($"Klasör içeriği kaydedildi: {path} - {fileCount} dosya, {folderCount} klasör");
            }
            
            // Restore selections only if there are any saved selections
            if (_selectionManager.GetAllSelections().Count > 0)
            {
                RestoreSelections(items);
            }
            
            // Read Revit version for .rvt files
            var revitFiles = items.OfType<FileItem>().Where(f => f.Name.EndsWith(".rvt", StringComparison.OrdinalIgnoreCase));
            foreach (var file in revitFiles)
            {
                // For server files, try to extract version from path first
                if (!isLocal && !string.IsNullOrEmpty(file.Path))
                {
                    var versionFromPath = _revitFileVersionService.GetRevitVersionFromServerPath(file.Path);
                    if (!string.IsNullOrEmpty(versionFromPath))
                    {
                        file.RevitVersion = versionFromPath;
                        FileLogger.Log($"Detected version {versionFromPath} from server path for file: {file.Name}");
                        continue;
                    }
                }
                
                // For local files or if server path extraction failed, read from file content
                if (isLocal)
                {
                    _ = Task.Run(async () =>
                    {
                        var version = await _revitFileVersionService.GetRevitFileVersionAsync(file.Path);
                        if (!string.IsNullOrEmpty(version))
                        {
                            Application.Current.Dispatcher.Invoke(() => file.RevitVersion = version);
                        }
                    });
                }
            }
            
            // Tüm öğelerin üst klasör durumlarını güncelle
            foreach (var item in items)
            {
                if (item.IsDirectory)
                {
                    FileLogger.Log($"Klasör durumu kontrol ediliyor: {item.Path}");
                    
                    // Bu klasörün tüm alt öğelerini bul
                    // Önce SelectionManager'dan kayıtlı olanları al (seçili olanlar)
                    var selectedChildren = _selectionManager.GetAllSelections().Keys
                        .Where(k => !k.EndsWith("\\*") && 
                               Path.GetDirectoryName(k)?.Equals(item.Path, StringComparison.OrdinalIgnoreCase) == true)
                        .ToList();
                    
                    // Eğer bu klasör için folder marker varsa, o klasörün gerçek dosya sayısını al
                    var folderMarker = item.Path.TrimEnd('\\') + "\\*";
                    var allSelections = _selectionManager.GetAllSelections();
                    
                    var children = new List<string>();
                    
                    // Öncelikle GetChildren kullanarak gerçek dosya sayısını al
                    children = _selectionManager.GetChildren(item.Path);
                    
                    if (!children.Any())
                    {
                        // GetChildren boşsa, seçili olanları kullan
                        children = selectedChildren;
                    }
                    
                    FileLogger.Log($"  GetChildren sonucu: {children.Count} adet - {string.Join(", ", children.Take(3))}{(children.Count > 3 ? "..." : "")}");
                    
                    // Eğer hiç child yoksa, bir önceki seçim işleminden kalan dosyaları kontrol et
                    if (!children.Any())
                    {
                        // Son çare: tüm kayıtlı dosyaları kontrol et
                        children = allSelections.Keys
                            .Where(k => !k.EndsWith("\\*") && 
                                   k.StartsWith(item.Path, StringComparison.OrdinalIgnoreCase) &&
                                   Path.GetDirectoryName(k)?.Equals(item.Path, StringComparison.OrdinalIgnoreCase) == true)
                            .ToList();
                    }
                    
                    if (children.Any())
                    {
                        FileLogger.Log($"  Alt öğeleri var: {children.Count} adet");
                        
                        // Download durumunu kontrol et
                        var downloadStates = children.Select(c => _selectionManager.GetSelection(c, true)).ToList();
                        var downloadSelected = downloadStates.Count(s => s == true);
                        var downloadUnselected = downloadStates.Count(s => s == false || s == null);
                        
                        if (downloadSelected > 0 && downloadUnselected > 0)
                        {
                            FileLogger.Log($"  Download için indeterminate: seçili={downloadSelected}, seçili değil={downloadUnselected}");
                            item.IsSelectedForDownload = null;
                        }
                        else if (downloadSelected == children.Count)
                        {
                            FileLogger.Log($"  Download için tümü seçili");
                            item.IsSelectedForDownload = true;
                        }
                        else
                        {
                            FileLogger.Log($"  Download için hiçbiri seçili değil");
                            item.IsSelectedForDownload = false;
                        }
                        
                        // Conversion durumunu kontrol et
                        var conversionStates = children.Select(c => _selectionManager.GetSelection(c, false)).ToList();
                        var conversionSelected = conversionStates.Count(s => s == true);
                        var conversionUnselected = conversionStates.Count(s => s == false || s == null);
                        
                        if (conversionSelected > 0 && conversionUnselected > 0)
                        {
                            FileLogger.Log($"  Conversion için indeterminate: seçili={conversionSelected}, seçili değil={conversionUnselected}");
                            item.IsSelectedForConversion = null;
                        }
                        else if (conversionSelected == children.Count)
                        {
                            FileLogger.Log($"  Conversion için tümü seçili");
                            item.IsSelectedForConversion = true;
                        }
                        else
                        {
                            FileLogger.Log($"  Conversion için hiçbiri seçili değil");
                            item.IsSelectedForConversion = false;
                        }
                    }
                }
            }

                FileSystemItems = new ObservableCollection<IFileSystemItem>(items);
                CurrentPath = path;
                StatusMessage = "Ready";
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

        // SaveCurrentSelections method removed - all selections managed through SelectionManager

        private void RestoreSelections(IEnumerable<IFileSystemItem> items)
        {
            FileLogger.Log($"RestoreSelections başlıyor, öğe sayısı: {items.Count()}");
            
            // Önce SelectionManager'dan seçim durumlarını al
            foreach (var item in items)
            {
                // SelectionManager'dan seçim durumunu kontrol et
                var downloadSelection = _selectionManager.GetSelection(item.Path, true);
                var conversionSelection = _selectionManager.GetSelection(item.Path, false);
                
                FileLogger.Log($"  Öğe: {item.Path}, Download: {downloadSelection}, Conversion: {conversionSelection}");
                
                if (downloadSelection.HasValue)
                {
                    item.IsSelectedForDownload = downloadSelection;
                    
                    // Tri-state durumunu güncelle
                    if (item is FolderItem folder)
                    {
                        folder.IsPartiallySelectedForDownload = downloadSelection == null;
                    }
                }
                
                if (conversionSelection.HasValue)
                {
                    item.IsSelectedForConversion = conversionSelection;
                    
                    // Tri-state durumunu güncelle
                    if (item is FolderItem folder)
                    {
                        folder.IsPartiallySelectedForConversion = conversionSelection == null;
                    }
                }
            }
            
            FileLogger.Log("RestoreSelections tamamlandı");
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
            try
            {
                FileLogger.Log("=== ProcessFilesAsync START ===");
            
            var filesToDownload = new List<FileItem>();
            var filesToConvert = new List<FileItem>();

            // Get all selections from SelectionManager
            var allSelections = _selectionManager.GetAllSelections();
            FileLogger.Log($"Total selections: {allSelections.Count}");
            
            foreach (var kvp in allSelections)
            {
                FileLogger.Log($"  {kvp.Key}: Download={kvp.Value.IsSelectedForDownload}, Conversion={kvp.Value.IsSelectedForConversion}");
            }
            
            // Process folder markers first (these represent fully selected folders)
            var folderMarkers = allSelections.Where(kvp => kvp.Key.EndsWith("\\*")).ToList();
            FileLogger.Log($"Found {folderMarkers.Count} folder markers");
            
            foreach (var marker in folderMarkers)
            {
                var folderPath = marker.Key.Substring(0, marker.Key.Length - 2); // Remove "\*"
                var selectionState = marker.Value;
                
                FileLogger.Log($"Processing folder marker: {folderPath}, Download={selectionState.IsSelectedForDownload}, Conversion={selectionState.IsSelectedForConversion}");
                
                // Determine if this is a local or server path
                var isLocal = !folderPath.StartsWith("\\\\");
                
                // When a folder marker exists, we need to check individual file overrides
                if (selectionState.IsSelectedForDownload == true)
                {
                    var allFiles = await GetAllFilesRecursive(folderPath, isLocal);
                    FileLogger.Log($"  Folder marker indicates download, checking {allFiles.Count} files for individual overrides");
                    
                    // Check each file to see if it has been individually deselected
                    foreach (var file in allFiles)
                    {
                        var fileSelection = _selectionManager.GetSelection(file.Path, true);
                        FileLogger.Log($"    File: {file.Path}, Individual selection: {fileSelection}");
                        
                        // If file has explicit false selection, skip it
                        if (fileSelection == false)
                        {
                            FileLogger.Log($"      -> File explicitly deselected, skipping");
                            continue;
                        }
                        
                        // Otherwise, include it (null or true means it inherits from folder)
                        FileLogger.Log($"      -> Including file for download");
                        filesToDownload.Add(file);
                    }
                }
                
                if (selectionState.IsSelectedForConversion == true)
                {
                    var allFiles = await GetAllFilesRecursive(folderPath, isLocal);
                    FileLogger.Log($"  Folder marker indicates conversion, checking {allFiles.Count} files for individual overrides");
                    
                    // Check each file to see if it has been individually deselected
                    foreach (var file in allFiles)
                    {
                        var fileSelection = _selectionManager.GetSelection(file.Path, false);
                        FileLogger.Log($"    File: {file.Path}, Individual selection: {fileSelection}");
                        
                        // If file has explicit false selection, skip it
                        if (fileSelection == false)
                        {
                            FileLogger.Log($"      -> File explicitly deselected, skipping");
                            continue;
                        }
                        
                        // Otherwise, include it (null or true means it inherits from folder)
                        FileLogger.Log($"      -> Including file for conversion");
                        filesToConvert.Add(file);
                    }
                }
            }
            
            // Process individual file selections (not folder markers)
            var individualSelections = allSelections.Where(kvp => !kvp.Key.EndsWith("\\*")).ToList();
            FileLogger.Log($"Processing {individualSelections.Count} individual selections");
            
            foreach (var kvp in individualSelections)
            {
                var path = kvp.Key;
                var selectionState = kvp.Value;
                
                FileLogger.Log($"  Checking path: {path}, Download={selectionState.IsSelectedForDownload}, Conversion={selectionState.IsSelectedForConversion}");
                
                // Skip if neither download nor conversion is selected
                if (selectionState.IsSelectedForDownload != true && selectionState.IsSelectedForConversion != true)
                {
                    FileLogger.Log($"    -> Skipping, not selected");
                    continue;
                }
                
                // Check if this is a file
                if (path.EndsWith(".rvt", StringComparison.OrdinalIgnoreCase))
                {
                    FileLogger.Log($"    -> Processing as file");
                    
                    // Check if this file is already covered by a folder marker
                    bool coveredByFolderMarker = false;
                    foreach (var marker in folderMarkers)
                    {
                        var folderPath = marker.Key.Substring(0, marker.Key.Length - 2);
                        if (path.StartsWith(folderPath, StringComparison.OrdinalIgnoreCase))
                        {
                            coveredByFolderMarker = true;
                            FileLogger.Log($"    -> File already covered by folder marker: {marker.Key}");
                            break;
                        }
                    }
                    
                    if (!coveredByFolderMarker)
                    {
                        // Determine if this is a local or server file
                        var isLocal = !path.StartsWith("\\\\");
                        
                        // Create FileItem for this file
                        var fileItem = new FileItem 
                        { 
                            Path = path, 
                            Name = Path.GetFileName(path), 
                            IsLocal = isLocal 
                        };
                        
                        if (selectionState.IsSelectedForDownload == true)
                        {
                            FileLogger.Log($"    -> Adding to download list");
                            filesToDownload.Add(fileItem);
                        }
                        
                        if (selectionState.IsSelectedForConversion == true)
                        {
                            FileLogger.Log($"    -> Adding to conversion list");
                            filesToConvert.Add(fileItem);
                        }
                    }
                }
                else
                {
                    // This might be a folder with indeterminate state
                    FileLogger.Log($"    -> Checking if this is a folder with partial selection");
                    
                    // Try to get directory contents to see if it's a folder
                    var isLocal = !path.StartsWith("\\\\");
                    try
                    {
                        var items = isLocal 
                            ? await _localFileService.GetDirectoryContentsAsync(path, CancellationToken.None)
                            : await _revitServerService.GetDirectoryContentsAsync(path, CancellationToken.None);
                        
                        if (items != null && items.Any())
                        {
                            FileLogger.Log($"    -> This is a folder with {items.Count()} items");
                            
                            // This is a folder - check for partial selection
                            if (selectionState.IsSelectedForDownload == null)
                            {
                                // Indeterminate - get only selected files
                                var files = await GetSelectedFilesRecursive(path, isLocal, true, false);
                                FileLogger.Log($"    -> Folder partially selected for download, found {files.Count} selected files");
                                filesToDownload.AddRange(files);
                            }
                            
                            if (selectionState.IsSelectedForConversion == null)
                            {
                                // Indeterminate - get only selected files
                                var files = await GetSelectedFilesRecursive(path, isLocal, false, true);
                                FileLogger.Log($"    -> Folder partially selected for conversion, found {files.Count} selected files");
                                filesToConvert.AddRange(files);
                            }
                        }
                    }
                    catch
                    {
                        FileLogger.Log($"    -> Not a valid folder path");
                    }
                }
            }

            var filesToProcess = filesToDownload.Union(filesToConvert).Distinct().ToList();
            
            FileLogger.Log($"=== Final file counts ===");
            FileLogger.Log($"Files to download: {filesToDownload.Count}");
            FileLogger.Log($"Files to convert: {filesToConvert.Count}");
            FileLogger.Log($"Total unique files to process: {filesToProcess.Count}");

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
                _progressWindow.Closed += (s, e) => { _progressWindow = null; };
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
                    
                    // Version compatibility check
                    var incompatibleFiles = new List<string>();
                    foreach (var file in filesToProcess)
                    {
                        if (!string.IsNullOrEmpty(file.RevitVersion) && !string.IsNullOrEmpty(settings.SelectedRevitServerToolVersion))
                        {
                            if (!_revitFileVersionService.IsVersionCompatible(file.RevitVersion, settings.SelectedRevitServerToolVersion))
                            {
                                incompatibleFiles.Add($"{file.Name} (File: {file.RevitVersion}, Tool: {settings.SelectedRevitServerToolVersion})");
                            }
                        }
                    }
                    
                    if (incompatibleFiles.Any())
                    {
                        var message = $"Version compatibility warning:\n\n" +
                                     $"The following files have different versions than the selected Revit Server Tool:\n\n" +
                                     string.Join("\n", incompatibleFiles.Take(10)) +
                                     (incompatibleFiles.Count > 10 ? $"\n... and {incompatibleFiles.Count - 10} more files" : "") +
                                     "\n\nDo you want to continue anyway?";
                        
                        var result = Application.Current.Dispatcher.Invoke(() => 
                            MessageBox.Show(message, "Version Compatibility Warning", MessageBoxButton.YesNo, MessageBoxImage.Warning));
                        
                        if (result != MessageBoxResult.Yes)
                        {
                            _progressViewModel.AddLog("Process cancelled due to version incompatibility");
                            return;
                        }
                    }
                    
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
                                
                                // Read version info after download
                                var localPath = System.IO.Path.Combine(settings.DefaultDownloadPath, file.Name);
                                if (System.IO.File.Exists(localPath))
                                {
                                    var version = await _revitFileVersionService.GetRevitFileVersionAsync(localPath);
                                    if (!string.IsNullOrEmpty(version))
                                    {
                                        Application.Current.Dispatcher.Invoke(() => file.RevitVersion = version);
                                        FileLogger.Log($"Read version {version} for downloaded file: {file.Name}");
                                    }
                                }
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
                        .Where(f => !(f.IsSelectedForDownload == true) || successfullyDownloadedFiles.Contains(f))
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
                if (_progressViewModel != null)
                {
                    _progressViewModel.AddLog("Operation was cancelled.");
                }
                StatusMessage = "Processing cancelled.";
            }
            catch (Exception ex)
            {
                if (_progressViewModel != null)
                {
                    _progressViewModel.AddLog($"FATAL ERROR: {ex.Message}");
                }
                MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                IsLoading = false;
                if (_cancellationTokenSource != null)
                {
                    _cancellationTokenSource.Dispose();
                    _cancellationTokenSource = null;
                }
                // progressWindow.Close(); // Optional: close window when done
            }
            }
            catch (Exception ex)
            {
                FileLogger.Log($"CRITICAL ERROR in ProcessFilesAsync: {ex.GetType().Name}: {ex.Message}");
                FileLogger.Log($"Stack trace: {ex.StackTrace}");
                MessageBox.Show($"An error occurred while starting processing:\n\n{ex.Message}", 
                               "Processing Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateFileStatus(FileItem file, string status)
        {
            Application.Current.Dispatcher.Invoke(() => file.Status = status);
            _fileStatusService.SetStatus(file.Path, status);
        }

        private async Task<List<FileItem>> GetAllFilesRecursive(string path, bool isLocal)
        {
            var allFiles = new List<FileItem>();
            var items = isLocal 
                ? await _localFileService.GetDirectoryContentsAsync(path, CancellationToken.None)
                : await _revitServerService.GetDirectoryContentsAsync(path, CancellationToken.None);

            foreach (var item in items)
            {
                if (item is FileItem file)
                {
                    allFiles.Add(file);
                }
                else if (item is FolderItem folder)
                {
                    allFiles.AddRange(await GetAllFilesRecursive(folder.Path, isLocal));
                }
            }
            return allFiles;
        }

        private async Task<List<FileItem>> GetSelectedFilesRecursive(string path, bool isLocal, bool checkDownload, bool checkConversion)
        {
            FileLogger.Log($"GetSelectedFilesRecursive: path={path}, isLocal={isLocal}, checkDownload={checkDownload}, checkConversion={checkConversion}");
            
            var selectedFiles = new List<FileItem>();
            var items = isLocal 
                ? await _localFileService.GetDirectoryContentsAsync(path, CancellationToken.None)
                : await _revitServerService.GetDirectoryContentsAsync(path, CancellationToken.None);

            foreach (var item in items)
            {
                if (item is FileItem file)
                {
                    bool shouldInclude = false;
                    var downloadSelection = _selectionManager.GetSelection(file.Path, true);
                    var conversionSelection = _selectionManager.GetSelection(file.Path, false);
                    
                    FileLogger.Log($"  File: {file.Path}, downloadSelection={downloadSelection}, conversionSelection={conversionSelection}");
                    
                    if (checkDownload && downloadSelection == true)
                        shouldInclude = true;
                    if (checkConversion && conversionSelection == true)
                        shouldInclude = true;
                    
                    if (shouldInclude)
                    {
                        FileLogger.Log($"    -> Including file: {file.Path}");
                        selectedFiles.Add(file);
                    }
                }
                else if (item is FolderItem folder)
                {
                    selectedFiles.AddRange(await GetSelectedFilesRecursive(folder.Path, isLocal, checkDownload, checkConversion));
                }
            }
            
            FileLogger.Log($"GetSelectedFilesRecursive returning {selectedFiles.Count} files");
            return selectedFiles;
        }
    }
}
