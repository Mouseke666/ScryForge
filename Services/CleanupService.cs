using Microsoft.Extensions.Logging;

namespace ScryForge.Services
{
    public class CleanupService
    {
        private readonly ILogger<CleanupService> _logger;

        public CleanupService(ILogger<CleanupService> logger)
        {
            _logger = logger;
        }

        public void CleanDirectory(string path, string? excludeSubfolder = null)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                return;
            }

            foreach (var file in Directory.GetFiles(path))
            {
                try
                {
                    File.Delete(file);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error deleting file {File}", file);
                }
            }

            foreach (var dir in Directory.GetDirectories(path))
            {
                var dirName = new DirectoryInfo(dir).Name;
                if (!string.Equals(dirName, excludeSubfolder, StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        Directory.Delete(dir, true);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error deleting subfolder {Dir}", dir);
                    }
                }
            }
        }

        public void DeleteFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return;
            }

            try
            {
                File.Delete(filePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting {File}", filePath);
            }
        }
    }
}