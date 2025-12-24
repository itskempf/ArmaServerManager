# Arma Server Manager

A comprehensive, modern desktop application for managing Arma 3 Dedicated Servers. Built with **.NET 8** and **WinUI 3**, this tool simplifies the complexity of setting up, configuring, and maintaining Arma 3 servers and mods.

## 🚀 Features

*   **Server Management**: Easily start, stop, and restart Arma 3 server instances. Manage multiple server profiles with distinct configurations.
*   **Mod Management**: Integrated SteamCMD support for downloading and updating mods from the Steam Workshop.
*   **Preset System**: Create, import, and export mod presets. Fully compatible with Arma 3 Launcher HTML presets.
*   **Headless Client Support**: Seamlessly manage headless client instances for improved server performance.
*   **Resource Monitoring**: Real-time monitoring of server CPU and RAM usage with alert thresholds.
*   **Automated Updates**: Schedule updates for servers and mods to ensure everything is always up-to-date.
*   **Configuration Editor**: User-friendly interface for managing `server.cfg` and other settings.
*   **Backup & Restore**: built-in tools to backup server configurations and state.

## 🛠️ Technology Stack

*   **Framework**: .NET 8.0
*   **UI Library**: WinUI 3 (Windows App SDK)
*   **Architecture**: MVVM (Model-View-ViewModel) with `CommunityToolkit.Mvvm`
*   **Dependency Injection**: `Microsoft.Extensions.DependencyInjection`
*   **Logging**: `Microsoft.Extensions.Logging` (ILogger)
*   **Unit Testing**: xUnit & Moq

## 📦 Getting Started

### Prerequisites

*   **Windows 10 (1809+) or Windows 11**
*   **[Runtime]** [.NET 8 Desktop Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)
*   **[Runtime]** [Windows App SDK Runtime](https://learn.microsoft.com/en-us/windows/apps/windows-app-sdk/downloads)
*   **SteamCMD**: The application can download this automatically, or you can provide a path to an existing installation.

### Installation

1.  Download the latest release from the [Releases](https://github.com/itskempf/ArmaServerManager/releases) page.
2.  Extract the zip archive to a location of your choice (e.g., `C:\Apps\ArmaServerManager`).
3.  Run `ArmaServerManager.exe`.

### First Run Setup

1.  On first launch, navigate to **Settings**.
2.  Configure the **SteamCMD Path** (or let the app download it).
3.  Set up your **Steam Credentials** (required for downloading mods).
4.  Define your **Directories** for servers and mods.

## 💻 Development

### Building from Source

1.  Clone the repository:
    ```bash
    git clone https://github.com/itskempf/ArmaServerManager.git
    cd ArmaServerManager
    ```
2.  Open `ArmaServerManager.sln` in **Visual Studio 2022**.
3.  Ensure the `ArmaServerManager` (Package) project is the startup project.
4.  Build and Run (F5).

### Running Tests

The solution includes a comprehensive unit test suite in `ArmaServerManager.Tests`.

```bash
dotnet test
```

## 🤝 Contributing

Contributions are welcome! Please read [CONTRIBUTING.md](CONTRIBUTING.md) for details on our code of conduct and the process for submitting pull requests.

## 📄 License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

*Note: This project is not affiliated with Bohemia Interactive.*
