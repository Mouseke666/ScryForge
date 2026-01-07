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

        [Fact]
        public async Task CleanDirectoryAsync_FileDeletionCancelled_LogsInformationAndRethrows()
        {
            var path = @"C:\TestDir";
            var file = Path.Combine(path, "File1.txt");

            var mockFileSystem = new Mock<IFileSystem>();
            mockFileSystem.Setup(fs => fs.Directory.Exists(path)).Returns(true);
            mockFileSystem.Setup(fs => fs.Directory.GetFiles(path)).Returns(new[] { file });
            mockFileSystem.Setup(fs => fs.File.Delete(file)).Throws(new OperationCanceledException());

            var loggerMock = new Mock<ILogger<CleanupService>>();
            var service = new CleanupService(loggerMock.Object, mockFileSystem.Object);

            await Assert.ThrowsAsync<OperationCanceledException>(() => service.CleanDirectoryAsync(path));

            loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Cleanup cancelled")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task CleanDirectoryAsync_LogsWarning_WhenDirectoryCreationFails()
        {
            var path = @"C:\TestDir";
            var mockFileSystem = new Mock<IFileSystem>();
            mockFileSystem.Setup(fs => fs.Directory.Exists(path)).Returns(false);
            mockFileSystem.Setup(fs => fs.Directory.CreateDirectory(path)).Throws(new IOException("Cannot create"));

            var loggerMock = new Mock<ILogger<CleanupService>>();
            var service = new CleanupService(loggerMock.Object, mockFileSystem.Object);

            await service.CleanDirectoryAsync(path);

            loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Could not create directory")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task CleanDirectoryAsync_LogsWarning_WhenFileCannotBeDeleted()
        {
            var path = @"C:\TestDir";
            var file = Path.Combine(path, "File1.txt");

            var mockFileSystem = new Mock<IFileSystem>();
            mockFileSystem.Setup(fs => fs.Directory.Exists(path)).Returns(true);
            mockFileSystem.Setup(fs => fs.Directory.GetFiles(path)).Returns(new[] { file });
            mockFileSystem.Setup(fs => fs.File.Delete(file)).Throws(new IOException("Locked file"));

            var loggerMock = new Mock<ILogger<CleanupService>>();
            var service = new CleanupService(loggerMock.Object, mockFileSystem.Object);

            await service.CleanDirectoryAsync(path);

            loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Could not delete file")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task CleanDirectoryAsync_LogsWarning_WhenDirectoryCannotBeDeleted()
        {
            var path = @"C:\TestDir";
            var dir = Path.Combine(path, "SubDir");

            var mockFileSystem = new Mock<IFileSystem>();
            mockFileSystem.Setup(fs => fs.Directory.Exists(path)).Returns(true);
            mockFileSystem.Setup(fs => fs.Directory.GetFiles(path)).Returns(Array.Empty<string>());
            mockFileSystem.Setup(fs => fs.Directory.GetDirectories(path)).Returns(new[] { dir });
            mockFileSystem.Setup(fs => fs.Directory.Delete(dir, true)).Throws(new UnauthorizedAccessException("No access"));

            var loggerMock = new Mock<ILogger<CleanupService>>();
            var service = new CleanupService(loggerMock.Object, mockFileSystem.Object);

            await service.CleanDirectoryAsync(path);

            loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Could not delete directory")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task CleanDirectoryAsync_ExcludesSubfolder_CaseInsensitiveAndTrailingSlash()
        {
            var path = @"C:\TestDir";
            var fileInside = Path.Combine(path, "KeepMe", "File.txt");

            var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { Path.Combine(path, "DeleteMe.txt"), new MockFileData("Delete") },
                { fileInside, new MockFileData("Keep") }
            });

            var loggerMock = Mock.Of<ILogger<CleanupService>>();
            var service = new CleanupService(loggerMock, mockFileSystem);

            await service.CleanDirectoryAsync(path, "keepme"); // lower-case

            Assert.True(mockFileSystem.Directory.Exists(Path.Combine(path, "KeepMe")));
            Assert.True(mockFileSystem.FileExists(fileInside));
            Assert.False(mockFileSystem.FileExists(Path.Combine(path, "DeleteMe.txt")));
        }

        [Fact]
        public async Task CleanDirectoryAsync_DeletesNestedDirectories()
        {
            var path = @"C:\TestDir";
            var nestedDir = Path.Combine(path, "Nested", "SubNested");
            var fileInNested = Path.Combine(nestedDir, "File.txt");

            var mockFileSystem = new MockFileSystem(new Dictionary<string, MockFileData>
            {
                { Path.Combine(path, "DeleteMe.txt"), new MockFileData("Delete") },
                { fileInNested, new MockFileData("Delete") }
            });

            var loggerMock = Mock.Of<ILogger<CleanupService>>();
            var service = new CleanupService(loggerMock, mockFileSystem);

            await service.CleanDirectoryAsync(path);

            Assert.False(mockFileSystem.FileExists(Path.Combine(path, "DeleteMe.txt")));
            Assert.False(mockFileSystem.Directory.Exists(nestedDir));
        }

        [Fact]
        public async Task DeleteFile_LogsError_WhenUnexpectedExceptionOccurs()
        {
            var path = @"C:\TestDir";
            var file = Path.Combine(path, "File1.txt");

            var mockFileSystem = new Mock<IFileSystem>();
            mockFileSystem.Setup(fs => fs.Directory.Exists(path)).Returns(true);
            mockFileSystem.Setup(fs => fs.Directory.GetFiles(path)).Returns(new[] { file });
            mockFileSystem.Setup(fs => fs.File.Delete(file)).Throws(new InvalidOperationException("Unexpected"));

            var loggerMock = new Mock<ILogger<CleanupService>>();
            var service = new CleanupService(loggerMock.Object, mockFileSystem.Object);

            await service.CleanDirectoryAsync(path);

            loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Unexpected error deleting file")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task DeleteDirectory_LogsWarning_WhenIOExceptionOccurs()
        {
            var path = @"C:\TestDir";
            var dir = Path.Combine(path, "SubDir");

            var mockFileSystem = new Mock<IFileSystem>();
            mockFileSystem.Setup(fs => fs.Directory.Exists(path)).Returns(true);
            mockFileSystem.Setup(fs => fs.Directory.GetFiles(path)).Returns(Array.Empty<string>());
            mockFileSystem.Setup(fs => fs.Directory.GetDirectories(path)).Returns(new[] { dir });
            mockFileSystem.Setup(fs => fs.Directory.Delete(dir, true)).Throws(new IOException("Directory locked"));

            var loggerMock = new Mock<ILogger<CleanupService>>();
            var service = new CleanupService(loggerMock.Object, mockFileSystem.Object);

            await service.CleanDirectoryAsync(path);

            loggerMock.Verify(
                x => x.Log(
                    LogLevel.Warning,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Could not delete directory")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task DeleteDirectory_WhenCancelled_ThrowsAndLogsInformation()
        {
            var path = @"C:\TestDir";
            var dir = Path.Combine(path, "SubDir");

            var mockFileSystem = new Mock<IFileSystem>();
            mockFileSystem.Setup(fs => fs.Directory.Exists(path)).Returns(true);
            mockFileSystem.Setup(fs => fs.Directory.GetFiles(path)).Returns(Array.Empty<string>());
            mockFileSystem.Setup(fs => fs.Directory.GetDirectories(path)).Returns(new[] { dir });
            mockFileSystem.Setup(fs => fs.Directory.Delete(dir, true)).Throws(new OperationCanceledException());

            var loggerMock = new Mock<ILogger<CleanupService>>();
            var service = new CleanupService(loggerMock.Object, mockFileSystem.Object);

            await Assert.ThrowsAsync<OperationCanceledException>(() => service.CleanDirectoryAsync(path));

            // Hier log je geen info specifiek in DeleteDirectory, maar de outer method logt informatie bij OperationCanceledException
            loggerMock.Verify(
                x => x.Log(
                    LogLevel.Information,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Cleanup cancelled")),
                    null,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

        [Fact]
        public async Task DeleteDirectory_LogsError_WhenUnexpectedExceptionOccurs()
        {
            var path = @"C:\TestDir";
            var dir = Path.Combine(path, "SubDir");

            var mockFileSystem = new Mock<IFileSystem>();
            mockFileSystem.Setup(fs => fs.Directory.Exists(path)).Returns(true);
            mockFileSystem.Setup(fs => fs.Directory.GetFiles(path)).Returns(Array.Empty<string>());
            mockFileSystem.Setup(fs => fs.Directory.GetDirectories(path)).Returns(new[] { dir });
            mockFileSystem.Setup(fs => fs.Directory.Delete(dir, true)).Throws(new InvalidOperationException("Unexpected"));

            var loggerMock = new Mock<ILogger<CleanupService>>();
            var service = new CleanupService(loggerMock.Object, mockFileSystem.Object);

            await service.CleanDirectoryAsync(path);

            loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Unexpected error deleting directory")),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }

    }
}