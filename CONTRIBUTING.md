# Contributing to ArmaServerManager

Thank you for your interest in contributing to ArmaServerManager! This document provides guidelines and instructions for contributing.

## Code of Conduct

- Be respectful and inclusive
- Provide constructive feedback
- Focus on what is best for the community
- Show empathy towards other community members

## How to Contribute

### Reporting Bugs

1. Check if the bug has already been reported in [Issues](https://github.com/itskempf/ArmaServerManager/issues)
2. If not, create a new issue with:
   - Clear title and description
   - Steps to reproduce
   - Expected vs actual behavior
   - Screenshots if applicable
   - System information (OS, .NET version)
   - Log files from `Data/Logs/manager.log`

### Suggesting Features

1. Check existing issues for similar suggestions
2. Create a new issue with:
   - Clear description of the feature
   - Use cases and benefits
   - Possible implementation approach
   - Any relevant examples or mockups

### Pull Requests

1. **Fork the repository**
   ```bash
   git clone https://github.com/itskempf/ArmaServerManager.git
   cd ArmaServerManager
   ```

2. **Create a feature branch**
   ```bash
   git checkout -b feature/your-feature-name
   ```

3. **Make your changes**
   - Follow existing code style
   - Add comments for complex logic
   - Update documentation if needed

4. **Test your changes**
   - Build the project
   - Test all affected functionality
   - Ensure no regressions

5. **Commit your changes**
   ```bash
   git add .
   git commit -m "Add: Brief description of changes"
   ```

6. **Push to your fork**
   ```bash
   git push origin feature/your-feature-name
   ```

7. **Create Pull Request**
   - Provide clear description
   - Reference related issues
   - List changes made
   - Include screenshots if UI changes

## Development Guidelines

### Code Style

- Use C# naming conventions
- Follow MVVM pattern
- Use async/await for I/O operations
- Add XML documentation for public APIs
- Keep methods focused and concise

### Architecture

- **Core/**: Business logic and services
- **UI/**: User interface (Pages and ViewModels)
- **Models/**: Data models
- Services should be registered in `App.xaml.cs`
- Use dependency injection

### Commit Messages

Use clear, descriptive commit messages:
- `Add: New feature description`
- `Fix: Bug description`
- `Update: What was updated`
- `Refactor: What was refactored`
- `Docs: Documentation changes`

### Testing

- Test on Windows 10 and Windows 11
- Test with actual Arma 3 servers
- Test with real Workshop mods
- Verify SteamCMD integration
- Check for memory leaks

## Project Setup

### Prerequisites

- Visual Studio 2022
- .NET 8.0 SDK
- Windows App SDK
- SteamCMD (for testing)

### Building

1. Open `ArmaServerManager.sln` in Visual Studio
2. Restore NuGet packages
3. Build solution (Ctrl+Shift+B)
4. Run (F5)

### Debugging

- Set breakpoints in Visual Studio
- Check `Data/Logs/manager.log` for runtime logs
- Use Debug output window for diagnostics

## Areas for Contribution

### High Priority
- Unit tests for core services
- Integration tests
- Performance optimizations
- Bug fixes

### Medium Priority
- UI improvements
- Additional features
- Documentation improvements
- Code refactoring

### Low Priority
- Localization
- Themes
- Plugin examples
- Sample configurations

## Questions?

If you have questions about contributing:
- Open a discussion in Issues
- Check existing documentation
- Review code comments

## Recognition

Contributors will be recognized in:
- README.md
- Release notes
- Project documentation

Thank you for contributing to ArmaServerManager! 🎉
