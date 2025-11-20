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

            _logger.LogInformation("Step {Step}/{TotalSteps} – Cleaning up directories...", step++, totalSteps);
            _cleanup.CleanDirectory(AppConfig.DownloadedFolder);
            _cleanup.CleanDirectory(AppConfig.UpscaledFolder);
            _cleanup.DeleteFile(Path.Combine(AppConfig.BasePath, "default.pdf"));
            _cleanup.DeleteFile(Path.Combine(AppConfig.BasePath, "flips.pdf"));

            _copy.CopyFile(
                Path.Combine(AppConfig.BasePath, "cards.txt"),
                Path.Combine(AppConfig.ArtDownloaderPath, "cards.txt"));

            _logger.LogInformation("Step {Step}/{TotalSteps} – Downloading card art... (this could take a while)", step++, totalSteps);
            bool downloadSucceeded = await _downloader.DownloadArtAsync();

            if (!downloadSucceeded || ct.IsCancellationRequested)
            {
                _logger.LogWarning("Download failed or was cancelled. Pipeline stopped.");
                return;
            }

            _copy.CopyFilesToRoot(AppConfig.ScryfallSource);

            _logger.LogInformation("Step {Step}/{TotalSteps} – Upscaling images... (this could take a while)", step++, totalSteps);
            await _upscaler.RunUpscalerAsync(true);

            _logger.LogInformation("Step {Step}/{TotalSteps} – Parsing cards.txt...", step++, totalSteps);
            List<CardInfo> cards = await _parser.ParseCardsAsync(AppConfig.CardsFile);

            _logger.LogInformation("Step {Step}/{TotalSteps} – Processing flip cards...", step++, totalSteps);
            _flips.ProcessFlipCards(cards);

            _logger.LogInformation("Step {Step}/{TotalSteps} – Generating default.pdf...", step++, totalSteps);
            if (cards.Any(c => !c.IsFlip))
            {
                await _pdf.RunAsync("default", true);
            }

            _logger.LogInformation("Step {Step}/{TotalSteps} – Cleaning upscaled folder (excluding flip cards)...", step++, totalSteps);
            _cleanup.CleanDirectory(AppConfig.UpscaledFolder, "flips");

            if (cards.Any(c => !c.IsFlip))
            {
                _copy.MoveFile(Path.Combine(AppConfig.PdfPath, "default.pdf"), Path.Combine(AppConfig.BasePath, "default.pdf"));
            }

            if (Directory.Exists(AppConfig.FlipsFolder) &&
                Directory.GetFiles(AppConfig.FlipsFolder).Length > 0)
            {
                _logger.LogInformation("Step {Step}/{TotalSteps} – Flip cards detected → generating flips.pdf...", step++, totalSteps);
                _copy.CopyFolderFiles(AppConfig.FlipsFolder, AppConfig.UpscaledFolder);
                await _pdf.RunAsync("flips", true);
                _copy.MoveFile(
                    Path.Combine(AppConfig.PdfPath, "flips.pdf"),
                    Path.Combine(AppConfig.BasePath, "flips.pdf"));
                _cleanup.CleanDirectory(AppConfig.UpscaledFolder);
            }
            else
            {
                _logger.LogInformation("Step {Step}/{TotalSteps} – No flip cards found – skipping flips.pdf generation.", step++, totalSteps);
            }

            _openfolder.OpenFolder(AppConfig.BasePath);
        }


        public override async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("PipelineService is shutting down...");
            await base.StopAsync(cancellationToken);
        }
    }
}