using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace ScryForge.Services
{
    public class UpscalerService
    {
        private readonly ILogger<UpscalerService> _logger;

        public UpscalerService(ILogger<UpscalerService> logger)
        {
            _logger = logger;
        }

        public async Task RunUpscalerAsync()
        {
            var exe = AppConfig.UpscalerExe;
            if (string.IsNullOrWhiteSpace(exe) || !File.Exists(exe))
            {
                _logger.LogError("Upscaler executable not found: {ExePath}", exe);
                return;
            }

            var args = $"-i \"{AppConfig.ScryfallSource}\" -o \"{AppConfig.UpscaledFolder}\" -n {AppConfig.UpscaleModel} -s {AppConfig.UpscaleScale}";

            var psi = new ProcessStartInfo
            {
                FileName = exe,
                Arguments = args,
                WorkingDirectory = Path.GetDirectoryName(exe)!,
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            try
            {
                using var process = Process.Start(psi);
                if (process == null)
                {
                    _logger.LogError("Failed to start upscaler process.");
                    return;
                }

                _logger.LogInformation("Upscaling images with {Model} (scale {Scale}x)...", AppConfig.UpscaleModel, AppConfig.UpscaleScale);

                await process.WaitForExitAsync();

                if (process.ExitCode == 0)
                {
                    _logger.LogInformation("Upscaling completed successfully.");
                }
                else
                {
                    _logger.LogError("Upscaler exited with error code {ExitCode}", process.ExitCode);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during upscaling");
            }
        }
    }
}