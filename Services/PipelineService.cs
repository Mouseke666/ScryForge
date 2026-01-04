using ScryForge.Models;
using ScryForge.Models.Scryfall;
using ScryForge.Services.Intefaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ScryForge.Services;

public class PipelineService : BackgroundService
{
    private readonly ILogger<PipelineService> _logger;
    private readonly ICleanupService _cleanup;
    private readonly OpenFolderService _openfolder;
    private readonly ICardParserService _parser;
    private readonly IDownloaderService _downloader;
    private readonly UpscalerService _upscaler;
    private readonly ICardCopyService _cardCopy;
    private readonly IPDFService _pdf;
    private readonly PDFOpenService _openPdf;
    private readonly IEmptySlotsService _emptySlots;
    private readonly IPDFNameService _pdfNameService;
    private readonly ICustomCardService _customCardService;

    public PipelineService(
        ILogger<PipelineService> logger,
        ICleanupService cleanup,
        OpenFolderService openfolder,
        ICardParserService parser,
        IDownloaderService downloader,
        UpscalerService upscaler,
        ICardCopyService cardCopy,
        IPDFService pdf,
        PDFOpenService openPdf,
        IEmptySlotsService emptySlots,
        IPDFNameService pdfNameService,
        ICustomCardService customCardService)
    {
        _logger = logger;
        _cleanup = cleanup;
        _openfolder = openfolder;
        _parser = parser;
        _downloader = downloader;
        _upscaler = upscaler;
        _cardCopy = cardCopy;
        _pdf = pdf;
        _openPdf = openPdf;
        _emptySlots = emptySlots;
        _pdfNameService = pdfNameService;
        _customCardService = customCardService;
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
        _logger.LogInformation(AppVersion.GetFull());

        int step = 1;
        int totalSteps = 14;

        LogStep(ref step, totalSteps, "Cleaning working directories");
        await _cleanup.CleanDirectoryAsync(AppConfig.ScryForgeDownloaderPath);
        await _cleanup.CleanDirectoryAsync(AppConfig.UpscaledFolder);

        LogStep(ref step, totalSteps, "Fetching Scryfall cards");

        List<ScryfallCard> scryfallCards;
        try
        {
            scryfallCards = (await _downloader.FetchCardsAsync()).ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fetching Scryfall cards failed");
            return;
        }

        LogStep(ref step, totalSteps, "Fetching custom cards");
        IReadOnlyList<CustomCard> customCards = await _customCardService.FetchCustomCardsAsync(AppConfig.CustomFolder);

        if (scryfallCards.Count == 0 && customCards.Count == 0)
        {
            _logger.LogWarning("No cards fetched from Scryfall and/or no custom cards available. Aborting pipeline.");
            return;
        }

        var emptySlotsResult = await _emptySlots.AnalyzeAsync(scryfallCards, customCards, ct);
        if (emptySlotsResult.ShouldStopPipeline)
        {
            _logger.LogInformation("Exiting program by user choice.");
            return;
        }

        LogStep(ref step, totalSteps, "Determining PDF name");
        var pdfNameResult = await _pdfNameService.DeterminePdfNameAsync(AppConfig.CardsFile);

        LogStep(ref step, totalSteps, "Downloading card images");
        try
        {
            await _downloader.DownloadImagesAsync(scryfallCards);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Downloading images failed");
        }

        // LogStep(ref step, totalSteps, "Upscaling images");

        // if (scryfallCards != null && scryfallCards.Count > 0)
        // {
        //     try
        //     {
        //         await _upscaler.RunUpscalerAsync(true, AppConfig.ScryForgeDownloaderPath);
        //     }
        //     catch (Exception ex)
        //     {
        //         _logger.LogError(ex, "Upscaling step failed");
        //     }
        // }
        // else
        // {
        //     _logger.LogInformation("No Scryfall cards to upscale, skipping this step.");
        // }

        LogStep(ref step, totalSteps, "Upscaling images");
        if (!await _upscaler.RunUpscalerForCardsAsync(scryfallCards))
        {
            return;
        }

        LogStep(ref step, totalSteps, "Copy Custom Cards");
        await _customCardService.CopyCustomCardsAsync(customCards, AppConfig.UpscaledFolder);

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

        LogStep(ref step, totalSteps, "Parsing Custom Cards");
        cards.AddRange(await _parser.ParseCustomCardsAsync(customCards));

        LogStep(ref step, totalSteps, "Processing cards");
        try
        {
            _cardCopy.ProcessCards(cards);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Processing cards failed");
        }

        LogStep(ref step, totalSteps, "Generating main PDF");
        try
        {
            await _pdf.GenerateMainPdfAsync(pdfNameResult.BaseName, cards);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Generating main PDF failed");
        }

        LogStep(ref step, totalSteps, "Cleaning upscaled folder (excluding flips)");
        try
        {
            await _cleanup.CleanDirectoryAsync(AppConfig.UpscaledFolder, "flips");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cleaning upscaled folder failed");
        }

        LogStep(ref step, totalSteps, "Generating flips PDF if required");
        try
        {
            await _pdf.GenerateFlipsPdfAsync(pdfNameResult.BaseName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Generating flips PDF failed");
        }

        LogStep(ref step, totalSteps, "Opening output folder");
        try
        {
            _openfolder.OpenFolder(AppConfig.OutputFolder);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Opening folder failed");
        }

        _logger.LogInformation("Pipeline finished");
        _logger.LogInformation("Thank you for using ScryForge!");

        Console.WriteLine("Press any key to exit...");
        _ = Console.ReadLine();
        Environment.Exit(0);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("PipelineService stopping");
        await base.StopAsync(cancellationToken);
    }
}