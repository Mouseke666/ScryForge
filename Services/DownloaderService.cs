using System.Diagnostics;
using System.ComponentModel;
using Microsoft.Extensions.Logging;

namespace ScryForge.Services
{
    public class DownloaderService
    {
        private readonly ILogger<DownloaderService> _logger;

        public DownloaderService(ILogger<DownloaderService> logger)
        {
            _logger = logger;
        }

        public async Task<bool> DownloadArtAsync()
        {
            var exe = AppConfig.ArtDownloaderExe;
            if (!File.Exists(exe))
            {
                _logger.LogError("Downloader exe missing: {ExePath}", exe);
                return false;
            }

            var psi = new ProcessStartInfo
            {
                FileName = exe,
                WorkingDirectory = AppConfig.ArtDownloaderPath,
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
            };

            try
            {
                using var process = Process.Start(psi);
                if (process == null)
                {
                    _logger.LogError("Could not start the downloader process.");
                    return false;
                }

                await process.WaitForExitAsync();

                if (process.ExitCode == 0)
                {
                    return true;
                }
                else
                {
                    _logger.LogWarning("Downloader exited with code {ExitCode}", process.ExitCode);
                    return false;
                }
            }
            catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
            {
                _logger.LogWarning("Download cancelled by user (UAC/SmartScreen). To fix: Right-click MTG Art Downloader.exe → Properties → Unblock");

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting downloader process.");
                return false;
            }
        }
    }
}