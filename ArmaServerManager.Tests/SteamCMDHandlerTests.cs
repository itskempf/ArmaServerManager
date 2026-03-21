using Xunit;
using ArmaServerManager.Core;
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.IO;
using System.Threading.Tasks;

namespace ArmaServerManager.Tests
{
    public class SteamCMDHandlerTests : IDisposable
    {
        private readonly string _tempDir;
        private readonly string _dummySteamCmdPath;
        private readonly Mock<ILogger> _mockLogger;

        public SteamCMDHandlerTests()
        {
            _tempDir = Path.Combine(Path.GetTempPath(), "SteamCMDHandlerTests_" + Guid.NewGuid());
            Directory.CreateDirectory(_tempDir);
            _dummySteamCmdPath = Path.Combine(_tempDir, "steamcmd.exe");
            File.WriteAllText(_dummySteamCmdPath, ""); // Create empty file
            _mockLogger = new Mock<ILogger>();
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, true);
            }
        }

        [Fact]
        public async Task DownloadModAsync_WithMaliciousModId_ShouldFailValidation()
        {
            var handler = new SteamCMDHandler(_dummySteamCmdPath, _mockLogger.Object);
            var maliciousModId = "123456 +quit"; // Attempting to inject commands
            var installPath = _tempDir;

            var result = await handler.DownloadModAsync(maliciousModId, installPath);

            Assert.False(result.Success);
            Assert.Contains("Invalid Mod ID", result.Error);
        }

        [Fact]
        public async Task DownloadModAsync_WithNonNumericModId_ShouldFailValidation()
        {
            var handler = new SteamCMDHandler(_dummySteamCmdPath, _mockLogger.Object);
            var maliciousModId = "not_a_number";
            var installPath = _tempDir;

            var result = await handler.DownloadModAsync(maliciousModId, installPath);

            Assert.False(result.Success);
            Assert.Contains("Invalid Mod ID", result.Error);
        }

        [Fact]
        public async Task InstallServerAsync_WithMaliciousPath_ShouldFailValidation()
        {
            var handler = new SteamCMDHandler(_dummySteamCmdPath, _mockLogger.Object);
            var maliciousPath = "C:\\Servers\" & echo hacked & \"";

            var result = await handler.InstallServerAsync(maliciousPath);

            Assert.False(result.Success);
            Assert.Contains("Invalid install path", result.Error);
        }

        [Fact]
        public async Task DownloadModAsync_WithValidModId_ShouldNotFailValidation()
        {
            var handler = new SteamCMDHandler(_dummySteamCmdPath, _mockLogger.Object);
            var validModId = "123456789";
            var installPath = _tempDir;

            var result = await handler.DownloadModAsync(validModId, installPath);

            // It might fail to execute because steamcmd.exe is empty, but it shouldn't be a validation error
            Assert.DoesNotContain("Invalid Mod ID", result.Error);
        }
    }
}
