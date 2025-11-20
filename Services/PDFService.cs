using System.Diagnostics;
using Microsoft.Extensions.Logging;

namespace ScryForge.Services
{
    public class PDFService
    {
        private readonly ILogger<PDFService> _logger;

        public PDFService(ILogger<PDFService> logger)
        {
            _logger = logger;
        }

        public async Task RunAsync(string project, bool showOutput = true)
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
                var projectPdf = Path.Combine(workingDir, $"{project}.pdf");

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
    }
}