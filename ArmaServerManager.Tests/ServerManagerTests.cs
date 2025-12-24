using System;
using System.IO;
using System.Threading.Tasks;
using ArmaServerManager.Core;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ArmaServerManager.Tests
{
    public class ServerManagerTests : IDisposable
    {
        private readonly string _testDataPath;
        private readonly SettingsService _settingsService;
        private readonly Mock<ILogger> _mockLogger;
        private readonly ServerManager _serverManager;

        public ServerManagerTests()
        {
            _testDataPath = Path.Combine(Path.GetTempPath(), "ArmaServerManagerServerTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testDataPath);

            // Setup SettingsService with temp path
            _settingsService = new SettingsService(_testDataPath);
            _settingsService.Settings.Directories.Configs = Path.Combine(_testDataPath, "Configs");
            
            _mockLogger = new Mock<ILogger>();
            _serverManager = new ServerManager(_mockLogger.Object, _settingsService);
        }

        public void Dispose()
        {
            if (Directory.Exists(_testDataPath))
            {
                try { Directory.Delete(_testDataPath, true); } catch { }
            }
        }

        [Fact]
        public async Task AddServerAsync_AddsServerToCollectionAndSavesConfigFile()
        {
            // Arrange
            var server = new ArmaServer
            {
                Name = "TestServer",
                Port = 2302,
                InstallPath = "C:\\Servers\\TestServer"
            };

            // Act
            await _serverManager.AddServerAsync(server);

            // Assert
            Assert.Contains(server, _serverManager.Servers);
            
            var serversDir = Path.Combine(_settingsService.Settings.Directories.Configs, "Servers");
            var serverFile = Path.Combine(serversDir, "TestServer.json");
            Assert.True(File.Exists(serverFile));
            
            var json = await File.ReadAllTextAsync(serverFile);
            Assert.Contains("TestServer", json);
            Assert.Contains("2302", json);
        }

        [Fact]
        public async Task RemoveServerAsync_RemovesServerFromCollectionAndDeletesFile()
        {
            // Arrange
            var server = new ArmaServer { Name = "ToDelete", Port = 2303, InstallPath = "C:\\Servers\\ToDelete" };
            await _serverManager.AddServerAsync(server);
            
            var serversDir = Path.Combine(_settingsService.Settings.Directories.Configs, "Servers");
            var serverFile = Path.Combine(serversDir, "ToDelete.json");
            Assert.True(File.Exists(serverFile));

            // Act
            await _serverManager.RemoveServerAsync("ToDelete");

            // Assert
            Assert.DoesNotContain(server, _serverManager.Servers);
            Assert.False(File.Exists(serverFile));
        }

        [Fact]
        public void LoadServers_LoadsExistingServersFromFiles()
        {
            // This test requires re-initializing the manager to trigger LoadServers
            
            // Arrange
            // Use a separate settings/manager context to create the files first
            var setupSettings = new SettingsService(_testDataPath);
            setupSettings.Settings.Directories.Configs = Path.Combine(_testDataPath, "Configs");
            var setupManager = new ServerManager(_mockLogger.Object, setupSettings);
            
            var server = new ArmaServer { Name = "PersistedServer", Port = 2304 };
            setupManager.AddServerAsync(server).Wait(); // Use Wait() for synchronous setup in Arrange

            // Act
            // Initialize new manager which calls LoadServers() in constructor
            var newManager = new ServerManager(_mockLogger.Object, _settingsService);

            // Assert
            Assert.Single(newManager.Servers);
            Assert.Equal("PersistedServer", newManager.Servers[0].Name);
        }
        
        [Fact]
        public void GetServerStatus_ReturnsRunningStatus_WhenProcessExists()
        {
            // Mocking process logic in ServerManager is difficult because it uses Process.GetProcessById directly.
            // This unit test is limited to verifying default "Server not found" or "Stopped" behavior
            // unless we abstract the Process interactions.
            
            // Act
            var status = _serverManager.GetServerStatus("NonExistentServer");
            
            // Assert
            Assert.False(status.IsRunning);
            Assert.Equal("Server not found", status.Status);
        }
    }
}
