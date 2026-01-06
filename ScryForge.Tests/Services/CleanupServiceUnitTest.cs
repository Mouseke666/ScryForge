using Moq;
using ScryForge.Services;
using System.IO.Abstractions;
using Microsoft.Extensions.Logging;
using System.IO.Abstractions.TestingHelpers;

namespace ScryForge.Tests.Services
{
    public class CleanupServiceUnitTest
    {
        [Fact]
        public async Task CleanDirectoryAsync_DeletesFilesAndDirectoriesExceptExcluded()
        {
            var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { @"C:\TestDir\File1.txt", new MockFileData("Hello") },
                { @"C:\TestDir\File2.txt", new MockFileData("World") },
                { @"C:\TestDir\KeepMe\File3.txt", new MockFileData("Keep") }
            });

            var mockLogger = Mock.Of<ILogger<CleanupService>>();

            var service = new CleanupService(mockLogger, mockFileSystem);

            await service.CleanDirectoryAsync(@"C:\TestDir", "KeepMe");

            Assert.False(mockFileSystem.FileExists(@"C:\TestDir\File1.txt"));
            Assert.False(mockFileSystem.FileExists(@"C:\TestDir\File2.txt"));

            Assert.True(mockFileSystem.Directory.Exists(@"C:\TestDir\KeepMe"));
            Assert.True(mockFileSystem.FileExists(@"C:\TestDir\KeepMe\File3.txt"));
        }

        [Theory]
        [InlineData(null)]
        [InlineData("")]
        [InlineData("   ")]
        public async Task CleanDirectoryAsync_ThrowsArgumentException_WhenPathIsNullOrWhiteSpace(string? invalidPath)
        {
            var mockFileSystem = new MockFileSystem();
            var mockLogger = Mock.Of<ILogger<CleanupService>>();
            var service = new CleanupService(mockLogger, mockFileSystem);

            var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
                service.CleanDirectoryAsync(invalidPath!));

            Assert.Equal("path", ex.ParamName);
        }

        [Fact]
        public async Task CleanDirectoryAsync_CreatesMissingDirectory_AndLogsDebug()
        {
            var path = @"C:\TestDir";

            var mockFileSystem = new MockFileSystem();
            var loggerMock = new Mock<ILogger<CleanupService>>();

            var service = new CleanupService(loggerMock.Object, mockFileSystem);

            await service.CleanDirectoryAsync(path);

            Assert.True(mockFileSystem.Directory.Exists(path));

            loggerMock.Verify(x => x.Log(LogLevel.Debug, It.IsAny<EventId>(), It.Is<It.IsAnyType>((v, t) => v != null && v.ToString()!.Contains("Created missing directory")), null, It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        [Fact]
        public async Task CleanDirectoryAsync_WhenCancelled_LogsInformationAndRethrows()
        {
            var path = @"C:\TestDir";

            var mockFileSystem = new Mock<IFileSystem>();
            mockFileSystem.Setup(fs => fs.Directory.Exists(path)).Returns(true);
            mockFileSystem.Setup(fs => fs.Directory.GetFiles(path))
                .Throws(new OperationCanceledException());

            var loggerMock = new Mock<ILogger<CleanupService>>();
            var service = new CleanupService(loggerMock.Object, mockFileSystem.Object);

            await Assert.ThrowsAsync<OperationCanceledException>(() => service.CleanDirectoryAsync(path));

            loggerMock.Verify(x => x.Log(LogLevel.Information, It.IsAny<EventId>(), It.Is<It.IsAnyType>((v, t) => v != null && v.ToString()!.Contains("Cleanup cancelled for directory")), null, It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

        [Fact]
        public async Task CleanDirectoryAsync_WhenUnexpectedException_LogsError()
        {
            var path = @"C:\TestDir";

            var mockFileSystem = new Mock<IFileSystem>();
            mockFileSystem.Setup(fs => fs.Directory.Exists(path)).Returns(true);
            mockFileSystem.Setup(fs => fs.Directory.GetFiles(path))
                .Throws(new InvalidOperationException("Test exception"));

            var loggerMock = new Mock<ILogger<CleanupService>>();
            var service = new CleanupService(loggerMock.Object, mockFileSystem.Object);

            await service.CleanDirectoryAsync(path);

            loggerMock.Verify(x => x.Log(LogLevel.Error, It.IsAny<EventId>(), It.Is<It.IsAnyType>((v, t) => v != null && v.ToString()!.Contains("Unexpected error during cleanup")), It.Is<Exception>(ex => ex is InvalidOperationException), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
        }

    }
}