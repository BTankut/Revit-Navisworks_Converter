# Revit to Navisworks Converter

## Overview

The Revit to Navisworks Converter is a powerful and flexible desktop application designed to streamline the process of downloading Revit files from a Revit Server and converting them into Navisworks formats (.nwd or .nwc). This tool is built for architects, engineers, and construction professionals who need an efficient way to manage their BIM workflows.

The application provides a user-friendly interface to browse both a remote Revit Server and the local file system, select files for processing, and manage conversion settings.

## Features

- **Revit Server Integration**: Connect to a Revit Server, browse project folders, and download `.rvt` files directly to your local machine.
- **Local File System Support**: Browse your local computer to select already downloaded or existing `.rvt` files for conversion.
- **Selective Batch Processing**: Choose which files to download and/or convert, giving you full control over the workflow.
- **Automated Conversion**: Utilizes the Navisworks `FileToolsTaskRunner.exe` to batch convert Revit files to Navisworks formats.
- **Consolidated Output**: Merge multiple files into a single `.nwd` file or convert them to individual `.nwc` files.
- **Configurable Settings**: Easily configure server IP, accelerator information, default file paths, and Navisworks tool paths through a dedicated settings window.
- **Real-time Progress Monitoring**: A detailed progress window shows the status of downloads and conversions, including logs for each step.
- **PowerShell Integration**: Leverages PowerShell for robust and reliable execution of command-line tools.

## Technical Details

- **Framework**: WPF (Windows Presentation Foundation) with .NET 7.
- **Architecture**: MVVM (Model-View-ViewModel) for a clean separation of concerns.
- **Dependency Injection**: Uses `Microsoft.Extensions.DependencyInjection` for managing services.
- **UI Toolkit**: Material Design in XAML for a modern and responsive user interface.

## Getting Started

### Prerequisites

- Windows Operating System
- .NET 7 Desktop Runtime
- Autodesk Revit Server (for server functionality)
- Autodesk Navisworks Manage (for conversion functionality)

### Installation

1.  Clone the repository:
    ```bash
    git clone https://github.com/BTankut/Revit-Navisworks_Converter.git
    ```
2.  Navigate to the project directory:
    ```bash
    cd Revit-Navisworks_Converter
    ```
3.  Build the project using Visual Studio or the .NET CLI:
    ```bash
    dotnet build
    ```
4.  Run the application from the output directory:
    ```bash
    cd bin/Debug/net7.0-windows
    ./RvtToNavisConverter.exe
    ```

### Configuration

Before using the application, you must configure the required paths and server information:

1.  Launch the application.
2.  Click on the **Settings** button.
3.  Fill in the following fields:
    -   **Revit Server IP**: The IP address of your Revit Server.
    -   **Revit Server Accelerator IP**: The IP or hostname of the Revit Server Accelerator.
    -   **Navisworks Tool Path**: The full path to `FileToolsTaskRunner.exe`.
    -   **Default Download Path**: The folder where Revit files will be downloaded.
    -   **Default NWD Path**: The folder for consolidated `.nwd` output files.
4.  Click **Save**.

## How to Use

1.  **Connect to Server**: Click the **Connect** button to browse your configured Revit Server.
2.  **Browse Local Files**: Click the **Browse Local** button to open a folder dialog and select a local directory containing `.rvt` files.
3.  **Select Files**:
    -   Use the **Download** checkbox to select files to be downloaded from the server.
    -   Use the **Convert** checkbox to select files for Navisworks conversion. For local files, only the convert option is available.
4.  **Start Processing**: Click the **Start Processing** button to begin the download and/or conversion process.
5.  **Monitor Progress**: A progress window will appear, showing the status of each file operation.
6.  **View Logs**: For detailed PowerShell command logs, click the **Monitor** button.

## License

This project is licensed under the MIT License. See the [LICENSE](LICENSE) file for details.
