using System.IO.Abstractions;
using Microsoft.Extensions.Logging;
using ScryForge.Services.Interfaces;

namespace ScryForge.Services
{
    public class CleanupService(ILogger<CleanupService> logger, IFileSystem fileSystem) : ICleanupService
    {
        private readonly ILogger<CleanupService> _logger = logger;
        private readonly IFileSystem _fileSystem = fileSystem;

        public async Task CleanDirectoryAsync(string path, string? excludeSubfolder = null, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(path)) return;

            if (!_fileSystem.Directory.Exists(path))
            {
                try
                {
                    await Task.Run(() => _fileSystem.Directory.CreateDirectory(path), ct);
                    _logger.LogDebug("Created missing directory: {Path}", path);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    _logger.LogWarning(ex, "Could not create directory: {Path}", path);
                }
                return;
            }

            try
            {
                var files = _fileSystem.Directory.GetFiles(path);
                var allDirectories = _fileSystem.Directory.GetDirectories(path);

                var directoriesToDelete = allDirectories
                    .Where(dir => !string.Equals(
                        _fileSystem.Path.GetFileName(dir),
                        excludeSubfolder,
                        StringComparison.OrdinalIgnoreCase));

                var fileTasks = files.Select(file => DeleteFileAsync(file, ct));
                var dirTasks = directoriesToDelete.Select(dir => DeleteDirectoryAsync(dir, ct));

                await Task.WhenAll(fileTasks.Concat(dirTasks));
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Cleanup cancelled for directory: {Path}", path);
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unexpected error during cleanup of: {Path}", path);
            }
        }

        private async Task DeleteFileAsync(string filePath, CancellationToken ct = default)
        {
            try
            {
                await Task.Run(() => _fileSystem.File.Delete(filePath), ct);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _logger.LogWarning(ex, "Could not delete file (possibly in use): {File}", filePath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unexpected error deleting file: {File}", filePath);
            }
        }

        private async Task DeleteDirectoryAsync(string directoryPath, CancellationToken ct = default)
        {
            try
            {
                await Task.Run(() => _fileSystem.Directory.Delete(directoryPath, recursive: true), ct);
            }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                _logger.LogWarning(ex, "Could not delete directory (possibly in use): {Dir}", directoryPath);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Unexpected error deleting directory: {Dir}", directoryPath);
            }
        }
    }
}