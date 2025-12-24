using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using ArmaServerManager.Core;
using System.IO;
using System.Threading.Tasks;
using System;

namespace ArmaServerManager.Tests
{
    public class ConfigManagerTests : IDisposable
    {
        private readonly string _testConfigsPath;
        private readonly Mock<ILogger> _loggerMock;
        private readonly ConfigManager _configManager;

        public ConfigManagerTests()
        {
            _testConfigsPath = Path.Combine(Path.GetTempPath(), "ArmaServerManager_Tests", Guid.NewGuid().ToString());
            _loggerMock = new Mock<ILogger>();
            _configManager = new ConfigManager(_testConfigsPath, _loggerMock.Object);
        }

        public void Dispose()
        {
            if (Directory.Exists(_testConfigsPath))
            {
                try { Directory.Delete(_testConfigsPath, true); } catch { }
            }
        }

        [Fact]
        public void GenerateDefaultConfig_ReturnsCorrectDefaults()
        {
            // Arrange
            var serverName = "TestServer";
            var installPath = @"C:\TestPath";

            // Act
            var server = _configManager.GenerateDefaultConfig(serverName, installPath);

            // Assert
            Assert.Equal(serverName, server.Name);
            Assert.Equal(installPath, server.InstallPath);
            Assert.Equal(Path.Combine(installPath, "server.cfg"), server.ConfigPath);
            Assert.Equal(2302, server.Port);
            Assert.Equal(64, server.MaxPlayers);
            Assert.Equal("admin123", server.AdminPassword);
        }

        [Fact]
        public async Task SaveConfigAsync_CreatesConfigFile()
        {
            // Arrange
            var server = new ArmaServer
            {
                Name = "TestServer",
                InstallPath = Path.Combine(_testConfigsPath, "ServerInstall"), // Fake install path
                Port = 2302
            };
            
            // Ensure InstallPath exists for server.cfg writing
            Directory.CreateDirectory(server.InstallPath);

            // Act
            await _configManager.SaveConfigAsync(server);

            // Assert
            var configJsonPath = Path.Combine(_testConfigsPath, "TestServer.json");
            Assert.True(File.Exists(configJsonPath), "Server JSON config file was not created.");
            
            var serverCfgPath = Path.Combine(server.InstallPath, "server.cfg");
            Assert.True(File.Exists(serverCfgPath), "server.cfg was not created.");
        }

        [Fact]
        public async Task LoadConfigAsync_ReturnsCorrectConfig()
        {
            // Arrange
            var serverName = "LoadedServer";
            var server = new ArmaServer
            {
                Name = serverName,
                Port = 2402,
                MaxPlayers = 100
            };
            await _configManager.SaveConfigAsync(server);

            // Act
            var loadedServer = await _configManager.LoadConfigAsync(serverName);

            // Assert
            Assert.NotNull(loadedServer);
            Assert.Equal(server.Name, loadedServer.Name);
            Assert.Equal(server.Port, loadedServer.Port);
            Assert.Equal(server.MaxPlayers, loadedServer.MaxPlayers);
        }

        [Fact]
        public async Task LoadConfigAsync_ReturnsNull_WhenFileDoesNotExist()
        {
            // Act
            var result = await _configManager.LoadConfigAsync("NonExistentServer");

            // Assert
            Assert.Null(result);
        }
    }
}
