# CLAUDE.md - RVT to Navisworks Converter

## Overview
This is a WPF desktop application built with .NET Framework 4.8 that facilitates downloading Revit (.rvt) files from a Revit Server and converting them to Navisworks formats (.nwd/.nwc). The application follows MVVM architecture pattern with dependency injection and Material Design UI components.

## Memories
- Benimle her zaman Türkçe konuş

## Architecture & Patterns

### Core Architecture
- **Pattern**: MVVM (Model-View-ViewModel) with strict separation of concerns
- **Framework**: WPF with .NET Framework 4.8, C# 8.0 language features
- **Dependency Injection**: Microsoft.Extensions.DependencyInjection for service registration and resolution
- **UI Framework**: Material Design in XAML for modern UI components
- **Data Binding**: Two-way data binding with INotifyPropertyChanged implementation

## Recent Version History

### v2.4.9 (2025-01-21)
- Fixed duplicate file counting in error reports
- Added Contains() check before adding files to FailedFiles list
- Improved error reporting accuracy

### v2.4.8 (2025-01-21)
- Added conversion.log parsing to detect actual conversion failures
- Fixed false success reporting when files failed with "Load was canceled" error
- Enhanced error detection by checking both PowerShell output and conversion logs

### v2.4.7 (2025-01-21)
- Restored /log parameter support for conversion logging
- Fixed issue where log parameter was mistakenly removed
- Re-enabled conversion.log generation in NWD folder

### v2.4.6 (2025-01-21)
- Fixed temp file locking issues by adding GUID to temp file names
- Implemented try-finally blocks for proper cleanup
- Added delays to ensure FileToolsTaskRunner finishes before deletion
- Resolved "dosyasına erişemiyor" (cannot access file) errors

### Key Technical Improvements
1. **Temp File Management**: Each conversion now uses unique temp files with GUID
2. **Error Detection**: Dual-layer error checking (PowerShell output + conversion logs)
3. **Duplicate Prevention**: Proper handling of multiple error lines for same file
4. **Logging Support**: Full restoration of Navisworks logging capabilities