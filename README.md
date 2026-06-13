# UniDesk

<p align="center">
  <img src="images/unidesk-hero.png" alt="UniDesk hero" width="900">
</p>

<p align="center">
  <b>A lightweight Windows desktop sidebar for time, weather, hardware status, shortcuts, todos, and personal desktop workflows.</b>
</p>

<p align="center">
  English | <a href="README.zh-CN.md">简体中文</a>
</p>

<p align="center">
  <a href="#about">About</a> ·
  <a href="#credits">Credits</a> ·
  <a href="#features">Features</a> ·
  <a href="#screenshots">Screenshots</a> ·
  <a href="#install">Install</a> ·
  <a href="#build">Build</a>
</p>

## About

UniDesk is a compact desktop sidebar for Windows. It stays on the edge of your desktop and keeps daily information, quick actions, lightweight hardware status, and todo items in one themeable panel.

The goal is to provide a calm personal desktop center: always visible when needed, easy to collapse when not needed, flexible enough for different screen sizes, and simple enough to run as a daily utility.

## Credits

UniDesk is developed based on [Happyeveryweek/LumiDesk](https://github.com/Happyeveryweek/LumiDesk). Thanks to the original author for the idea, foundation, and implementation of the desktop widget experience.

This project keeps the spirit of LumiDesk while adding project rebranding, integrated hardware monitoring, network speed display, updated layout, panel personalization, bilingual documentation, and installer packaging.

## Features

- **Time & Weather**: clock, date, lunar date, weather, humidity, air quality, and temperature range.
- **Hardware Monitor**: CPU, memory, GPU, temperature, and real-time network RX/TX speed.
- **Quick Shortcuts**: pin apps, folders, and files for one-click access.
- **Todo List**: manage tasks with priority, due date, completion state, backup, and restore.
- **Built-in Calendar**: open a desktop calendar panel with solar and lunar dates.
- **Personalized Panel**: adjust display title, theme colors, transparency, width, height, font size, topmost, lock, collapse, and shortcut count.
- **Weather API Settings**: configure weather API host and key from the settings panel.
- **Local-first Data**: settings, shortcuts, and todos are stored locally.

## Screenshots

### Main Panel

<p align="center">
  <img src="images/unidesk-main-panel.png" alt="UniDesk main panel" width="360">
</p>

### Personalization Settings

<p align="center">
  <img src="images/unidesk-settings-panel.png" alt="UniDesk personalization settings" width="360">
</p>

### System & Weather API Settings

<p align="center">
  <img src="images/unidesk-settings-system.png" alt="UniDesk system and weather API settings" width="360">
</p>

### Calendar

<p align="center">
  <img src="images/unidesk-calendar-panel.png" alt="UniDesk calendar panel" width="860">
</p>

## Hardware Monitor

The hardware monitor is integrated directly into the main UniDesk panel, between weather and shortcuts. It follows the same theme, transparency, width, and panel settings as the rest of the app.

Data sources vary by device and installed drivers:

- CPU usage comes from Windows performance counters.
- Memory usage comes from Windows system memory status.
- CPU temperature is read from available hardware-monitoring providers when possible.
- AMD GPU usage and temperature are read from available driver or vendor data when possible.
- Network speed is calculated from active physical Ethernet and Wi-Fi adapters.

Some temperature fields may show `--` if the required driver, vendor component, or permission is unavailable.

## Location Note

Auto location uses the current network exit IP. It usually works well on normal home networks, but VPNs, proxy tools, company networks, and carrier routing may return a different city. Users can switch to a manually selected city in settings when needed.

## Install

Download the latest installer from [GitHub Releases](https://github.com/SuperDaddyV/UniDesk/releases/latest) and run:

```powershell
UniDesk_Setup_1.3.0.exe
```

System requirements:

- Windows 10 1903 or later
- Windows 11

## Build

### Requirements

- Visual Studio 2022 or JetBrains Rider
- .NET 9 SDK
- Windows 10 v1903+ or Windows 11
- Inno Setup 6, only required for building the installer

### Compile and Run

```powershell
git clone https://github.com/SuperDaddyV/UniDesk.git
cd UniDesk

dotnet restore
dotnet build --configuration Release
dotnet run --project UniDesk
```

### Publish

```powershell
dotnet publish .\UniDesk\UniDesk.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=false -o publish\win-x64
```

### Installer

```powershell
ISCC.exe .\UniDesk.iss
```

The installer is written to the `installer` directory.

## Tech Stack

| Technology | Usage |
| --- | --- |
| .NET 9 | Runtime and base framework |
| WPF | Windows desktop UI |
| Wpf.Ui | Windows 11 style controls |
| CommunityToolkit.Mvvm | MVVM helpers |
| Microsoft.Data.Sqlite | Local data storage |
| Hardcodet.NotifyIcon.Wpf | System tray icon |
| Inno Setup | Windows installer |

## Project Structure

```text
UniDesk/
├─ UniDesk/                # Main application
│  ├─ Controls/            # Custom controls
│  ├─ Helpers/             # Utility helpers
│  ├─ Models/              # Data models
│  ├─ Services/            # Business services
│  ├─ ViewModels/          # MVVM view models
│  ├─ Resources/           # Themes and resources
│  └─ icon/                # App icons and bundled assets
├─ UniDesk.Tests/          # Unit tests
├─ docs/                   # Documentation
├─ images/                 # README images
├─ installer-assets/       # Installer language assets
├─ installer/              # Generated installer output
├─ UniDesk.iss             # Inno Setup script
└─ README.md
```

## Data Compatibility

UniDesk stores new user data under `%LOCALAPPDATA%\UniDesk`. On startup, it can copy compatible legacy local data into the new UniDesk data directory so existing settings, tasks, shortcuts, theme choices, and cache files are preserved where possible.

## License

This project follows the repository license. Please also respect the license and copyright of the original [LumiDesk](https://github.com/Happyeveryweek/LumiDesk) project and its third-party dependencies.
