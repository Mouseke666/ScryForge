using System.IO.Abstractions;
using Microsoft.Extensions.Logging;
using ScryForge.Services.Interfaces;

namespace ScryForge.Services
{
    /// <summary>
    /// Service to clean up a directory by deleting all files and subdirectories, 
    /// optionally excluding a single subfolder.
    /// </summary>
    public class CleanupService(ILogger<CleanupService> logger, IFileSystem fileSystem) : ICleanupService
    {
        /// <summary>
        /// Cleans the specified directory by deleting all files and subdirectories, 
        /// optionally excluding one subfolder.
        /// </summary>
        /// <param name="path">The directory to clean.</param>
        /// <param name="excludeSubfolder">Optional name of a subfolder to exclude from deletion.</param>
        /// <param name="ct">Cancellation token to cancel the operation.</param>
        /// <exception cref="ArgumentException">Thrown if <paramref name="path"/> is null, empty, or whitespace.</exception>
        public async Task<bool> CleanDirectoryAsync(string path, string? excludeSubfolder = null, CancellationToken ct = default)
        {
            bool success = true; // start met aanname dat alles goed gaat
            ct.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(path))
                throw new ArgumentException("Path must not be null, empty or whitespace.", nameof(path));

            if (!fileSystem.Directory.Exists(path))
            {
                try
                {
                    ct.ThrowIfCancellationRequested();
                    fileSystem.Directory.CreateDirectory(path);
                    logger.LogDebug("Created missing directory: {Path}", path);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    logger.LogWarning(ex, "Could not create directory: {Path}", path);
                    success = false;
                }
                return success;
            }

            try
            {
                var files = fileSystem.Directory.GetFiles(path);
                var allDirectories = fileSystem.Directory.GetDirectories(path);

                var directoriesToDelete = allDirectories
                    .Where(dir => !string.Equals(
                        Path.GetFullPath(dir).TrimEnd(Path.DirectorySeparatorChar),
                        Path.Combine(path, excludeSubfolder ?? string.Empty).TrimEnd(Path.DirectorySeparatorChar),
                        StringComparison.OrdinalIgnoreCase));

                foreach (var file in files)
                {
                    ct.ThrowIfCancellationRequested();
                    if (!DeleteFile(file)) success = false;
                }

                foreach (var dir in directoriesToDelete)
                {
                    ct.ThrowIfCancellationRequested();
                    if (!DeleteDirectory(dir)) success = false;
                }
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("Cleanup cancelled for directory: {Path}", path);
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error during cleanup of: {Path}", path);
                success = false;
            }

            return success;
        }

        private bool DeleteFile(string filePath)
        {
            try
            {
                fileSystem.File.Delete(filePath);
                return true;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                logger.LogWarning(ex, "Could not delete file (possibly in use): {File}", filePath);
                return false;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error deleting file: {File}", filePath);
                return false;
            }
        }

        private bool DeleteDirectory(string directoryPath)
        {
            try
            {
                fileSystem.Directory.Delete(directoryPath, recursive: true);
                return true;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                logger.LogWarning(ex, "Could not delete directory (possibly in use): {Dir}", directoryPath);
                return false;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unexpected error deleting directory: {Dir}", directoryPath);
                return false;
            }
        }

    }
}