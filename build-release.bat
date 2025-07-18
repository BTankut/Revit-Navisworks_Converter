@echo off
echo Building RVT to Navisworks Converter Release...
echo.

REM Clean previous builds
echo Cleaning previous builds...
dotnet clean -c Release
if errorlevel 1 goto error

REM Build Release
echo Building Release configuration...
dotnet build -c Release
if errorlevel 1 goto error

REM Publish
echo Publishing application...
dotnet publish -c Release -r win-x64 --self-contained false -o ./publish
if errorlevel 1 goto error

REM Copy additional files
echo Copying additional files...
copy LICENSING.md publish\
copy README.md publish\

REM Create zip
echo Creating release package...
powershell Compress-Archive -Path publish\* -DestinationPath RvtToNavisConverter_v2.1.0_Release.zip -Force

echo.
echo ========================================
echo Release build completed successfully!
echo Output: RvtToNavisConverter_v2.1.0_Release.zip
echo ========================================
goto end

:error
echo.
echo ========================================
echo ERROR: Build failed!
echo ========================================
exit /b 1

:end