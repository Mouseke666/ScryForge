using System.Diagnostics;
using Microsoft.Extensions.Logging;
using ScryForge.Services.Interfaces;

namespace ScryForge.Services
{
    public class OpenFolderService(ILogger<OpenFolderService> logger) : IOpenFolderService
    {
        private readonly ILogger<OpenFolderService> _logger = logger;

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