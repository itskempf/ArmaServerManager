using System;
using System.IO;
using System.Security.Cryptography;
using ArmaServerManager.Core;
using Xunit;

namespace ArmaServerManager.Tests
{
    public class SteamAuthManagerTests : IDisposable
    {
        private readonly string _testDataPath;
        private readonly SteamAuthManager _authManager;

        public SteamAuthManagerTests()
        {
            _testDataPath = Path.Combine(Path.GetTempPath(), "ArmaServerManagerValueTests", Guid.NewGuid().ToString());
            _authManager = new SteamAuthManager(_testDataPath);
        }

        public void Dispose()
        {
            if (Directory.Exists(_testDataPath))
            {
                Directory.Delete(_testDataPath, true);
            }
        }

        [Fact]
        public void SaveCredentials_EncryptsAndSavesFile()
        {
            // Arrange
            var credentials = new SteamCredentials 
            { 
                Username = "testuser", 
                GuardToken = "token123", 
                RememberCredentials = true 
            };

            // Act
            _authManager.SaveCredentials(credentials);

            // Assert
            var authDir = Path.Combine(_testDataPath, "Auth");
            var authFile = Path.Combine(authDir, "steam_auth.dat");
            Assert.True(File.Exists(authFile));
            
            // Verify file is not plain text json
            var content = File.ReadAllText(authFile);
            Assert.DoesNotContain("testuser", content);
        }

        [Fact]
        public void LoadCredentials_ReturnsSavedCredentials()
        {
            // Arrange
            var originalCredentials = new SteamCredentials 
            { 
                Username = "testuser", 
                GuardToken = "token123", 
                RememberCredentials = true 
            };
            _authManager.SaveCredentials(originalCredentials);

            // Act
            var loadedCredentials = _authManager.LoadCredentials();

            // Assert
            Assert.NotNull(loadedCredentials);
            Assert.Equal(originalCredentials.Username, loadedCredentials.Username);
            Assert.Equal(originalCredentials.GuardToken, loadedCredentials.GuardToken);
            Assert.Equal(originalCredentials.RememberCredentials, loadedCredentials.RememberCredentials);
        }

        [Fact]
        public void LoadCredentials_ReturnsNullWhenFileDoesNotExist()
        {
            // Act
            var credentials = _authManager.LoadCredentials();

            // Assert
            Assert.Null(credentials);
        }

        [Fact]
        public void ClearCredentials_RemovesAuthFile()
        {
            // Arrange
            var credentials = new SteamCredentials();
            _authManager.SaveCredentials(credentials);
            var authFile = Path.Combine(_testDataPath, "Auth", "steam_auth.dat");
            Assert.True(File.Exists(authFile));

            // Act
            _authManager.ClearCredentials();

            // Assert
            Assert.False(File.Exists(authFile));
        }

        [Fact]
        public void LoadCredentials_ReturnsNullOnDecryptionFailure()
        {
            // Arrange
            var authDir = Path.Combine(_testDataPath, "Auth");
            Directory.CreateDirectory(authDir);
            var authFile = Path.Combine(authDir, "steam_auth.dat");
            // Write garbage data
            File.WriteAllText(authFile, "InvalidEncryptedData");

            // Act
            var result = _authManager.LoadCredentials();

            // Assert
            Assert.Null(result);
        }
    }
}
