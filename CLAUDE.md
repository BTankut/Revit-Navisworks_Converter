# CLAUDE.md - RVT to Navisworks Converter

## Overview
This is a WPF desktop application built with .NET Framework 4.8 that facilitates downloading Revit (.rvt) files from a Revit Server and converting them to Navisworks formats (.nwd/.nwc). The application follows MVVM architecture pattern with dependency injection and Material Design UI components.

## Architecture & Patterns

### Core Architecture
- **Pattern**: MVVM (Model-View-ViewModel) with strict separation of concerns
- **Framework**: WPF with .NET Framework 4.8, C# 8.0 language features
- **Dependency Injection**: Microsoft.Extensions.DependencyInjection for service registration and resolution
- **UI Framework**: Material Design in XAML for modern UI components
- **Data Binding**: Two-way data binding with INotifyPropertyChanged implementation

### Key Design Decisions
- **Service-Oriented Architecture**: Business logic encapsulated in injectable services with interfaces
- **Command Pattern**: RelayCommand implementation for UI actions
- **Async/Await**: Extensive use of asynchronous operations for file I/O and network calls
- **Event-Driven Updates**: PropertyChanged events and custom events for UI synchronization

## Project Structure

### Root Configuration Files
- `RvtToNavisConverter.csproj` - .NET project file with package references
- `appsettings.json` - Application configuration (server IPs, tool paths, default directories)
- `App.xaml/App.xaml.cs` - Application entry point with DI container setup
- `MainWindow.xaml/MainWindow.xaml.cs` - Main application window with navigation logic

### Core Directories

#### `/Models/` - Data Models
- `IFileSystemItem.cs` - Common interface for files and folders with selection state
- `FileItem.cs` - Represents individual files with download/conversion selection state
- `FolderItem.cs` - Represents directories with hierarchical selection logic
- `AppSettings.cs` - Configuration data model
- `ConversionTask.cs` - Task representation for processing queue

#### `/ViewModels/` - MVVM View Models
- `ViewModelBase.cs` - Base class with INotifyPropertyChanged implementation
- `MainViewModel.cs` - Primary application logic and file system navigation
- `SettingsViewModel.cs` - Configuration management with validation
- `ProgressViewModel.cs` - Task progress tracking and monitoring
- `MonitorViewModel.cs` - PowerShell command logging and monitoring

#### `/Views/` - WPF Views
- `SettingsView.xaml` - Configuration interface with real-time validation
- `ProgressView.xaml` - Progress tracking modal dialog
- `MonitorView.xaml` - Command logging window
- `PermissionDialog.xaml` - Permission validation results dialog

#### `/Services/` - Business Logic Services
**Core Services:**
- `ISettingsService/SettingsService` - JSON-based configuration persistence
- `IRevitServerService/RevitServerService` - Revit Server API communication
- `ILocalFileService/LocalFileService` - Local file system operations
- `IFileDownloadService/FileDownloadService` - File download coordination
- `INavisworksConversionService/NavisworksConversionService` - Navisworks conversion via external tools
- `IFileStatusService/FileStatusService` - File processing state management
- `IValidationService/ValidationService` - IP/path validation with async network checks
- `IToolDetectionService/ToolDetectionService` - Automatic Revit/Navisworks tool discovery
- `IRevitFileVersionService/RevitFileVersionService` - Revit file version detection and compatibility
- `SelectionManager` - Hierarchical file selection state management

**Security Services:**
- `ILicenseService/LicenseService` - License validation and management
- `IHardwareIdService/HardwareIdService` - Hardware identifier generation
- `ICryptoService/CryptoService` - AES encryption for local storage
- `IRsaCryptoService/RsaCryptoService` - RSA signature verification

#### `/Helpers/` - Utility Classes
- `PowerShellHelper.cs` - PowerShell script execution with logging
- `FileHelper.cs` - File system utility operations
- `FileLogger.cs` - Application-wide logging to text files
- `PermissionChecker.cs` - File system permission validation
- `RelayCommand.cs` - ICommand implementation for MVVM
- **Converters:** Various IValueConverter implementations for UI data binding

## Key Business Workflows

### Application Startup
1. **DI Container Setup** (App.xaml.cs): Register all services, ViewModels, and Views
2. **Permission Validation** (MainWindow): Check file system permissions on load
3. **Settings Loading** (SettingsService): Load configuration from appsettings.json
4. **UI Initialization** (MainViewModel): Initialize file system browsing state

### File System Operations
1. **Server Connection**: Connect to Revit Server using configured IP and credentials
2. **Local Browsing**: Navigate local file system for existing .rvt files
3. **Version Detection**: Automatic Revit version detection from server paths, filenames, and file content
4. **Selection Management**: Hierarchical selection state (parent/child folder relationships)
5. **Batch Processing**: Queue files for download and/or conversion operations

### External Tool Integration
- **Revit Server Tool**: `RevitServerTool.exe` for server communication and file download
- **Navisworks Tool**: `FileToolsTaskRunner.exe` for .rvt to .nwd/.nwc conversion
- **PowerShell Execution**: All external tools executed via PowerShell with comprehensive logging
- **Tool Version Management**: Support for multiple tool versions with automatic detection

### Key Features (v1.8.0)
1. **Automatic Tool Detection**
   - Scans Program Files for installed Revit/Navisworks versions
   - Dropdown selection for available tool versions
   - Persists tool selections in settings

2. **Revit Version Detection**
   - Server path parsing (e.g., `\Revit Server 2021\`)
   - Filename pattern matching (e.g., `R21` → `2021`)
   - File content analysis for version strings
   - Real-time version display in UI

3. **Version Compatibility Checking**
   - Pre-process validation of file/tool version matching
   - Warning dialog for incompatible versions
   - User override option for compatibility warnings

## Configuration Management

### Settings Architecture
- **Storage**: JSON-based configuration in `appsettings.json`
- **Validation**: Real-time IP ping validation and path existence checking
- **UI Feedback**: Live validation status with color-coded indicators
- **Persistence**: Automatic save on settings changes

### Key Configuration Points
```json
{
  "AppSettings": {
    "RevitServerIp": "Server IP address",
    "RevitServerAccelerator": "Accelerator IP/hostname", 
    "RevitServerToolPath": "Path to RevitServerTool.exe",
    "NavisworksToolPath": "Path to FileToolsTaskRunner.exe",
    "DefaultDownloadPath": "Default download directory",
    "DefaultNwcPath": "Default .nwc output directory",
    "DefaultNwdPath": "Default .nwd output directory"
  }
}
```

## Development Workflow

### Build Commands
```bash
# Build the application
dotnet build

# Run the application
dotnet run

# Build for release
dotnet build --configuration Release
```

### Dependencies
**NuGet Packages:**
- `MaterialDesignThemes` (4.8.0) - UI components and theming
- `Microsoft.Extensions.DependencyInjection` (6.0.1) - Service container
- `Newtonsoft.Json` (13.0.3) - JSON serialization
- `Microsoft.Xaml.Behaviors.Wpf` (1.1.39) - XAML behaviors

**Node.js Dependencies (MCP Integration):**
- `@missionsquad/mcp-github` - GitHub integration for Model Context Protocol
- `@modelcontextprotocol/server-github` - GitHub server for MCP

### Version History Pattern
- **v2.3.0**: Enhanced logging with timestamps and improved UI text alignment
  - Added timestamps to PowerShell monitor logs for better tracking
  - Added timestamps to progress window logs during file processing
  - Fixed vertical text alignment in PowerShell monitor (now top-aligned)
  - Fixed vertical text alignment in progress window (now top-aligned)
  - Made PowerShell monitor text selectable while maintaining word wrap
  - Improved overall logging visibility and usability
- **v2.2.0**: Fixed critical folder selection processing bugs
  - Fixed issue where deselected files within a selected folder were still being processed
  - Fixed "Start Processing" button not working correctly across different directories
  - Improved folder marker handling to respect individual file selection overrides
  - Enhanced selection state management and processing logic
- **v2.1.0**: RSA-based licensing system with Hardware ID protection
  - Secure license generation with asymmetric encryption
  - Hardware ID based on MAC address, CPU ID, and volume serial
  - About dialog with copyable Hardware ID
  - Automatic license file import on startup
  - Support for both Trial and Full licenses
- **v2.0.0**: Production-ready release with Windows Server 2019 support
- **v1.8.0**: Revit file version detection and compatibility checking
- **v1.7.0**: Automatic tool detection for multiple Revit/Navisworks versions
- **v1.6.0**: Permission validation system and UX improvements
- **v1.5.0**: Indeterminate checkbox states for folder hierarchy
- **v1.4.0**: Bug fixes for folder selection clearing
- **v1.3.0**: Clear All Selections functionality
- **v1.2.0**: Folder selection for both download and conversion

## Key Technical Considerations

### Error Handling
- **Global Exception Handling**: App-level dispatcher exception handling
- **Service-Level**: Try/catch blocks with proper error propagation
- **UI Feedback**: Status messages and validation indicators
- **Logging**: Comprehensive file-based logging for troubleshooting

### Performance Optimizations
- **Async Operations**: Non-blocking UI during file operations
- **Lazy Loading**: File system items loaded on-demand during navigation
- **Memory Management**: Proper disposal of file handles and network resources

### Security Considerations
- **Permission Validation**: Startup checks for required file system permissions
- **Input Validation**: Path and IP address validation before execution
- **External Tool Safety**: PowerShell execution with controlled parameter passing
- **Licensing System**: RSA-2048 asymmetric encryption for license signatures
  - Hardware ID generation from system identifiers
  - License validation only in Release builds
  - Automatic import of `.lic` files from app directory or `%AppData%`
  - Secure storage in `%LocalAppData%` with registry backup

## Working with This Codebase

### Adding New Features
1. **Services**: Create interface in `/Services/` and implement with DI registration
2. **UI Components**: Follow MVVM pattern with ViewModels in `/ViewModels/`
3. **Models**: Add data classes in `/Models/` with INotifyPropertyChanged if needed
4. **Validation**: Extend ValidationService for new input validation requirements

### Testing Approach
- **Manual Testing**: Run application and test workflows end-to-end
- **Configuration Testing**: Verify settings persistence and validation
- **External Tool Integration**: Test with actual Revit Server and Navisworks installations

### Common Modification Patterns
- **New File Types**: Extend IFileSystemItem interface and service implementations
- **Additional Servers**: Abstract server communication interfaces
- **UI Enhancements**: Leverage Material Design components and MVVM data binding
- **Logging Extensions**: Extend FileLogger for additional log categories

### Dependencies for Development
- **Visual Studio 2019+** or **VS Code** with C# extension
- **Autodesk Revit Server** (for server functionality testing)
- **Autodesk Navisworks Manage** (for conversion functionality testing)
- **.NET Framework 4.8 Developer Pack**

## Troubleshooting Common Issues

### Build Issues
- Ensure .NET Framework 4.8 Developer Pack is installed
- Check NuGet package restoration
- Verify MaterialDesignThemes package compatibility

### Runtime Issues
- Check `appsettings.json` configuration completeness
- Validate file system permissions (run as administrator if needed)
- Ensure external tools (RevitServerTool.exe, FileToolsTaskRunner.exe) are accessible
- Review `app_log.txt` for detailed error information

### Performance Issues
- Monitor file system access patterns for large directory structures
- Check network connectivity for server operations
- Review PowerShell execution logs for external tool performance