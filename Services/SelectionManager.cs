using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RvtToNavisConverter.Helpers;
using RvtToNavisConverter.Models;

namespace RvtToNavisConverter.Services
{
    public class SelectionManager
    {
        private readonly Dictionary<string, SelectionState> _selectionStates = new Dictionary<string, SelectionState>(StringComparer.OrdinalIgnoreCase);
        
        public event EventHandler<SelectionChangedEventArgs>? SelectionChanged;

        public void SetSelection(string path, bool isDownload, bool? value)
        {
            if (string.IsNullOrEmpty(path)) return;

            FileLogger.Log($"SetSelection: {path}, isDownload: {isDownload}, value: {value}");

            if (!_selectionStates.ContainsKey(path))
            {
                _selectionStates[path] = new SelectionState();
                FileLogger.Log($"  Yeni seçim durumu oluşturuldu");
            }

            bool? oldValue = isDownload ? 
                _selectionStates[path].IsSelectedForDownload : 
                _selectionStates[path].IsSelectedForConversion;
                
            FileLogger.Log($"  Eski değer: {oldValue}, Yeni değer: {value}");

            if (isDownload)
                _selectionStates[path].IsSelectedForDownload = value;
            else
                _selectionStates[path].IsSelectedForConversion = value;

            // Notify UI of the change
            SelectionChanged?.Invoke(this, new SelectionChangedEventArgs { Path = path, IsDownload = isDownload, Value = value });
            FileLogger.Log($"  SelectionChanged event tetiklendi: {path}, isDownload: {isDownload}, value: {value}");
        }

        public bool? GetSelection(string path, bool isDownload)
        {
            if (string.IsNullOrEmpty(path)) return null;

            // First check if the path itself has a selection state
            if (_selectionStates.TryGetValue(path, out var state))
            {
                bool? directSelection = isDownload ? 
                    state.IsSelectedForDownload : 
                    state.IsSelectedForConversion;
                
                if (directSelection.HasValue)
                {
                    FileLogger.Log($"GetSelection: {path}, isDownload: {isDownload}, doğrudan seçim: {directSelection}");
                    return directSelection;
                }
            }
            
            // Check if any parent folder has a marker indicating all children are selected
            string currentPath = path;
            while (!string.IsNullOrEmpty(currentPath))
            {
                string parentPath = Path.GetDirectoryName(currentPath);
                if (string.IsNullOrEmpty(parentPath))
                    break;
                
                string folderMarker = parentPath.TrimEnd('\\') + "\\*";
                if (_selectionStates.TryGetValue(folderMarker, out var markerState))
                {
                    bool? markerSelection = isDownload ? 
                        markerState.IsSelectedForDownload : 
                        markerState.IsSelectedForConversion;
                    
                    if (markerSelection == true)
                    {
                        FileLogger.Log($"GetSelection: {path}, isDownload: {isDownload}, üst klasör marker'ı bulundu: {folderMarker}, değer: true");
                        return true;
                    }
                }
                
                currentPath = parentPath;
            }
            
            FileLogger.Log($"GetSelection: {path}, isDownload: {isDownload}, seçim bulunamadı, varsayılan: null");
            return null;
        }

        public void SelectFolderRecursively(string folderPath, bool isDownload, bool value)
        {
            if (string.IsNullOrEmpty(folderPath)) return;
            
            FileLogger.Log($"SelectFolderRecursively: {folderPath}, isDownload: {isDownload}, value: {value}");
            
            // Mark the folder path with a special suffix to indicate it's fully selected
            string folderMarker = folderPath.TrimEnd('\\') + "\\*";
            
            if (value)
            {
                // When selecting a folder, we add a special marker to indicate all children are selected
                if (!_selectionStates.ContainsKey(folderMarker))
                {
                    _selectionStates[folderMarker] = new SelectionState();
                    FileLogger.Log($"  Yeni marker oluşturuldu: {folderMarker}");
                }
                
                if (isDownload)
                {
                    _selectionStates[folderMarker].IsSelectedForDownload = true;
                    FileLogger.Log($"  Download için işaretlendi");
                }
                else
                {
                    _selectionStates[folderMarker].IsSelectedForConversion = true;
                    FileLogger.Log($"  Conversion için işaretlendi");
                }
                
                // Notify that the marker was added
                SelectionChanged?.Invoke(this, new SelectionChangedEventArgs 
                { 
                    Path = folderMarker, 
                    IsDownload = isDownload, 
                    Value = true,
                    IsMarker = true
                });
                FileLogger.Log($"  SelectionChanged event tetiklendi: {folderMarker}, isDownload: {isDownload}, value: true");
            }
            else
            {
                // ÖNCE: Klasör seçimi kaldırılırken tüm alt öğeleri de temizle
                var childrenToRemove = _selectionStates.Keys
                    .Where(path => !path.EndsWith("\\*") && 
                           path.StartsWith(folderPath, StringComparison.OrdinalIgnoreCase) &&
                           !path.Equals(folderPath, StringComparison.OrdinalIgnoreCase))
                    .ToList();
                
                FileLogger.Log($"  Temizlenecek alt öğe sayısı: {childrenToRemove.Count}");
                
                foreach (var childPath in childrenToRemove)
                {
                    FileLogger.Log($"  Alt öğe temizleniyor: {childPath}");
                    SetSelection(childPath, isDownload, false);
                }
                
                // SONRA: Marker'ı kaldır veya güncelle
                if (_selectionStates.ContainsKey(folderMarker))
                {
                    if (isDownload)
                    {
                        _selectionStates[folderMarker].IsSelectedForDownload = false;
                        FileLogger.Log($"  Download işareti kaldırıldı");
                    }
                    else
                    {
                        _selectionStates[folderMarker].IsSelectedForConversion = false;
                        FileLogger.Log($"  Conversion işareti kaldırıldı");
                    }
                        
                    // If both selections are false, remove the marker entirely
                    if (_selectionStates[folderMarker].IsSelectedForDownload == false && 
                        _selectionStates[folderMarker].IsSelectedForConversion == false)
                    {
                        _selectionStates.Remove(folderMarker);
                        FileLogger.Log($"  Marker tamamen kaldırıldı: {folderMarker}");
                    }
                    
                    // Notify that the marker was removed - Bu UI'daki tüm alt öğeleri temizleyecek
                    SelectionChanged?.Invoke(this, new SelectionChangedEventArgs 
                    { 
                        Path = folderMarker, 
                        IsDownload = isDownload, 
                        Value = false,
                        IsMarker = true
                    });
                    FileLogger.Log($"  SelectionChanged event tetiklendi: {folderMarker}, isDownload: {isDownload}, value: false");
                }
            }
            
            // Update all existing paths that are under this folder (sadece seçim durumunda)
            if (value)
            {
                foreach (var path in _selectionStates.Keys.ToList())
                {
                    if (path != folderMarker && !path.EndsWith("\\*") && 
                        path.StartsWith(folderPath, StringComparison.OrdinalIgnoreCase))
                    {
                        FileLogger.Log($"  Alt öğe güncelleniyor: {path}");
                        SetSelection(path, isDownload, value);
                    }
                }
            }

            // Also set the folder itself
            FileLogger.Log($"  Klasörün kendisi işaretleniyor: {folderPath}");
            SetSelection(folderPath, isDownload, value);

            // Update parent states
            FileLogger.Log($"  Üst klasörlerin durumu güncelleniyor");
            UpdateParentStates(folderPath, isDownload);
        }

        public void UpdateParentStates(string changedPath, bool isDownload, IEnumerable<IFileSystemItem>? currentItems = null)
        {
            if (string.IsNullOrEmpty(changedPath)) return;
            
            FileLogger.Log($"UpdateParentStates: {changedPath}, isDownload: {isDownload}");
            
            var parentPath = Path.GetDirectoryName(changedPath);
            while (!string.IsNullOrEmpty(parentPath))
            {
                FileLogger.Log($"  Üst klasör kontrol ediliyor: {parentPath}");
                
                // Önce mevcut UI öğelerinden alt öğeleri bul
                var children = new List<string>();
                if (currentItems != null)
                {
                    var parentDir = Path.GetDirectoryName(changedPath);
                    if (!string.IsNullOrEmpty(parentDir) && parentDir.Equals(parentPath, StringComparison.OrdinalIgnoreCase))
                    {
                        children.AddRange(currentItems.Select(item => item.Path));
                        FileLogger.Log($"  UI'dan {children.Count} alt öğe bulundu");
                    }
                }
                
                // Eğer UI'dan bulamazsak, kayıtlı olanları kullan
                if (!children.Any())
                {
                    children = GetChildren(parentPath);
                    FileLogger.Log($"  Kayıtlardan {children.Count} alt öğe bulundu");
                }
                
                if (children.Any())
                {
                    // Alt öğelerin gerçek seçim durumlarını kontrol et
                    var childStates = new List<bool?>();
                    
                    foreach (var child in children)
                    {
                        var childState = GetSelection(child, isDownload);
                        childStates.Add(childState);
                        FileLogger.Log($"    Alt öğe: {child}, durum: {childState}");
                    }
                    
                    var selectedCount = childStates.Count(s => s == true);
                    var unselectedCount = childStates.Count(s => s == false || s == null);
                    
                    FileLogger.Log($"  Alt öğe durumları: seçili={selectedCount}, seçili değil={unselectedCount}, toplam={children.Count}");

                    bool? parentState;
                    
                    // Eğer tüm alt öğeler seçili ise
                    if (selectedCount == children.Count && selectedCount > 0)
                    {
                        parentState = true;
                        FileLogger.Log($"  Tüm alt öğeler seçili, üst klasör seçili olarak işaretleniyor");
                    }
                    // Eğer hiçbir alt öğe seçili değil ise
                    else if (selectedCount == 0)
                    {
                        parentState = false;
                        FileLogger.Log($"  Hiçbir alt öğe seçili değil, üst klasör seçili değil olarak işaretleniyor");
                    }
                    // Karışık durum (bazı seçili, bazı değil)
                    else
                    {
                        parentState = null; // Indeterminate state
                        FileLogger.Log($"  Karışık seçim durumu, üst klasör belirsiz olarak işaretleniyor");
                    }

                    FileLogger.Log($"  Üst klasör durumu: {parentState}");
                    
                    // Sadece durum değişmişse güncelle
                    var currentParentState = GetSelection(parentPath, isDownload);
                    if (currentParentState != parentState)
                    {
                        FileLogger.Log($"  Üst klasör durumu değişti: {currentParentState} -> {parentState}");
                        SetSelection(parentPath, isDownload, parentState);
                    }
                    else
                    {
                        FileLogger.Log($"  Üst klasör durumu aynı kaldı: {parentState}");
                    }
                }
                else
                {
                    // Alt öğe yoksa, klasörü seçili değil olarak işaretle
                    FileLogger.Log($"  Alt öğe yok, klasör seçili değil olarak işaretleniyor");
                    SetSelection(parentPath, isDownload, false);
                }
                
                parentPath = Path.GetDirectoryName(parentPath);
            }
        }

        private List<string> GetChildren(string folderPath)
        {
            if (string.IsNullOrEmpty(folderPath)) return new List<string>();
            
            return _selectionStates.Keys
                .Where(path => !path.EndsWith("\\*") && 
                       Path.GetDirectoryName(path)?.Equals(folderPath, StringComparison.OrdinalIgnoreCase) == true)
                .ToList();
        }

        public void ClearAllSelections()
        {
            _selectionStates.Clear();
            SelectionChanged?.Invoke(this, new SelectionChangedEventArgs { Path = "*", IsDownload = true, Value = false });
            SelectionChanged?.Invoke(this, new SelectionChangedEventArgs { Path = "*", IsDownload = false, Value = false });
        }

        public Dictionary<string, SelectionState> GetAllSelections()
        {
            return new Dictionary<string, SelectionState>(_selectionStates, StringComparer.OrdinalIgnoreCase);
        }

        public void RestoreSelections(Dictionary<string, SelectionState> selections)
        {
            if (selections == null) return;
            
            FileLogger.Log($"RestoreSelections: {selections.Count} seçim geri yükleniyor");
            
            _selectionStates.Clear();
            foreach (var kvp in selections)
            {
                _selectionStates[kvp.Key] = new SelectionState
                {
                    IsSelectedForDownload = kvp.Value.IsSelectedForDownload,
                    IsSelectedForConversion = kvp.Value.IsSelectedForConversion
                };
                
                FileLogger.Log($"  Seçim geri yüklendi: {kvp.Key}, Download: {kvp.Value.IsSelectedForDownload}, Convert: {kvp.Value.IsSelectedForConversion}");
            }
            
            // Notify UI of all changes
            foreach (var kvp in _selectionStates)
            {
                if (kvp.Value.IsSelectedForDownload.HasValue)
                {
                    SelectionChanged?.Invoke(this, new SelectionChangedEventArgs 
                    { 
                        Path = kvp.Key, 
                        IsDownload = true, 
                        Value = kvp.Value.IsSelectedForDownload,
                        IsMarker = kvp.Key.EndsWith("\\*")
                    });
                    
                    FileLogger.Log($"  SelectionChanged event tetiklendi: {kvp.Key}, isDownload: true, value: {kvp.Value.IsSelectedForDownload}");
                }
                
                if (kvp.Value.IsSelectedForConversion.HasValue)
                {
                    SelectionChanged?.Invoke(this, new SelectionChangedEventArgs 
                    { 
                        Path = kvp.Key, 
                        IsDownload = false, 
                        Value = kvp.Value.IsSelectedForConversion,
                        IsMarker = kvp.Key.EndsWith("\\*")
                    });
                    
                    FileLogger.Log($"  SelectionChanged event tetiklendi: {kvp.Key}, isDownload: false, value: {kvp.Value.IsSelectedForConversion}");
                }
            }
        }
        
        // Yeni metot: Bir klasörün tüm alt öğelerini işaretlemek için
        public void ApplyFolderMarkersToItems(IEnumerable<IFileSystemItem> items)
        {
            if (items == null) return;
            
            // Önce tüm klasör işaretleyicilerini bul
            var folderMarkers = _selectionStates.Keys
                .Where(k => k.EndsWith("\\*"))
                .ToList();
                
            FileLogger.Log($"ApplyFolderMarkersToItems: Bulunan klasör işaretleyicileri: {string.Join(", ", folderMarkers)}");
                
            foreach (var marker in folderMarkers)
            {
                string folderPath = marker.Substring(0, marker.Length - 2); // "\*" kısmını çıkar
                var state = _selectionStates[marker];
                
                FileLogger.Log($"İşaretleyici: {marker}, Klasör: {folderPath}, Download: {state.IsSelectedForDownload}, Convert: {state.IsSelectedForConversion}");
                
                // Bu klasörün altındaki tüm öğeleri işaretle
                foreach (var item in items)
                {
                    if (item.Path.StartsWith(folderPath, StringComparison.OrdinalIgnoreCase))
                    {
                        FileLogger.Log($"  Öğe: {item.Path} işaretleniyor");
                        
                        if (state.IsSelectedForDownload.HasValue && state.IsSelectedForDownload.Value)
                        {
                            item.IsSelectedForDownload = true;
                            FileLogger.Log($"    Download için işaretlendi");
                        }
                        
                        if (state.IsSelectedForConversion.HasValue && state.IsSelectedForConversion.Value)
                        {
                            item.IsSelectedForConversion = true;
                            FileLogger.Log($"    Conversion için işaretlendi");
                        }
                    }
                }
            }
        }
    }

    public class SelectionState
    {
        public bool? IsSelectedForDownload { get; set; } = null;
        public bool? IsSelectedForConversion { get; set; } = null;
    }

    public class SelectionChangedEventArgs : EventArgs
    {
        public string Path { get; set; } = string.Empty;
        public bool IsDownload { get; set; }
        public bool? Value { get; set; }
        public bool IsMarker { get; set; } = false;
    }
}
