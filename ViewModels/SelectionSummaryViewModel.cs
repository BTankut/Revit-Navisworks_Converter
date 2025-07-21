using RvtToNavisConverter.Helpers;
using RvtToNavisConverter.Models;
using RvtToNavisConverter.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

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

            RefreshCommand = new RelayCommand(_ => RefreshSelections());
            ClearAllCommand = new RelayCommand(_ => ClearAllSelections());
            RemoveDownloadCommand = new RelayCommand(RemoveFromDownload);
            RemoveConvertCommand = new RelayCommand(RemoveFromConvert);

            RefreshSelections();
        }

        public async void RefreshSelections()
        {
            try
            {
                var downloadFiles = new ObservableCollection<FileItem>();
                var convertFiles = new ObservableCollection<FileItem>();

                // Get all selections from SelectionManager
                var selections = _selectionManager.GetAllSelections();
                FileLogger.Log($"SelectionSummary: Found {selections.Count} total selections");

                foreach (var kvp in selections)
                {
                    var path = kvp.Key;
                    var state = kvp.Value;

                    // Skip if no selections
                    if (state.IsSelectedForDownload != true && state.IsSelectedForConversion != true)
                        continue;

                    // Check if this is a file or folder
                    if (path.EndsWith("\\*"))
                    {
                        // This is a folder marker - skip it, we'll process files within folders
                        continue;
                    }
                    else if (System.IO.File.Exists(path) || !System.IO.Directory.Exists(path))
                    {
                        // This is a file (or at least not a directory)
                        var fileName = System.IO.Path.GetFileName(path);
                        var isLocal = !path.StartsWith("\\\\");
                        
                        var fileItem = new FileItem
                        {
                            Name = fileName,
                            Path = path,
                            IsLocal = isLocal
                        };

                        if (state.IsSelectedForDownload == true && !isLocal)
                            downloadFiles.Add(fileItem);
                        
                        if (state.IsSelectedForConversion == true)
                            convertFiles.Add(fileItem);
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

        private async System.Threading.Tasks.Task ProcessItemsRecursive(
            System.Collections.Generic.List<IFileSystemItem> items,
            SelectionState parentState,
            ObservableCollection<FileItem> downloadFiles,
            ObservableCollection<FileItem> convertFiles,
            bool isLocal)
        {
            foreach (var item in items)
            {
                if (item is FileItem file)
                {
                    file.IsLocal = isLocal;
                    
                    // Check individual file selection state
                    var fileState = _selectionManager.GetSelectionState(file.Path);
                    
                    // Use file's own state if set, otherwise use parent state
                    var shouldDownload = fileState?.IsSelectedForDownload ?? parentState.IsSelectedForDownload;
                    var shouldConvert = fileState?.IsSelectedForConversion ?? parentState.IsSelectedForConversion;

                    if (shouldDownload == true && !isLocal)
                        downloadFiles.Add(file);
                    
                    if (shouldConvert == true)
                        convertFiles.Add(file);
                }
                else if (item is FolderItem folder)
                {
                    var folderItems = isLocal
                        ? await _localFileService.GetDirectoryContentsAsync(folder.Path, System.Threading.CancellationToken.None)
                        : await _revitServerService.GetDirectoryContentsAsync(folder.Path, System.Threading.CancellationToken.None);

                    // Check folder's own state
                    var folderState = _selectionManager.GetSelectionState(folder.Path + "\\*");
                    var effectiveState = folderState ?? parentState;

                    await ProcessItemsRecursive(folderItems.ToList(), effectiveState, downloadFiles, convertFiles, isLocal);
                }
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
            RefreshSelections();
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