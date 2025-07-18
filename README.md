# RVT to Navisworks Converter

A WPF desktop application for downloading Revit files from Revit Server and converting them to Navisworks formats (.nwd/.nwc).

![Version](https://img.shields.io/badge/version-2.1.0-blue.svg)
![.NET Framework](https://img.shields.io/badge/.NET%20Framework-4.8-purple.svg)
![License](https://img.shields.io/badge/license-MIT-green.svg)

## Features

### Core Functionality
- **Revit Server Integration**: Connect to Revit Server and browse/download .rvt files
- **Local File Support**: Browse and process local Revit files
- **Batch Processing**: Select multiple files for download and/or conversion
- **Format Support**: Convert to both .nwd and .nwc Navisworks formats
- **Progress Tracking**: Real-time progress monitoring with detailed status updates

### Advanced Features (v2.1.0)
- **Automatic Tool Detection**: Scans for installed Revit/Navisworks versions
- **Version Compatibility**: Detects Revit file versions and validates tool compatibility
- **Hierarchical Selection**: Smart folder selection with parent/child relationships
- **Permission Validation**: Startup checks for required file system permissions
- **PowerShell Logging**: Comprehensive logging of all external tool executions
- **Windows Server Support**: Fully tested on Windows Server 2019
- **Licensing System**: RSA-based license protection with Hardware ID
  - Trial licenses with customizable duration
  - Full licenses for permanent use
  - Hardware-locked to prevent unauthorized copying
  - About dialog with copyable Hardware ID

## System Requirements

### Minimum Requirements
- Windows 10/11 or Windows Server 2019
- .NET Framework 4.8
- PowerShell 5.1 or higher
- 4GB RAM minimum (8GB recommended)
- Administrator privileges (for initial setup)

### Required Software
- **Autodesk Revit Server** (for server functionality)
- **Autodesk Navisworks Manage** (for conversion functionality)
- RevitServerTool.exe (included with Revit installation)
- FileToolsTaskRunner.exe (included with Navisworks installation)

## Installation

### Quick Start
1. Download the latest release from the [Releases](https://github.com/[your-username]/RvtToNavisConverter/releases) page
2. Extract to your preferred location (e.g., `C:\Program Files\RvtToNavisConverter`)
3. Run `RvtToNavisConverter.exe` as Administrator for first-time setup
4. Configure settings (Revit Server IP, tool paths, default directories)
5. Click "Validate Permissions" to ensure proper access

### Windows Server 2019 Installation
See `INSTALL_INSTRUCTIONS.txt` in the release package for detailed server installation steps.

## Usage

### Initial Configuration
1. Click the **Settings** button
2. Configure:
   - Revit Server IP address
   - Revit Server Accelerator IP (optional)
   - Tool paths (or use "Detect Tools" for automatic detection)
   - Default download and output directories
3. Save settings (validates connections automatically)

### Basic Workflow
1. **Source Selection**: Choose between "Revit Server Files" or "Local Files"
2. **Navigation**: Browse to desired files/folders
3. **Selection**: Check files/folders for processing
4. **Options**: Choose "Download" and/or "Convert" actions
5. **Process**: Click "Process Selected Files" to begin

### Advanced Features

#### Version Detection
The application automatically detects Revit file versions from:
- Server path patterns (e.g., `\Revit Server 2021\`)
- Filename patterns (e.g., `Model_R21.rvt`)
- File content analysis

#### Tool Compatibility
- Pre-process validation ensures file/tool version matching
- Warning dialogs for incompatible versions
- Override option for experienced users

## Architecture

### Technology Stack
- **Framework**: WPF with .NET Framework 4.8
- **Architecture**: MVVM with Dependency Injection
- **UI Library**: Material Design in XAML
- **Data Format**: JSON configuration
- **External Tools**: PowerShell for tool execution

### Key Components
- **Services**: Business logic with interface-based design
- **ViewModels**: MVVM pattern with INotifyPropertyChanged
- **Validation**: Real-time network and path validation
- **Logging**: File-based logging with rotation

## Development

### Prerequisites
- Visual Studio 2019 or later
- .NET Framework 4.8 Developer Pack
- Git for version control

### Building from Source
```bash
# Clone the repository
git clone https://github.com/[your-username]/RvtToNavisConverter.git

# Navigate to project directory
cd RvtToNavisConverter

# Restore NuGet packages and build
dotnet restore
dotnet build

# Run the application
dotnet run
```

### Project Structure
```
RvtToNavisConverter/
├── Models/          # Data models and interfaces
├── ViewModels/      # MVVM ViewModels
├── Views/           # WPF Views (XAML)
├── Services/        # Business logic services
├── Helpers/         # Utility classes
└── Resources/       # Images and resources
```

## Configuration

### appsettings.json
```json
{
  "AppSettings": {
    "RevitServerIp": "192.168.1.100",
    "RevitServerAccelerator": "accelerator.local",
    "RevitServerToolPath": "C:\\Program Files\\Autodesk\\...",
    "NavisworksToolPath": "C:\\Program Files\\Autodesk\\...",
    "DefaultDownloadPath": "C:\\RevitDownloads",
    "DefaultNwcPath": "C:\\NavisworksOutput\\NWC",
    "DefaultNwdPath": "C:\\NavisworksOutput\\NWD"
  }
}
```

## Troubleshooting

### Common Issues

#### Cannot Connect to Revit Server
- Verify network connectivity
- Check Windows Firewall settings
- Ensure Revit Server service is running
- Validate IP address in settings

#### Tool Validation Fails
- Verify Autodesk software installation
- Check file permissions on tool executables
- Use "Detect Tools" feature
- Run as Administrator if needed

#### Permission Errors
- Run application as Administrator for initial setup
- Check folder permissions for output directories
- Review permission validation dialog

### Log Files
Application logs are stored at:
- `%LocalAppData%\RvtToNavisConverter\app_log.txt`

## Version History

- **v2.1.0** - RSA-based licensing system with Hardware ID protection
- **v2.0.0** - Production-ready release with Windows Server 2019 support
- **v1.8.0** - Added Revit file version detection and compatibility checking
- **v1.7.0** - Automatic tool detection for multiple versions
- **v1.6.0** - Permission validation system
- **v1.5.0** - Hierarchical folder selection
- **v1.0.0** - Initial release

## Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- Built with [Material Design in XAML](http://materialdesigninxaml.net/)
- Uses Microsoft.Extensions.DependencyInjection for IoC
- PowerShell integration for external tool execution

## Author

**Baris Tankut** - *Initial work* - [GitHub Profile](https://github.com/[your-username])

## Support

For issues, questions, or contributions:
- Open an issue on [GitHub Issues](https://github.com/[your-username]/RvtToNavisConverter/issues)
- Check existing issues before creating new ones
- Include log files when reporting bugs

---

© 2025 Baris Tankut. All rights reserved.