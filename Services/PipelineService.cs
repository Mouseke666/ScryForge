using ScryForge.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ScryForge.Services
{
    public class PipelineService : BackgroundService
    {
        private readonly ILogger<PipelineService> _logger;
        private readonly CleanupService _cleanup;
        private readonly OpenFolderService _openfolder;
        private readonly CardParserService _parser;
        private readonly IDownloaderService _downloader;
        private readonly UpscalerService _upscaler;
        private readonly CopyService _copy;
        private readonly FlipService _flips;
        private readonly PDFService _pdf;
        private readonly PDFOpenService _openPdf;

        public PipelineService(
            ILogger<PipelineService> logger,
            CleanupService cleanup,
            OpenFolderService openfolder,
            CardParserService parser,
            IDownloaderService downloader,
            UpscalerService upscaler,
            CopyService copy,
            FlipService flips,
            PDFService pdf,
            PDFOpenService openPdf)
        {
            _logger = logger;
            _cleanup = cleanup;
            _openfolder = openfolder;
            _parser = parser;
            _downloader = downloader;
            _upscaler = upscaler;
            _copy = copy;
            _flips = flips;
            _pdf = pdf;
            _openPdf = openPdf;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("PipelineService started – beginning execution...");

            try
            {
                await RunPipelineAsync(stoppingToken);
                _logger.LogInformation("Pipeline completed successfully!");
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("Pipeline stopped due to shutdown request.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unrecoverable error in the pipeline. Application will terminate.");
            }
        }

        private void LogStep(ref int step, int totalSteps, string message)
        {
            _logger.LogInformation("Step {Step}/{TotalSteps} – {Message}", step++, totalSteps, message);
        }

        private async Task RunPipelineAsync(CancellationToken ct)
        {
            var banner = @"
 _________                    ___________                         
 /   _____/ ___________ ___.__.\_   _____/__________  ____   ____  
 \_____  \_/ ___\_  __ <   |  | |    __)/  _ \_  __ \/ ___\_/ __ \ 
 /        \  \___|  | \/\___  | |     \(  <_> )  | \/ /_/  >  ___/ 
 /_______  /\___  >__|   / ____| \___  / \____/|__|  \___  / \___  >
         \/     \/       \/          \/             /_____/      \/ 
".Trim();

            foreach (var line in banner.Split('\n', StringSplitOptions.TrimEntries))
                _logger.LogInformation(line);

            _logger.LogInformation("Pipeline started");

            int step = 1;
            int totalSteps = 9;

            LogStep(ref step, totalSteps, "Cleaning up directories...");
            try
            {
                _cleanup.CleanDirectory(AppConfig.ScryForgeDownloaderPath);
                _cleanup.CleanDirectory(AppConfig.UpscaledFolder);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during cleanup, continuing pipeline...");
            }

            // PDF name suggestion + user input
            string suggestedName = await _parser.GetSuggestedPdfNameAsync(AppConfig.CardsFile);
            string timestamp = DateTime.Now.ToString("dd-MM-yyyy_HH-mm-ss");

            _logger.LogInformation("Suggested PDF name: {Name}", suggestedName);
            _logger.LogInformation("Enter PDF name (press Enter to accept suggestion):");

            Console.Write("> ");
            var input = Console.ReadLine();

            string pdfBaseName = string.IsNullOrWhiteSpace(input)
                ? suggestedName
                : input.Trim();

            pdfBaseName = $"{pdfBaseName}_{timestamp}";

            _logger.LogInformation("Using PDF base name: {Name}", pdfBaseName);

            LogStep(ref step, totalSteps, "Downloading card art... (this could take a while)");
            try
            {
                bool success = await _downloader.DownloadArtAsync();
                if (!success)
                    _logger.LogWarning("Download failed, continuing pipeline...");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Download step failed, continuing pipeline...");
            }

            LogStep(ref step, totalSteps, "Upscaling images... (this could take a while)");
            try
            {
                await _upscaler.RunUpscalerAsync(true, AppConfig.ScryForgeDownloaderPath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Upscaling step failed, continuing pipeline...");
            }

            LogStep(ref step, totalSteps, "Parsing cards.txt...");
            List<CardInfo> cards = new();
            try
            {
                cards = await _parser.ParseCardsAsync(AppConfig.CardsFile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Parsing step failed, continuing pipeline...");
            }

            LogStep(ref step, totalSteps, "Processing flip cards...");
            try
            {
                _flips.ProcessFlipCards(cards);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Flip cards step failed, continuing pipeline...");
            }

            LogStep(ref step, totalSteps, "Generating main PDF...");
            try
            {
                if (cards.Any(c => !c.IsFlip))
                {
                    await _pdf.RunAsync("default", pdfBaseName, true);
                    _copy.MoveFile(
                        Path.Combine(AppConfig.PdfPath, $"{pdfBaseName}.pdf"),
                        Path.Combine(AppConfig.BasePath, $"{pdfBaseName}.pdf"));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PDF generation failed, continuing pipeline...");
            }

            LogStep(ref step, totalSteps, "Cleaning upscaled folder (excluding flip cards)...");
            try
            {
                _cleanup.CleanDirectory(AppConfig.UpscaledFolder, "flips");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Cleanup after PDF failed, continuing pipeline...");
            }

            LogStep(ref step, totalSteps, "Checking for flip cards and generating flips PDF if needed...");
            try
            {
                if (Directory.Exists(AppConfig.FlipsFolder) &&
                    Directory.GetFiles(AppConfig.FlipsFolder).Length > 0)
                {
                    string flipsName = $"{pdfBaseName}_flips";

                    _copy.CopyFolderFiles(AppConfig.FlipsFolder, AppConfig.UpscaledFolder);
                    await _pdf.RunAsync("flips", flipsName, true);
                    _copy.MoveFile(
                        Path.Combine(AppConfig.PdfPath, $"{flipsName}.pdf"),
                        Path.Combine(AppConfig.BasePath, $"{flipsName}.pdf"));

                    _cleanup.CleanDirectory(AppConfig.UpscaledFolder);
                }
                else
                {
                    _logger.LogInformation("No flip cards found – skipping flips PDF generation.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Flips PDF generation failed, continuing pipeline...");
            }

            LogStep(ref step, totalSteps, "Opening base folder...");
            try
            {
                _openfolder.OpenFolder(AppConfig.BasePath);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Opening folder failed");
            }
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("PipelineService is shutting down...");
            await base.StopAsync(cancellationToken);
        }
    }
}