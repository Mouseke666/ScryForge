using Microsoft.Extensions.Logging;
using ScryForge.Services.Interfaces;

namespace ScryForge.Services;

public class CleanupService(ILogger<CleanupService> logger) : ICleanupService
{
    private readonly ILogger<CleanupService> _logger = logger;

    public async Task CleanDirectoryAsync(string path, string? excludeSubfolder = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        if (!Directory.Exists(path))
        {
            try
            {
                await Task.Run(() => Directory.CreateDirectory(path), ct);
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
            string[] files = Directory.GetFiles(path);
            string[] allDirectories = Directory.GetDirectories(path);

            string[] directoriesToDelete = [.. allDirectories
                .Where(dir => !string.Equals(
                    Path.GetFileName(dir),
                    excludeSubfolder,
                    StringComparison.OrdinalIgnoreCase))];

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
            await Task.Run(() => File.Delete(filePath), ct);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
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
            await Task.Run(() => Directory.Delete(directoryPath, recursive: true), ct);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
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