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
            {
                _logger.LogInformation(line);
            }

            _logger.LogInformation("Pipeline started");

            int step = 1;
            int totalSteps = 9;

            LogStep(ref step, totalSteps, "Cleaning up directories...");
            try
            {
                _cleanup.CleanDirectory(AppConfig.ScryForgeDownloaderPath);
                _cleanup.CleanDirectory(AppConfig.UpscaledFolder);
                _cleanup.DeleteFile(Path.Combine(AppConfig.BasePath, "default.pdf"));
                _cleanup.DeleteFile(Path.Combine(AppConfig.BasePath, "flips.pdf"));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during cleanup, continuing pipeline...");
            }

            // LogStep(ref step, totalSteps, "Copying cards.txt to ArtDownloaderPath...");
            // try
            // {
            //     _copy.CopyFile(
            //         Path.Combine(AppConfig.BasePath, "cards.txt"),
            //         Path.Combine(AppConfig.ArtDownloaderPath, "cards.txt"));
            // }
            // catch (Exception ex)
            // {
            //     _logger.LogError(ex, "Error copying cards.txt, continuing pipeline...");
            // }

            LogStep(ref step, totalSteps, "Downloading card art... (this could take a while)");
            bool downloadSucceeded = false;
            try
            {
                downloadSucceeded = await _downloader.DownloadArtAsync();
                if (!downloadSucceeded)
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
            List<CardInfo> cards = new List<CardInfo>();
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

            LogStep(ref step, totalSteps, "Generating default.pdf...");
            try
            {
                if (cards.Any(c => !c.IsFlip))
                {
                    await _pdf.RunAsync("default", true);
                    _copy.MoveFile(Path.Combine(AppConfig.PdfPath, "default.pdf"), Path.Combine(AppConfig.BasePath, "default.pdf"));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "PDF generation failed for default.pdf, continuing pipeline...");
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

            LogStep(ref step, totalSteps, "Checking for flip cards and generating flips.pdf if needed...");
            try
            {
                if (Directory.Exists(AppConfig.FlipsFolder) && Directory.GetFiles(AppConfig.FlipsFolder).Length > 0)
                {
                    _copy.CopyFolderFiles(AppConfig.FlipsFolder, AppConfig.UpscaledFolder);
                    await _pdf.RunAsync("flips", true);
                    _copy.MoveFile(Path.Combine(AppConfig.PdfPath, "flips.pdf"), Path.Combine(AppConfig.BasePath, "flips.pdf"));
                    _cleanup.CleanDirectory(AppConfig.UpscaledFolder);
                }
                else
                {
                    _logger.LogInformation("No flip cards found – skipping flips.pdf generation.");
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