# PROJECT CONTEXT - ArmaServerManager

## WHAT WE ARE BUILDING
A **Windows desktop application** for managing Arma 3 dedicated servers using SteamCMD integration.

## TECHNOLOGY STACK
- **Framework**: .NET 8.0 WinUI 3 (Windows App SDK)
- **Architecture**: MVVM pattern with dependency injection
- **External Tool**: SteamCMD for server/mod downloads
- **Target Platform**: Windows 10/11 desktop only

## CORE FUNCTIONALITY
1. **Server Management**: Install, configure, start/stop Arma 3 dedicated servers
2. **Mod Management**: Download Steam Workshop mods via SteamCMD
3. **Preset System**: Create/import/export mod collections
4. **Monitoring**: Real-time server resource monitoring
5. **Automation**: Scheduled restarts, backups, updates

## PROJECT STATUS
- **Phase**: COMPLETION & BUG FIXING
- **Core Services**: 16 services fully implemented with real functionality
- **UI**: Complete MVVM ViewModels and XAML pages
- **Integration**: Real SteamCMD process execution
- **Repository**: Live on GitHub with CI/CD

## CRITICAL CONSTRAINTS
- **NO HALLUCINATION**: Only work with actual existing code
- **NO NEW FEATURES**: Project is feature-complete
- **FOCUS**: Bug fixes, compilation errors, code quality only
- **STEAMCMD REQUIRED**: All server/mod operations use SteamCMD

## KEY FILES
- `Core/ServerManager.cs` - Server lifecycle management
- `Core/ModManager.cs` - Workshop mod downloads
- `Core/SteamCMDHandler.cs` - SteamCMD process execution
- `UI/ViewModels/` - MVVM ViewModels for all pages
- `Models/` - Data models (ArmaServer, ArmaMod, ModPreset)

## CURRENT TASK
Maintaining code quality and fixing any compilation/runtime issues. Project is ready for end-user deployment.