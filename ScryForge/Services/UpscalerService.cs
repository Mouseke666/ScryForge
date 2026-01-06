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

            string inputFolder = AppConfig.ScryForgeDownloaderPath;
            string outputFolder = AppConfig.PDFImagesFolder;
            Directory.CreateDirectory(outputFolder);

            var exe = AppConfig.UpscalerExe;
            if (string.IsNullOrWhiteSpace(exe) || !File.Exists(exe))
            {
                _logger.LogError("Upscaler executable not found: {ExePath}", exe);
                return false;
            }

            var args = $"-i \"{inputFolder}\" -o \"{outputFolder}\" -n {AppConfig.UpscaleModel} -s {AppConfig.UpscaleScale} -v";

            var psi = new ProcessStartInfo
            {
                FileName = exe,
                Arguments = args,
                WorkingDirectory = Path.GetDirectoryName(exe)!,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };

            try
            {
                using var process = new Process { StartInfo = psi };
                process.Start();

                // StdOut task
                var stdoutTask = Task.Run(async () =>
                {
                    string? line;
                    while ((line = await process.StandardOutput.ReadLineAsync()) != null)
                    {
                        ProcessUpscalerLine(line, cards);
                    }
                });

                // StdErr task
                var stderrTask = Task.Run(async () =>
                {
                    string? line;
                    while ((line = await process.StandardError.ReadLineAsync()) != null)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        if (line.Contains("error", StringComparison.OrdinalIgnoreCase) ||
                            line.Contains("fail", StringComparison.OrdinalIgnoreCase))
                        {
                            _logger.LogError(line);
                            continue;
                        }

                        ProcessUpscalerLine(line, cards);
                    }
                });

                await Task.WhenAll(stdoutTask, stderrTask);
                await process.WaitForExitAsync();

                if (process.ExitCode != 0)
                {
                    _logger.LogError("Upscaler exited with code {ExitCode}", process.ExitCode);
                    return false;
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during batch upscaling");
                return false;
            }
        }






        private void ProcessUpscalerLine(string line, IReadOnlyList<ScryfallCard> cards)
        {
            // Alleen lines met "-> ... done" zijn relevant
            if (!line.Contains("->") || !line.Contains("done")) return;

            // Haal bestandsnaam van input af
            string inputFile = line.Split("->")[0].Trim();
            string fileName = Path.GetFileName(inputFile);

            // Zoek bijpassende card
            var card = cards.FirstOrDefault(c =>
                Path.GetFileName(c.FrontImagePath) == fileName ||
                Path.GetFileName(c.BackImagePath) == fileName);

            if (card == null) return;

            // Bepaal faces van deze kaart
            var cardFaces = new List<(string Path, string Name)>();
            if (!string.IsNullOrWhiteSpace(card.FrontImagePath))
                cardFaces.Add((card.FrontImagePath, "Front"));
            if (!string.IsNullOrWhiteSpace(card.BackImagePath))
                cardFaces.Add((card.BackImagePath, "Back"));

            int totalFaces = cardFaces.Count;
            int currentFaceIndex = cardFaces.FindIndex(f => Path.GetFileName(f.Path) == fileName) + 1;

            string faceName = cardFaces[currentFaceIndex - 1].Name;

            _logger.LogInformation(
                totalFaces > 1
                    ? "Upscaling [{Current}/{Total}] — {CardName} ({Face})"
                    : "Upscaling [{Current}/{Total}] — {CardName}",
                currentFaceIndex,
                totalFaces,
                card.Name,
                faceName
            );
        }

    }
}