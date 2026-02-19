using System.Diagnostics;
using ScryForge.Models.Scryfall;
using Microsoft.Extensions.Logging;
using ScryForge.Services.Interfaces;
using System.Text.RegularExpressions;
using System.Text;

namespace ScryForge.Services
{
    public class UpscalerService(ILogger<UpscalerService> logger) : IUpscalerService
    {
        private readonly ILogger<UpscalerService> _logger = logger;

        public async Task<bool> RunUpscalerForCardsAsync(IReadOnlyList<ScryfallCard> cards, string model, int scale)
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

            string tempFolder = Path.Combine(inputFolder, "_temp_isolation");
            Directory.CreateDirectory(tempFolder);

            try
            {
                HashSet<string> allowedFiles = cards
                    .SelectMany(c =>
                    {
                        var files = new List<string>();

                        if (!string.IsNullOrWhiteSpace(c.FrontImagePath))
                            files.Add(Path.GetFileName(c.FrontImagePath)!);

                        if (!string.IsNullOrWhiteSpace(c.BackImagePath))
                            files.Add(Path.GetFileName(c.BackImagePath)!);

                        return files;
                    })
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);

                foreach (var file in Directory.GetFiles(inputFolder))
                {
                    var name = Path.GetFileName(file);
                    if (!allowedFiles.Contains(name))
                    {
                        var dest = Path.Combine(tempFolder, name);
                        File.Move(file, dest, overwrite: true);
                    }
                }

                var args = $"-i \"{inputFolder}\" -o \"{outputFolder}\" -n {model} -s {scale} -v";

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

                int currentImage = 0;
                int totalImages = cards.Sum(c =>
                {
                    int count = 0;
                    if (!string.IsNullOrWhiteSpace(c.FrontImagePath)) count++;
                    if (!string.IsNullOrWhiteSpace(c.BackImagePath)) count++;
                    return count;
                });

                using var process = new Process { StartInfo = psi };
                process.Start();

                var stdoutTask = Task.Run(async () =>
                {
                    string? line;
                    while ((line = await process.StandardOutput.ReadLineAsync()) != null)
                    {
                        ProcessUpscalerLine(line, cards, ref currentImage, totalImages);
                    }
                });

                var stderrTask = Task.Run(async () =>
                {
                    string? line;
                    while ((line = await process.StandardError.ReadLineAsync()) != null)
                    {
                        if (string.IsNullOrWhiteSpace(line)) continue;

                        if (Regex.IsMatch(line, @"\berror\b", RegexOptions.IgnoreCase) || Regex.IsMatch(line, @"\bfail\b", RegexOptions.IgnoreCase))
                        {
                            _logger.LogError(line);
                            continue;
                        }

                        ProcessUpscalerLine(line, cards, ref currentImage, totalImages);
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
            finally
            {
                foreach (var file in Directory.GetFiles(tempFolder))
                {
                    var dest = Path.Combine(inputFolder, Path.GetFileName(file));
                    File.Move(file, dest, overwrite: true);
                }

                try
                {
                    Directory.Delete(tempFolder, recursive: true);
                }
                catch { /* Niet kritisch */ }
            }
        }

        private void ProcessUpscalerLine(string line, IReadOnlyList<ScryfallCard> cards, ref int currentImage, int totalImages)
        {
            if (!line.Contains("->") || !line.Contains("done")) return;

            string inputFile = line.Split("->")[0].Trim();
            string fileName = Path.GetFileName(inputFile);
            fileName = FixEncoding(fileName);  // hier ontstaat �

            var card = cards.FirstOrDefault(c =>
                FileNameMatches(Path.GetFileName(c.FrontImagePath), fileName) ||
                FileNameMatches(Path.GetFileName(c.BackImagePath), fileName));

            if (card == null)
                return;

            var cardFaces = new List<(string Path, string Name)>();
            if (!string.IsNullOrWhiteSpace(card.FrontImagePath))
                cardFaces.Add((card.FrontImagePath, "Front"));
            if (!string.IsNullOrWhiteSpace(card.BackImagePath))
                cardFaces.Add((card.BackImagePath, "Back"));

            int totalFaces = cardFaces.Count;
            int faceIndex = cardFaces.FindIndex(f => FileNameMatches(Path.GetFileName(f.Path), fileName));

            if (faceIndex == -1)
                return;

            faceIndex += 1;
            string faceName = cardFaces[faceIndex - 1].Name;

            int globalIndex = Interlocked.Increment(ref currentImage);

            _logger.LogInformation(
                totalFaces > 1
                    ? "Upscaling [{Current}/{Total}] - {CardName} ({Face})"
                    : "Upscaling [{Current}/{Total}] - {CardName}",
                globalIndex,
                totalImages,
                $"{card.Name}{(totalFaces > 1 ? $" // {card.CardFaces?[1].Name}" : string.Empty)}",
                faceName
            );
        }

        private static bool FileNameMatches(string? correct, string? buggy)
        {
            if (string.IsNullOrEmpty(correct) || string.IsNullOrEmpty(buggy))
                return false;

            if (correct.Length != buggy.Length)
                return false;

            for (int i = 0; i < correct.Length; i++)
            {
                char c1 = correct[i];
                char c2 = buggy[i];

                if (c1 == c2)
                    continue;

                if (c1 == '\uFFFD' || c2 == '\uFFFD')
                    continue;

                return false;
            }

            return true;
        }

        private static string FixEncoding(string input)
        {
            var bytes = Encoding.GetEncoding(1252).GetBytes(input);
            return Encoding.UTF8.GetString(bytes);
        }
    }
}