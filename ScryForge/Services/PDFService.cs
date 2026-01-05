using System.Text.Json;
using ScryForge.Models;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using ScryForge.Services.Interfaces;

namespace ScryForge.Services
{
    public class PDFService(ILogger<PDFService> logger, ICopyService copy, ICleanupService cleanup) : IPDFService
    {
        private readonly ILogger<PDFService> _logger = logger;
        private readonly ICopyService _copy = copy;
        private readonly ICleanupService _cleanup = cleanup;

        public async Task RunAsync(string project, string pdfFileName, bool showOutput = true)
        {
            var exe = AppConfig.PDFExe;
            if (!File.Exists(exe))
            {
                if (showOutput)
                    _logger.LogError("PDF Service executable missing at: {Exe}", exe);

                return;
            }

            var workingDir = Path.GetDirectoryName(exe);
            if (workingDir == null)
            {
                if (showOutput)
                    _logger.LogError("Working directory could not be determined for PDF service.");

                throw new InvalidOperationException("workingDir cannot be null.");
            }

            string projectFile = $"{project}.json";

            var psi = new ProcessStartInfo
            {
                FileName = exe,
                Arguments = $"--render --project \"{projectFile}\"",
                WorkingDirectory = workingDir,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };

            process.OutputDataReceived += (sender, e) =>
            {
                if (showOutput && !string.IsNullOrEmpty(e.Data))
                    _logger.LogInformation("[PDFService] {Message}", e.Data);
            };

            process.ErrorDataReceived += (sender, e) =>
            {
                if (showOutput && !string.IsNullOrEmpty(e.Data))
                    _logger.LogError("[PDFService] {Message}", e.Data);
            };

            try
            {
                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                await process.WaitForExitAsync();

                var printMeFile = Path.Combine(workingDir, "_printme.pdf");
                var projectPdf = Path.Combine(workingDir, $"{pdfFileName}.pdf");

                if (File.Exists(printMeFile))
                {
                    if (File.Exists(projectPdf))
                        File.Delete(projectPdf);

                    File.Move(printMeFile, projectPdf);

                    if (showOutput)
                        _logger.LogInformation("PDF saved as: {Pdf}", projectPdf);
                }
                else
                {
                    if (showOutput)
                        _logger.LogWarning("Cannot find _printme.pdf to rename.");
                }
            }
            catch (Exception ex)
            {
                if (showOutput)
                    _logger.LogError(ex, "PDF Service failed for project {Project}", project);
            }
        }

        public async Task GenerateMainPdfAsync(string baseName, IEnumerable<CardInfo> cards)
        {
            if (!cards.Any(c => !c.IsFlip))
                return;

            try
            {
                await RunAsync("default", baseName, true);

                string outputPath = Path.Combine(AppConfig.BasePath, "Output");
                Directory.CreateDirectory(outputPath); // Zorg dat de folder bestaat

                _copy.MoveFile(
                    Path.Combine(AppConfig.PdfPath, $"{baseName}.pdf"),
                    Path.Combine(outputPath, $"{baseName}.pdf"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Generating main PDF failed");
            }
        }

        public async Task GenerateFlipsPdfAsync(string baseName)
        {
            if (!Directory.Exists(AppConfig.FlipsFolder) ||
                Directory.GetFiles(AppConfig.FlipsFolder).Length == 0)
            {
                _logger.LogInformation("No flip cards found");
                return;
            }

            try
            {
                string flipsName = $"{baseName}_flips";

                _copy.CopyFolderFiles(AppConfig.FlipsFolder, AppConfig.PDFImagesFolder);

                await RunAsync("flips", flipsName, true);

                string outputPath = Path.Combine(AppConfig.BasePath, "Output");
                Directory.CreateDirectory(outputPath);

                _copy.MoveFile(
                    Path.Combine(AppConfig.PdfPath, $"{flipsName}.pdf"),
                    Path.Combine(outputPath, $"{flipsName}.pdf"));

                await _cleanup.CleanDirectoryAsync(AppConfig.PDFImagesFolder);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Generating flips PDF failed");
            }
        }

        public async Task<int> GetMaxCardsPerPage(string jsonFilePath)
        {
            if (!File.Exists(jsonFilePath))
                throw new FileNotFoundException($"File not found: {jsonFilePath}");

            string jsonContent = File.ReadAllText(jsonFilePath);
            using var doc = JsonDocument.Parse(jsonContent);

            var root = doc.RootElement;

            if (root.TryGetProperty("card_layout_vertical", out var layout))
            {
                int height = layout.GetProperty("height").GetInt32();
                int width = layout.GetProperty("width").GetInt32();
                return height * width;
            }

            return 0;
        }
    }
}