using RvtToNavisConverter.Helpers;
using RvtToNavisConverter.Models;
using RvtToNavisConverter.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;
using System.Threading.Tasks;

namespace RvtToNavisConverter.ViewModels
{
    public class SelectionSummaryViewModel : ViewModelBase
    {
        private readonly SelectionManager _selectionManager;
        private readonly ILocalFileService _localFileService;
        private readonly IRevitServerService _revitServerService;

        private ObservableCollection<FileItem> _downloadFiles;
        public ObservableCollection<FileItem> DownloadFiles
        {
            get => _downloadFiles;
            set
            {
                _downloadFiles = value;
                OnPropertyChanged();
                UpdateCounts();
            }
        }

        private ObservableCollection<FileItem> _convertFiles;
        public ObservableCollection<FileItem> ConvertFiles
        {
            get => _convertFiles;
            set
            {
                _convertFiles = value;
                OnPropertyChanged();
                UpdateCounts();
            }
        }

        private int _downloadCount;
        public int DownloadCount
        {
            get => _downloadCount;
            set
            {
                _downloadCount = value;
                OnPropertyChanged();
            }
        }

        private int _convertCount;
        public int ConvertCount
        {
            get => _convertCount;
            set
            {
                _convertCount = value;
                OnPropertyChanged();
            }
        }

        private int _totalCount;
        public int TotalCount
        {
            get => _totalCount;
            set
            {
                _totalCount = value;
                OnPropertyChanged();
            }
        }

        private string _downloadSizeText = "0 MB";
        public string DownloadSizeText
        {
            get => _downloadSizeText;
            set
            {
                _downloadSizeText = value;
                OnPropertyChanged();
            }
        }

        private string _convertSizeText = "0 MB";
        public string ConvertSizeText
        {
            get => _convertSizeText;
            set
            {
                _convertSizeText = value;
                OnPropertyChanged();
            }
        }

        private string _totalSizeText = "0 MB";
        public string TotalSizeText
        {
            get => _totalSizeText;
            set
            {
                _totalSizeText = value;
                OnPropertyChanged();
            }
        }

        public ICommand RefreshCommand { get; }
        public ICommand ClearAllCommand { get; }
        public ICommand RemoveDownloadCommand { get; }
        public ICommand RemoveConvertCommand { get; }

        public SelectionSummaryViewModel(
            SelectionManager selectionManager,
            ILocalFileService localFileService,
            IRevitServerService revitServerService)
        {
            _selectionManager = selectionManager;
            _localFileService = localFileService;
            _revitServerService = revitServerService;

            _downloadFiles = new ObservableCollection<FileItem>();
            _convertFiles = new ObservableCollection<FileItem>();

            RefreshCommand = new RelayCommand(async _ => await RefreshSelectionsAsync());
            ClearAllCommand = new RelayCommand(_ => ClearAllSelections());
            RemoveDownloadCommand = new RelayCommand(RemoveFromDownload);
            RemoveConvertCommand = new RelayCommand(RemoveFromConvert);

            // Initial refresh
            Task.Run(async () => await RefreshSelectionsAsync());
        }

        public async Task RefreshSelectionsAsync()
        {
            try
            {
                var downloadFiles = new ObservableCollection<FileItem>();
                var convertFiles = new ObservableCollection<FileItem>();

                // Get all selections from SelectionManager
                var selections = _selectionManager.GetAllSelections();
                FileLogger.Log($"SelectionSummary: Found {selections.Count} total selections");

                // First, process folder markers to get all files
                var folderMarkers = selections.Where(kvp => kvp.Key.EndsWith("\\*") && 
                    (kvp.Value.IsSelectedForDownload == true || kvp.Value.IsSelectedForConversion == true))
                    .ToList();
                
                foreach (var marker in folderMarkers)
                {
                    var folderPath = marker.Key.Substring(0, marker.Key.Length - 2); // Remove \*
                    var isLocal = !folderPath.StartsWith("\\\\");
                    
                    FileLogger.Log($"Processing folder marker: {marker.Key}, folder: {folderPath}");
                    
                    // Get all files from the folder contents stored in SelectionManager
                    var folderContents = _selectionManager.GetFolderContents(folderPath);
                    
                    // If folder contents not loaded yet, load them now
                    if (folderContents == null || !folderContents.Any())
                    {
                        FileLogger.Log($"  Folder contents not loaded, loading now...");
                        try
                        {
                            var items = isLocal 
                                ? await _localFileService.GetDirectoryContentsAsync(folderPath, System.Threading.CancellationToken.None)
                                : await _revitServerService.GetDirectoryContentsAsync(folderPath, System.Threading.CancellationToken.None);
                            
                            var paths = items.Select(i => i.Path).ToList();
                            _selectionManager.SetFolderContents(folderPath, paths);
                            folderContents = paths;
                            FileLogger.Log($"  Loaded {paths.Count} items from folder");
                        }
                        catch (Exception ex)
                        {
                            FileLogger.LogError($"  Failed to load folder contents: {ex.Message}");
                        }
                    }
                    
                    if (folderContents != null && folderContents.Any())
                    {
                        FileLogger.Log($"  Found {folderContents.Count} items in folder contents");
                        foreach (var filePath in folderContents.Where(p => !p.EndsWith("\\*")))
                        {
                            // Skip if not a .rvt file
                            if (filePath.IndexOf(".rvt", StringComparison.OrdinalIgnoreCase) < 0)
                                continue;
                                
                            // Check if this specific file has been deselected
                            var fileState = _selectionManager.GetSelectionState(filePath);
                            
                            // Use file's specific state if it exists, otherwise use folder's state
                            var shouldDownload = fileState?.IsSelectedForDownload ?? marker.Value.IsSelectedForDownload;
                            var shouldConvert = fileState?.IsSelectedForConversion ?? marker.Value.IsSelectedForConversion;
                            
                            if (shouldDownload == true || shouldConvert == true)
                            {
                                var fileName = System.IO.Path.GetFileName(filePath);
                                var fileItem = new FileItem
                                {
                                    Name = fileName,
                                    Path = filePath,
                                    IsLocal = isLocal
                                };
                                
                                if (shouldDownload == true && !isLocal)
                                    downloadFiles.Add(fileItem);
                                    
                                if (shouldConvert == true)
                                    convertFiles.Add(fileItem);
                            }
                        }
                    }
                    else
                    {
                        FileLogger.Log($"  No folder contents found for: {folderPath}");
                    }
                }
                
                // Then process individual file selections that aren't part of folders
                FileLogger.Log($"Processing individual file selections...");
                foreach (var kvp in selections)
                {
                    var path = kvp.Key;
                    var state = kvp.Value;

                    // Skip if no selections
                    if (state.IsSelectedForDownload != true && state.IsSelectedForConversion != true)
                        continue;

                    // Skip folder markers (already processed above)
                    if (path.EndsWith("\\*"))
                        continue;
                    
                    // Skip directories - only process .rvt files
                    if (path.IndexOf(".rvt", StringComparison.OrdinalIgnoreCase) < 0)
                    {
                        // This might be a folder without a marker
                        if (!path.Contains(".") && (state.IsSelectedForDownload == true || state.IsSelectedForConversion == true))
                        {
                            FileLogger.Log($"  Found folder selection without marker: {path}");
                            var isLocal = !path.StartsWith("\\\\");
                            
                            // Check if we already have a folder marker for this path
                            var hasFolderMarker = selections.ContainsKey(path + "\\*");
                            if (hasFolderMarker)
                            {
                                FileLogger.Log($"  Skipping - folder marker already exists for: {path}");
                                continue;
                            }
                            
                            // Load folder contents
                            try
                            {
                                var items = isLocal 
                                    ? await _localFileService.GetDirectoryContentsAsync(path, System.Threading.CancellationToken.None)
                                    : await _revitServerService.GetDirectoryContentsAsync(path, System.Threading.CancellationToken.None);
                                
                                var paths = items.Select(i => i.Path).ToList();
                                _selectionManager.SetFolderContents(path, paths);
                                
                                // Process the files in this folder
                                foreach (var filePath in paths.Where(p => p.IndexOf(".rvt", StringComparison.OrdinalIgnoreCase) >= 0))
                                {
                                    // Check if this file is already included
                                    var fileAlreadyIncluded = downloadFiles.Any(f => f.Path == filePath) || 
                                                        convertFiles.Any(f => f.Path == filePath);
                                    if (fileAlreadyIncluded)
                                    {
                                        FileLogger.Log($"    Skipping duplicate file: {filePath}");
                                        continue;
                                    }
                                    
                                    var fileName = System.IO.Path.GetFileName(filePath);
                                    var fileItem = new FileItem
                                    {
                                        Name = fileName,
                                        Path = filePath,
                                        IsLocal = isLocal
                                    };
                                    
                                    if (state.IsSelectedForDownload == true && !isLocal)
                                        downloadFiles.Add(fileItem);
                                        
                                    if (state.IsSelectedForConversion == true)
                                        convertFiles.Add(fileItem);
                                }
                            }
                            catch (Exception ex)
                            {
                                FileLogger.LogError($"  Failed to load folder contents: {ex.Message}");
                            }
                        }
                        continue;
                    }
                    
                    // Check if this file is already included from a folder
                    var alreadyIncluded = downloadFiles.Any(f => f.Path == path) || 
                                        convertFiles.Any(f => f.Path == path);
                    if (alreadyIncluded)
                        continue;
                    
                    // This is an individual .rvt file selection
                    if (true)
                    {
                        FileLogger.Log($"  Processing file: {path}, Download={state.IsSelectedForDownload}, Convert={state.IsSelectedForConversion}");
                        
                        var fileName = System.IO.Path.GetFileName(path);
                        var isLocal = !path.StartsWith("\\\\");
                        
                        var fileItem = new FileItem
                        {
                            Name = fileName,
                            Path = path,
                            IsLocal = isLocal
                        };

                        if (state.IsSelectedForDownload == true && !isLocal)
                        {
                            downloadFiles.Add(fileItem);
                            FileLogger.Log($"    Added to download list");
                        }
                        
                        if (state.IsSelectedForConversion == true)
                        {
                            convertFiles.Add(fileItem);
                            FileLogger.Log($"    Added to convert list");
                        }
                    }
                }

                // Update collections
                DownloadFiles = downloadFiles;
                ConvertFiles = convertFiles;

                FileLogger.Log($"SelectionSummary: {DownloadCount} download files, {ConvertCount} convert files");
            }
            catch (Exception ex)
            {
                FileLogger.LogError($"Error refreshing selections: {ex.Message}");
            }
        }

        private void UpdateCounts()
        {
            DownloadCount = DownloadFiles?.Count ?? 0;
            ConvertCount = ConvertFiles?.Count ?? 0;
            
            // Calculate unique files (some might be in both lists)
            var uniqueFiles = DownloadFiles.Union(ConvertFiles, new FileItemComparer()).ToList();
            TotalCount = uniqueFiles.Count;

            // Update size texts (this is simplified - you might want to get actual file sizes)
            DownloadSizeText = $"~{DownloadCount * 50} MB";
            ConvertSizeText = $"~{ConvertCount * 50} MB";
            TotalSizeText = $"~{TotalCount * 50} MB";
        }

        private void ClearAllSelections()
        {
            _selectionManager.ClearAllSelections();
            Task.Run(async () => await RefreshSelectionsAsync());
        }

        private void RemoveFromDownload(object parameter)
        {
            if (parameter is FileItem file)
            {
                _selectionManager.SetSelection(file.Path, true, false);
                DownloadFiles.Remove(file);
                UpdateCounts();
            }
        }

        private void RemoveFromConvert(object parameter)
        {
            if (parameter is FileItem file)
            {
                _selectionManager.SetSelection(file.Path, false, false);
                ConvertFiles.Remove(file);
                UpdateCounts();
            }
        }

        private class FileItemComparer : System.Collections.Generic.IEqualityComparer<FileItem>
        {
            public bool Equals(FileItem x, FileItem y)
            {
                return x?.Path == y?.Path;
            }

            public int GetHashCode(FileItem obj)
            {
                return obj?.Path?.GetHashCode() ?? 0;
            }
        }
    }
}