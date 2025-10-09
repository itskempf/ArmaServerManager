# ArmaServerManager

<div align="center">

![License](https://img.shields.io/badge/license-MIT-blue.svg)
![.NET](https://img.shields.io/badge/.NET-8.0-purple.svg)
![Platform](https://img.shields.io/badge/platform-Windows-blue.svg)

A modern WinUI 3 desktop application for managing Arma 3 dedicated servers with SteamCMD integration.

[Features](#features) • [Installation](#installation) • [Usage](#usage) • [Documentation](#documentation) • [Contributing](#contributing)

</div>

---

## 🎯 Features

### Server Management
- ✅ Install and configure multiple Arma 3 server instances
- ✅ Start, stop, and restart servers with one click
- ✅ Headless client support
- ✅ Real-time server status monitoring
- ✅ Custom command-line parameters
- ✅ Automatic server configuration generation

### Mod Management
- ✅ Download Steam Workshop mods via SteamCMD
- ✅ Local mod support
- ✅ Mod verification and validation
- ✅ Mod size and key information
- ✅ Enable/disable mods per server

### Preset System
- ✅ Create and manage mod presets
- ✅ Import presets from Arma 3 Launcher (HTML)
- ✅ Export presets for sharing
- ✅ Quick preset switching

### Monitoring & Alerts
- ✅ Real-time CPU and RAM monitoring
- ✅ Per-server resource tracking
- ✅ Configurable alert thresholds
- ✅ Historical data visualization
- ✅ System uptime tracking

### Automation
- ✅ Scheduled server restarts
- ✅ Automatic mod updates
- ✅ Scheduled backups
- ✅ Task persistence across restarts

### Backup & Restore
- ✅ One-click server backups
- ✅ Safe restore with pre-restore backup
- ✅ Automatic old backup cleanup
- ✅ Compressed ZIP backups

### Additional Features
- ✅ Modern Fluent Design UI
- ✅ Dark/Light theme support
- ✅ Comprehensive logging
- ✅ Plugin system for extensibility
- ✅ Secure credential storage (DPAPI)
- ✅ Profile management

## 📋 Requirements

- **OS**: Windows 10 (1809+) or Windows 11
- **Runtime**: .NET 8.0 Runtime
- **SteamCMD**: Required for server/mod management
- **RAM**: 4GB minimum (8GB recommended)
- **Disk**: 50GB+ free space (for servers and mods)

## 🚀 Installation

### Option 1: Download Release (Recommended)
1. Download the latest release from [Releases](https://github.com/itskempf/ArmaServerManager/releases)
2. Extract the ZIP file
3. Run `ArmaServerManager.exe`

### Option 2: Build from Source
1. Install [Visual Studio 2022](https://visualstudio.microsoft.com/) with:
   - .NET Desktop Development workload
   - Windows App SDK
2. Clone the repository:
   ```bash
   git clone https://github.com/itskempf/ArmaServerManager.git
   cd ArmaServerManager
   ```
3. Open `ArmaServerManager.sln` in Visual Studio
4. Restore NuGet packages
5. Build and run (F5)

### SteamCMD Setup
1. Download SteamCMD: https://steamcdn-a.akamaihd.net/client/installer/steamcmd.zip
2. Extract to a folder (e.g., `C:\SteamCMD`)
3. Run `steamcmd.exe` once to initialize
4. Configure the path in ArmaServerManager Settings

## 📖 Usage

### First Time Setup
1. Launch ArmaServerManager
2. Go to **Settings**
3. Set **SteamCMD Path** (e.g., `C:\SteamCMD\steamcmd.exe`)
4. Directories will auto-configure based on SteamCMD location
5. Save settings

### Creating a Server
1. Go to **Servers** page
2. Click **Add Server**
3. Configure server settings:
   - Name
   - Port (default: 2302)
   - Max Players
   - Passwords
4. Click **Install Server** to download Arma 3 server files
5. Click **Start** to launch the server

### Installing Mods
1. Go to **Mods** page
2. Enter Workshop ID (e.g., `450814997` for CBA_A3)
3. Click **Install**
4. Wait for download to complete
5. Enable mods for your servers

### Creating Presets
1. Go to **Presets** page
2. Click **New Preset**
3. Add Workshop IDs
4. Save preset
5. Apply to servers or export for sharing

### Monitoring
1. Go to **Dashboard**
2. View real-time CPU/RAM usage
3. Monitor server status
4. Check system uptime
5. Review recent logs

## 📁 Project Structure

```
ArmaServerManager/
├── Core/                    # Business logic
│   ├── ServerManager.cs     # Server lifecycle management
│   ├── ModManager.cs        # Mod installation & management
│   ├── SteamCMDHandler.cs   # SteamCMD integration
│   ├── ConfigManager.cs     # Configuration generation
│   ├── PresetManager.cs     # Preset management
│   ├── ResourceMonitor.cs   # System monitoring
│   ├── BackupService.cs     # Backup & restore
│   ├── SchedulerService.cs  # Task scheduling
│   ├── UpdateService.cs     # Update management
│   └── ...                  # Other services
├── UI/
│   ├── Pages/              # XAML pages
│   └── ViewModels/         # MVVM ViewModels
├── Models/                 # Data models
├── Assets/                 # Resources & themes
└── Data/                   # Application data (gitignored)
```

## 🛠️ Technologies

- **Framework**: .NET 8.0
- **UI**: WinUI 3 (Windows App SDK)
- **Architecture**: MVVM
- **DI**: Microsoft.Extensions.DependencyInjection
- **Logging**: Microsoft.Extensions.Logging
- **Charts**: LiveChartsCore
- **Dialogs**: Ookii.Dialogs
- **Monitoring**: System.Diagnostics.PerformanceCounter

## 📚 Documentation

- [Quick Start Guide](ArmaServerManager/QUICK_START_GUIDE.md)
- [Core Services Documentation](ArmaServerManager/CORE_SERVICES_IMPLEMENTATION.md)
- [Build Status](ArmaServerManager/BUILD_STATUS.md)
- [Verification Checklist](ArmaServerManager/VERIFICATION_CHECKLIST.md)

## 🤝 Contributing

Contributions are welcome! Please feel free to submit a Pull Request.

1. Fork the repository
2. Create your feature branch (`git checkout -b feature/AmazingFeature`)
3. Commit your changes (`git commit -m 'Add some AmazingFeature'`)
4. Push to the branch (`git push origin feature/AmazingFeature`)
5. Open a Pull Request

## 📝 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## 🙏 Acknowledgments

- Bohemia Interactive for Arma 3
- Valve for SteamCMD
- Microsoft for WinUI 3
- The Arma community

## 📧 Support

If you encounter any issues or have questions:
- Open an [Issue](https://github.com/itskempf/ArmaServerManager/issues)
- Check existing documentation
- Review logs in `Data/Logs/manager.log`

## 🗺️ Roadmap

- [ ] Unit tests
- [ ] Integration tests
- [ ] Auto-update functionality
- [ ] Cloud backup support
- [ ] Multi-language support
- [ ] Server performance analytics
- [ ] Discord integration
- [ ] Web dashboard

---

<div align="center">

Made with ❤️ for the Arma community

</div>