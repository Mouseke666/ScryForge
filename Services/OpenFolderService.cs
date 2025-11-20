using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace ScryForge.Services
{
    public class OpenFolderService
    {
        private readonly ILogger<OpenFolderService> _logger;

        public OpenFolderService(ILogger<OpenFolderService> logger)
        {
            _logger = logger;
        }

        public void OpenFolder(string? path = null)
        {
            var folder = string.IsNullOrWhiteSpace(path)
                ? AppConfig.BasePath
                : path;

            if (Directory.Exists(folder))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = folder,
                    UseShellExecute = true
                });
            }
            else
            {
                _logger.LogWarning("Folder does not exist: {Folder}", folder);
            }
        }
    }
}
