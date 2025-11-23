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

        public async Task RunUpscalerAsync(bool logOutput, string imageSource)
        {
            var exe = AppConfig.UpscalerExe;
            if (string.IsNullOrWhiteSpace(exe) || !File.Exists(exe))
            {
                _logger.LogError("Upscaler executable not found: {ExePath}", exe);
                return;
            }

            var args =
                $"-i \"{imageSource}\" " +
                $"-o \"{AppConfig.UpscaledFolder}\" " +
                $"-n {AppConfig.UpscaleModel} " +
                $"-s {AppConfig.UpscaleScale}";

            var psi = new ProcessStartInfo
            {
                FileName = exe,
                Arguments = args,
                WorkingDirectory = Path.GetDirectoryName(exe)!,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,   // altijd aan
                RedirectStandardError = true     // altijd aan
            };

            try
            {
                using var process = new Process { StartInfo = psi };

                // Start the process
                process.Start();

                // Read stdout asynchronously
                var stdoutTask = Task.Run(async () =>
                {
                    string? line;
                    while ((line = await process.StandardOutput.ReadLineAsync()) != null)
                    {
                        if (logOutput && !string.IsNullOrWhiteSpace(line))
                            _logger.LogInformation(line);
                    }
                });

                // Read stderr asynchronously
                var stderrTask = Task.Run(async () =>
                {
                    string? line;
                    while ((line = await process.StandardError.ReadLineAsync()) != null)
                    {
                        if (!logOutput || string.IsNullOrWhiteSpace(line))
                            continue;

                        if (line.Contains("%"))
                            _logger.LogInformation(line);
                        else
                            _logger.LogError(line);
                    }
                });

                // Wait for exit
                await process.WaitForExitAsync();

                // Ensure output tasks finish
                await Task.WhenAll(stdoutTask, stderrTask);

                if (process.ExitCode != 0)
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