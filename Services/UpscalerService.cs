using System.Diagnostics;
using ScryForge.Models.Scryfall;
using Microsoft.Extensions.Logging;
using ScryForge.Services.Interfaces;

namespace ScryForge.Services
{
    public class UpscalerService(ILogger<UpscalerService> logger) : IUpscalerService
    {
        private readonly ILogger<UpscalerService> _logger = logger;

        public async Task<bool> RunUpscalerForCardsAsync(IReadOnlyList<ScryfallCard> cards)
        {
            if (cards == null || cards.Count == 0)
            {
                _logger.LogInformation("No Scryfall cards to upscale, skipping this step.");
                return false;
            }

            var allImages = cards.SelectMany(c =>
            {
                if (c.IsDoubleFaced && c.CardFaces != null && c.CardFaces.Count > 1)
                {
                    return new[]
                    {
                        (c.FrontImagePath, c.Name, face: (string?)"Front"),
                        (c.BackImagePath, c.Name, face: (string?)"Back")
                    };
                }
                return new[] { (c.FrontImagePath, c.Name, (string?)null) };
            }).ToList();

            int totalImages = allImages.Count;
            int currentImage = 0;

            using var semaphore = new SemaphoreSlim(AppConfig.UpscalerThreads);
            var tasks = allImages.Select(async imageTuple =>
            {
                await semaphore.WaitAsync();
                try
                {
                    var (imagePath, cardName, face) = imageTuple;

                    if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath))
                    {
                        int skipped = Interlocked.Increment(ref currentImage);
                        _logger.LogWarning(
                            "Image not found for card {CardName}{Face}, skipping ([{Current}/{Total}]).",
                            cardName,
                            face != null ? $" ({face})" : string.Empty,
                            skipped,
                            totalImages);
                        return;
                    }

                    int index = Interlocked.Increment(ref currentImage);

                    _logger.LogInformation(
                        "Upscaling [{Current}/{Total}] — {CardName}{Face}",
                        index,
                        totalImages,
                        cardName,
                        face != null ? $" ({face})" : string.Empty);

                    await RunUpscalerForSingleImageAsync(imagePath, logOutput: false);
                }
                finally
                {
                    semaphore.Release();
                }
            });

            await Task.WhenAll(tasks);
            return true;
        }

        private async Task<bool> RunUpscalerForSingleImageAsync(
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