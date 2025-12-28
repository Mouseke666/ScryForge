using ScryForge.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ScryForge.Services;

public class PipelineService : BackgroundService
{
    private readonly ILogger<PipelineService> _logger;
    private readonly CleanupService _cleanup;
    private readonly OpenFolderService _openfolder;
    private readonly ICardParserService _parser;
    private readonly IDownloaderService _downloader;
    private readonly UpscalerService _upscaler;
    private readonly CopyService _copy;
    private readonly FlipService _flips;
    private readonly IPDFService _pdf;
    private readonly PDFOpenService _openPdf;
    private readonly IEmptySlotsService _emptySlots;
    private readonly IPDFNameService _pdfNameService;

    public PipelineService(
        ILogger<PipelineService> logger,
        CleanupService cleanup,
        OpenFolderService openfolder,
        ICardParserService parser,
        IDownloaderService downloader,
        UpscalerService upscaler,
        CopyService copy,
        FlipService flips,
        IPDFService pdf,
        PDFOpenService openPdf,
        IEmptySlotsService emptySlots,
        IPDFNameService pdfNameService)
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
        _emptySlots = emptySlots;
        _pdfNameService = pdfNameService;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("PipelineService started");

        try
        {
            await RunPipelineAsync(stoppingToken);
            _logger.LogInformation("Pipeline completed successfully");
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Pipeline cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal pipeline error");
        }
    }

    private void LogStep(ref int step, int total, string message)
    {
        _logger.LogInformation("Step {Step}/{Total} – {Message}", step++, total, message);
    }

    private async Task RunPipelineAsync(CancellationToken ct)
    {
        int step = 1;
        int totalSteps = 11;

        // --------------------------
        // Cleanup
        // --------------------------
        LogStep(ref step, totalSteps, "Cleaning working directories");
        try
        {
            _cleanup.CleanDirectory(AppConfig.ScryForgeDownloaderPath);
            _cleanup.CleanDirectory(AppConfig.UpscaledFolder);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cleanup step failed");
        }

        // --------------------------
        // FETCH Scryfall JSON
        // --------------------------
        LogStep(ref step, totalSteps, "Fetching Scryfall cards (JSON)");

        List<ScryfallCard> scryfallCards = [];
        try
        {
            scryfallCards = (await _downloader.FetchScryfallCardsAsync()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fetching Scryfall cards failed");
            return;
        }

        if (scryfallCards.Count == 0)
        {
            _logger.LogWarning("No cards fetched from Scryfall. Aborting pipeline.");
            return;
        }

        var emptySlotsResult = await _emptySlots.AnalyzeAsync(scryfallCards, ct);

        if (emptySlotsResult.ShouldStopPipeline)
        {
            _logger.LogInformation("Exiting program by user choice.");
            return;
        }

        LogStep(ref step, totalSteps, "Determining PDF name");
        var pdfNameResult = await _pdfNameService.DeterminePdfNameAsync(AppConfig.CardsFile);

        // --------------------------
        // DOWNLOAD IMAGES
        // --------------------------
        LogStep(ref step, totalSteps, "Downloading card images");

        try
        {
            await _downloader.DownloadImagesAsync(scryfallCards);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Downloading images failed");
        }

        // --------------------------
        // UPSCALE
        // --------------------------
        LogStep(ref step, totalSteps, "Upscaling images");

        try
        {
            await _upscaler.RunUpscalerAsync(true, AppConfig.ScryForgeDownloaderPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Upscaling step failed");
        }

        // --------------------------
        // PARSE CARDS.TXT
        // --------------------------
        LogStep(ref step, totalSteps, "Parsing cards.txt");

        List<CardInfo> cards = new();
        try
        {
            cards = await _parser.ParseCardsAsync(AppConfig.CardsFile);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Parsing cards.txt failed");
        }

        // --------------------------
        // FLIPS
        // --------------------------
        LogStep(ref step, totalSteps, "Processing flip cards");

        try
        {
            _flips.ProcessFlipCards(cards);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Processing flips failed");
        }

        // --------------------------
        // MAIN PDF
        // --------------------------
        LogStep(ref step, totalSteps, "Generating main PDF");

        try
        {
            if (cards.Any(c => !c.IsFlip))
            {
                await _pdf.RunAsync("default", pdfNameResult.BaseName, true);
                _copy.MoveFile(
                    Path.Combine(AppConfig.PdfPath, $"{pdfNameResult.BaseName}.pdf"),
                    Path.Combine(AppConfig.BasePath, $"{pdfNameResult.BaseName}.pdf"));
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Generating main PDF failed");
        }

        // --------------------------
        // CLEAN UPSCALED (NO FLIPS)
        // --------------------------
        LogStep(ref step, totalSteps, "Cleaning upscaled folder (excluding flips)");

        try
        {
            _cleanup.CleanDirectory(AppConfig.UpscaledFolder, "flips");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cleaning upscaled folder failed");
        }

        // --------------------------
        // FLIPS PDF
        // --------------------------
        LogStep(ref step, totalSteps, "Generating flips PDF if required");

        try
        {
            if (Directory.Exists(AppConfig.FlipsFolder) &&
                Directory.GetFiles(AppConfig.FlipsFolder).Any())
            {
                string flipsName = $"{pdfNameResult.BaseName}_flips";

                _copy.CopyFolderFiles(AppConfig.FlipsFolder, AppConfig.UpscaledFolder);
                await _pdf.RunAsync("flips", flipsName, true);

                _copy.MoveFile(
                    Path.Combine(AppConfig.PdfPath, $"{flipsName}.pdf"),
                    Path.Combine(AppConfig.BasePath, $"{flipsName}.pdf"));

                _cleanup.CleanDirectory(AppConfig.UpscaledFolder);
            }
            else
            {
                _logger.LogInformation("No flip cards found");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Generating flips PDF failed");
        }

        // --------------------------
        // OPEN FOLDER
        // --------------------------
        LogStep(ref step, totalSteps, "Opening output folder");

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
        _logger.LogInformation("PipelineService stopping");
        await base.StopAsync(cancellationToken);
    }
}
