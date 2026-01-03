using ScryForge.Models;
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

        // public async Task RunUpscalerAsync(bool logOutput, string imageSource)
        // {
        //     var exe = AppConfig.UpscalerExe;
        //     if (string.IsNullOrWhiteSpace(exe) || !File.Exists(exe))
        //     {
        //         _logger.LogError("Upscaler executable not found: {ExePath}", exe);
        //         return;
        //     }

        //     var args =
        //         $"-i \"{imageSource}\" " +
        //         $"-o \"{AppConfig.UpscaledFolder}\" " +
        //         $"-n {AppConfig.UpscaleModel} " +
        //         $"-s {AppConfig.UpscaleScale}" +
        //         (logOutput ? " -v" : string.Empty);

        //     var psi = new ProcessStartInfo
        //     {
        //         FileName = exe,
        //         Arguments = args,
        //         WorkingDirectory = Path.GetDirectoryName(exe)!,
        //         UseShellExecute = false,
        //         CreateNoWindow = true,
        //         RedirectStandardOutput = logOutput,
        //         RedirectStandardError = logOutput
        //     };

        //     try
        //     {
        //         using var process = new Process { StartInfo = psi };
        //         process.Start();

        //         Task stdoutTask = logOutput
        //             ? ReadStdOutAsync(process)
        //             : Task.CompletedTask;

        //         Task stderrTask = logOutput
        //             ? ReadStdErrAsync(process)
        //             : Task.CompletedTask;

        //         await process.WaitForExitAsync();
        //         await Task.WhenAll(stdoutTask, stderrTask);

        //         if (process.ExitCode != 0)
        //         {
        //             _logger.LogError(
        //                 "Upscaler exited with error code {ExitCode}",
        //                 process.ExitCode);
        //         }
        //     }
        //     catch (Exception ex)
        //     {
        //         _logger.LogError(ex, "Unexpected error during upscaling");
        //     }
        // }

        public async Task<bool> RunUpscalerForCardsAsync(IReadOnlyList<ScryfallCard> cards)
        {
            if (cards == null || cards.Count == 0)
            {
                _logger.LogInformation("No Scryfall cards to upscale, skipping this step.");
                return false;
            }

            int total = cards.Count;
            int current = 0;

            foreach (var card in cards)
            {
                current++;

                if (string.IsNullOrWhiteSpace(card.ImagePath) || !File.Exists(card.ImagePath))
                {
                    _logger.LogWarning(
                        "Image not found for card {CardName}, skipping ([{Current}/{Total}]).",
                        card.Name,
                        current,
                        total);
                    continue;
                }

                _logger.LogInformation(
                    "Upscaling [{Current}/{Total}] — {CardName}",
                    current,
                    total,
                    card.Name);

                try
                {
                    // Single-image upscaler, output suppressed
                    await RunUpscalerForSingleImageAsync(
                        card.ImagePath,
                        logOutput: false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Upscaling failed for [{Current}/{Total}] — {CardName}",
                        current,
                        total,
                        card.Name);
                }
            }

            return true;
        }

        // Single-image upscaling (stil by default)
        public async Task<bool> RunUpscalerForSingleImageAsync(
            string imagePath,
            bool logOutput = false)
        {
            if (!File.Exists(imagePath))
            {
                _logger.LogError("Image not found: {ImagePath}", imagePath);
                return false;
            }

            var exe = AppConfig.UpscalerExe;
            if (string.IsNullOrWhiteSpace(exe) || !File.Exists(exe))
            {
                _logger.LogError("Upscaler executable not found: {ExePath}", exe);
                return false;
            }

            Directory.CreateDirectory(AppConfig.UpscaledFolder);

            var outputFile = Path.Combine(
                AppConfig.UpscaledFolder,
                Path.GetFileName(imagePath));

            if (File.Exists(outputFile))
                return true;

            var args =
                $"-i \"{imagePath}\" " +
                $"-o \"{outputFile}\" " +
                $"-n {AppConfig.UpscaleModel} " +
                $"-s {AppConfig.UpscaleScale}" +
                (logOutput ? " -v" : string.Empty);

            var psi = new ProcessStartInfo
            {
                FileName = exe,
                Arguments = args,
                WorkingDirectory = Path.GetDirectoryName(exe)!,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = logOutput,
                RedirectStandardError = logOutput
            };

            try
            {
                using var process = new Process { StartInfo = psi };
                process.Start();

                Task stdoutTask = logOutput
                    ? ReadStdOutAsync(process)
                    : Task.CompletedTask;

                Task stderrTask = logOutput
                    ? ReadStdErrAsync(process)
                    : Task.CompletedTask;

                await process.WaitForExitAsync();
                await Task.WhenAll(stdoutTask, stderrTask);

                if (process.ExitCode != 0)
                {
                    _logger.LogError(
                        "Upscaler exited with code {ExitCode}",
                        process.ExitCode);
                    return false;
                }

                return File.Exists(outputFile);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Unexpected error during single-image upscaling");
                return false;
            }
        }

        private async Task ReadStdOutAsync(Process process)
        {
            string? line;
            while ((line = await process.StandardOutput.ReadLineAsync()) != null)
            {
                if (!string.IsNullOrWhiteSpace(line))
                    _logger.LogInformation(line);
            }
        }

        private async Task ReadStdErrAsync(Process process)
        {
            string? line;
            while ((line = await process.StandardError.ReadLineAsync()) != null)
            {
                if (string.IsNullOrWhiteSpace(line))
                    continue;

                if (line.Contains("error", StringComparison.OrdinalIgnoreCase) ||
                    line.Contains("fail", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogError(line);
                }
                else
                {
                    _logger.LogInformation(line);
                }
            }
        }
    }
}
