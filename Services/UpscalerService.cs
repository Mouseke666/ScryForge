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

        public async Task RunUpscalerAsync(bool logOutput)
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
                RedirectStandardOutput = logOutput,
                RedirectStandardError = logOutput
            };

            try
            {
                using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };

                if (logOutput)
                {
                    process.OutputDataReceived += (sender, e) =>
                    {
                        if (!string.IsNullOrEmpty(e.Data))
                            _logger.LogInformation(e.Data);
                    };

                    process.ErrorDataReceived += (sender, e) =>
                    {
                        if (string.IsNullOrEmpty(e.Data)) return;

                        if (e.Data.Contains("%"))
                            _logger.LogInformation(e.Data);
                        else
                            _logger.LogError(e.Data);
                    };
                }

                process.Start();

                if (logOutput)
                {
                    process.BeginOutputReadLine();
                    process.BeginErrorReadLine();
                }

                await process.WaitForExitAsync();

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
