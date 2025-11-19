using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ScryForge.Models;

namespace ScryForge.Services
{
    public class PipelineService : BackgroundService
    {
        private readonly ILogger<PipelineService> _logger;
        private readonly CleanupService _cleanup;
        private readonly OpenFolderService _openfolder;
        private readonly CardParserService _parser;
        private readonly DownloaderService _downloader;
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
            DownloaderService downloader,
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

            _logger.LogInformation("Step 1/10 – Cleaning up directories...");
            _cleanup.CleanDirectory(AppConfig.DownloadedFolder);
            _cleanup.CleanDirectory(AppConfig.UpscaledFolder);
            _cleanup.DeleteFile(Path.Combine(AppConfig.BasePath, "default.pdf"));
            _cleanup.DeleteFile(Path.Combine(AppConfig.BasePath, "flips.pdf"));

            _copy.CopyFile(
                Path.Combine(AppConfig.BasePath, "cards.txt"),
                Path.Combine(AppConfig.ArtDownloaderPath, "cards.txt"));

            _logger.LogInformation("Step 2/10 – Downloading card art... (this could take a while)");
            bool downloadSucceeded = await _downloader.DownloadArtAsync();

            if (!downloadSucceeded || ct.IsCancellationRequested)
            {
                _logger.LogWarning("Download failed or was cancelled. Pipeline stopped.");
                return;
            }

            _copy.CopyFilesToRoot(AppConfig.ScryfallSource);

            _logger.LogInformation("Step 3/10 – Upscaling images... (this could take a while)");
            await _upscaler.RunUpscalerAsync();

            _logger.LogInformation("Step 4/10 – Parsing cards.txt...");
            List<CardInfo> cards = _parser.ParseCards(AppConfig.CardsFile);

            _logger.LogInformation("Step 5/10 – Duplicating double-faced cards...");
            _copy.DuplicateCards(cards);

            _logger.LogInformation("Step 6/10 – Processing flip cards...");
            _flips.ProcessFlipCards(cards);

            _logger.LogInformation("Step 7/10 – Generating default.pdf...");

            if (cards.Any(c => !c.IsFlip))
            {
                await _pdf.RunAsync("default");
            }

            _logger.LogInformation("Step 8/10 – Cleaning upscaled folder (excluding flip cards)...");
            _cleanup.CleanDirectory(AppConfig.UpscaledFolder, "flips");

            _logger.LogInformation("Step 9/10 – Moving default.pdf to root folder...");
            _copy.MoveFile(
                Path.Combine(AppConfig.PdfPath, "default.pdf"),
                Path.Combine(AppConfig.BasePath, "default.pdf"));

            if (Directory.Exists(AppConfig.FlipsFolder) &&
                Directory.GetFiles(AppConfig.FlipsFolder).Length > 0)
            {
                _logger.LogInformation("Flip cards detected → generating flips.pdf...");
                _copy.CopyFolderFiles(AppConfig.FlipsFolder, AppConfig.UpscaledFolder);
                await _pdf.RunAsync("flips");
                _copy.MoveFile(
                    Path.Combine(AppConfig.PdfPath, "flips.pdf"),
                    Path.Combine(AppConfig.BasePath, "flips.pdf"));
                _cleanup.CleanDirectory(AppConfig.UpscaledFolder);
            }
            else
            {
                _logger.LogInformation("No flip cards found – skipping flips.pdf generation.");
            }

            _logger.LogInformation("Mission was a great success!");

            _openfolder.OpenFolder(AppConfig.BasePath);
        }

        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("PipelineService is shutting down...");
            await base.StopAsync(cancellationToken);
        }
    }
}