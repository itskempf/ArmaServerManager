using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ArmaServerManager.Core;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace ArmaServerManager.Tests
{
    public class ModManagerTests : IDisposable
    {
        private readonly string _testModsPath;
        private readonly Mock<ISteamCMDHandler> _mockSteamCmd;
        private readonly Mock<ILogger> _mockLogger;
        private readonly ModManager _modManager;

        public ModManagerTests()
        {
            _testModsPath = Path.Combine(Path.GetTempPath(), "ArmaServerManagerModTests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testModsPath);
            
            _mockSteamCmd = new Mock<ISteamCMDHandler>();
            _mockLogger = new Mock<ILogger>();
            
            _modManager = new ModManager(_testModsPath, _mockSteamCmd.Object, _mockLogger.Object);
        }

        public void Dispose()
        {
            if (Directory.Exists(_testModsPath))
            {
                try { Directory.Delete(_testModsPath, true); } catch { }
            }
        }

        [Fact]
        public async Task InstallModAsync_DownloadsModAndAddToCollection()
        {
            // Arrange
            var modId = "123456";
            _mockSteamCmd.Setup(x => x.DownloadModAsync(modId, _testModsPath))
                .ReturnsAsync(new SteamCmdResult { Success = true });

            // Act
            var result = await _modManager.InstallModAsync(modId);

            // Assert
            Assert.True(result);
            Assert.Contains(_modManager.Mods, m => m.WorkshopId == modId);
            _mockSteamCmd.Verify(x => x.DownloadModAsync(modId, _testModsPath), Times.Once);
        }

        [Fact]
        public async Task InstallModAsync_ReturnsFalse_WhenDownloadFails()
        {
            // Arrange
            var modId = "123456";
            _mockSteamCmd.Setup(x => x.DownloadModAsync(modId, _testModsPath))
                .ReturnsAsync(new SteamCmdResult { Success = false, Error = "Download failed" });

            // Act
            var result = await _modManager.InstallModAsync(modId);

            // Assert
            Assert.False(result);
            Assert.DoesNotContain(_modManager.Mods, m => m.WorkshopId == modId);
        }

        [Fact]
        public async Task RemoveModAsync_RemovesFromCollectionAndDeletesFile()
        {
            // Arrange
            var modId = "123456";
            // Pre-seed a mod
            _mockSteamCmd.Setup(x => x.DownloadModAsync(modId, _testModsPath))
                .ReturnsAsync(new SteamCmdResult { Success = true });
            await _modManager.InstallModAsync(modId);
            
            // Verify it exists in config
            var configPath = Path.Combine(_testModsPath, "mods.json");
            Assert.True(File.Exists(configPath));
            Assert.Contains(modId, await File.ReadAllTextAsync(configPath));

            // Act
            await _modManager.RemoveModAsync(modId);

            // Assert
            Assert.DoesNotContain(_modManager.Mods, m => m.WorkshopId == modId);
            // Verify removed from config
            Assert.DoesNotContain(modId, await File.ReadAllTextAsync(configPath));
        }

        [Fact]
        public void LoadInstalledMods_LoadsFromConfig()
        {
            // Arrange
            var modId = "987654";
            var existingMods = new[] { new ArmaMod { WorkshopId = modId, Name = "Test Mod" } };
            var json = System.Text.Json.JsonSerializer.Serialize(existingMods);
            File.WriteAllText(Path.Combine(_testModsPath, "mods.json"), json);
            
            // Re-initialize manager to trigger load
            var newManager = new ModManager(_testModsPath, _mockSteamCmd.Object, _mockLogger.Object);
            newManager.LoadInstalledMods();

            // Assert
            Assert.Single(newManager.Mods);
            Assert.Equal(modId, newManager.Mods[0].WorkshopId);
        }
    }
}
