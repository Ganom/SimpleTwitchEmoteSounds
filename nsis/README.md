# NSIS Windows installer

> [!NOTE]
> When creating the program binary add the constant `CUSTOM_FEATURE_INSTALLED` (`-p:DefineConstants="CUSTOM_FEATURE_INSTALLED"'`) so that the Settings are stored in a separate application data directory.

## Features

- Installs application to `%LocalAppData%/SimpleTwitchEmoteSounds` and a program data directory in `%AppData%/SimpleTwitchEmoteSounds`
  - Meaning the installer does only need user level privileges
- Creates Windows start menu shortcuts to:
  - Start the binary executable
  - Open the `Settings` directory
  - Uninstall the application (Remove all files, optionally remove settings via a dialog)
- Registers Windows registry keys to:
  - List it as installed application
  - List uninstall option to installed application
  - Recognize existing previous installations and automatically uninstall them before installing the current (new) version
- Optionally adds a binary executable desktop shortcut if user checks box
- After a successful install it starts the program automatically

## Setup

1. Install [NSIS](https://nsis.sourceforge.io/Download) (e.g. `winget install -e --id NSIS.NSIS`)
2. Enable the command `makensis` in the terminal by adding `C:\Program Files (x86)\NSIS\Bin` to the user environment `PATH` variable or using the full path

## Build

> [!IMPORTANT]
> This installer requires a built version of the program binary: `../publish/SimpleTwitchEmoteSounds.exe`

```ps1
# This creates ../publish/SimpleTwitchEmoteSounds.exe
dotnet publish ../SimpleTwitchEmoteSounds/SimpleTwitchEmoteSounds.csproj -o ../publish -r win-x64 -c Release -p:PublishSingleFile=true -p:DebugType=none -p:PublishReadyToRun=false -p:IncludeNativeLibrariesForSelfExtract=true --self-contained false -p:DefineConstants="CUSTOM_FEATURE_INSTALLED"
# This creates ../publish/SimpleTwitchEmoteSounds_installer.exe
& "C:\Program Files (x86)\NSIS\Bin\makensis" windows_installer.nsi
# Alternative if makensis is found in the PATH:
makensis windows_installer.nsi
```

## Registry Keys

To check the added/removed registry entries open the `Registry Editor` (`regedit.exe`) and search for:

- Install directory: `Computer\HKEY_CURRENT_USER\Software\SimpleTwitchEmoteSounds`
- (Un)Install information: `Computer\HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Uninstall\SimpleTwitchEmoteSounds`
