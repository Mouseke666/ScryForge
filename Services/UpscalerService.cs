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

            // Tel alle afbeeldingen van alle kaarten
            int totalImages = cards.Sum(c =>
                c.IsDoubleFaced && c.CardFaces != null && c.CardFaces.Count > 1 ? 2 : 1);

            int currentImage = 0;

            foreach (var card in cards)
            {
                // Double-faced card
                if (card.IsDoubleFaced && card.CardFaces != null && card.CardFaces.Count > 1)
                {
                    var images = new[] { card.FrontImagePath, card.BackImagePath };

                    foreach (var (imagePath, faceName) in images.Select((p, i) => (p, i == 0 ? "Front" : "Back")))
                    {
                        if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath))
                        {
                            _logger.LogWarning(
                                "Image not found for card {CardName} ({Face}), skipping ([{Current}/{Total}]).",
                                card.Name,
                                faceName,
                                currentImage + 1,
                                totalImages);
                            continue;
                        }

                        currentImage++;
                        _logger.LogInformation(
                            "Upscaling [{Current}/{Total}] — {CardName} ({Face})",
                            currentImage,
                            totalImages,
                            card.Name,
                            faceName);

                        try
                        {
                            await RunUpscalerForSingleImageAsync(imagePath, logOutput: false);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(
                                ex,
                                "Upscaling failed for [{Current}/{Total}] — {CardName} ({Face})",
                                currentImage,
                                totalImages,
                                card.Name,
                                faceName);
                        }
                    }
                }
                else // Normale kaart
                {
                    if (string.IsNullOrWhiteSpace(card.FrontImagePath) || !File.Exists(card.FrontImagePath))
                    {
                        _logger.LogWarning(
                            "Image not found for card {CardName}, skipping ([{Current}/{Total}]).",
                            card.Name,
                            currentImage + 1,
                            totalImages);
                        continue;
                    }

                    currentImage++;
                    _logger.LogInformation(
                        "Upscaling [{Current}/{Total}] — {CardName}",
                        currentImage,
                        totalImages,
                        card.Name);

                    try
                    {
                        await RunUpscalerForSingleImageAsync(card.FrontImagePath, logOutput: false);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(
                            ex,
                            "Upscaling failed for [{Current}/{Total}] — {CardName}",
                            currentImage,
                            totalImages,
                            card.Name);
                    }
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
