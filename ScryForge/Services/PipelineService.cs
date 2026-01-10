using ScryForge.Models;
using ScryForge.Models.Scryfall;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ScryForge.Services.Interfaces;

namespace ScryForge.Services;

public class PipelineService(
    ILogger<PipelineService> logger,
    ICleanupService cleanup,
    IOpenFolderService openfolder,
    ICardParserService parser,
    IDownloaderService downloader,
    IUpscalerService upscaler,
    ICardCopyService cardCopy,
    IPDFService pdf,
    IPDFOpenService openPdf,
    IEmptySlotsService emptySlots,
    IPDFNameService pdfNameService,
    ICustomCardService customCardService) : BackgroundService
{
    private readonly ILogger<PipelineService> _logger = logger;
    private readonly ICleanupService _cleanup = cleanup;
    private readonly IOpenFolderService _openfolder = openfolder;
    private readonly ICardParserService _parser = parser;
    private readonly IDownloaderService _downloader = downloader;
    private readonly IUpscalerService _upscaler = upscaler;
    private readonly ICardCopyService _cardCopy = cardCopy;
    private readonly IPDFService _pdf = pdf;
    private readonly IPDFOpenService _openPdf = openPdf;
    private readonly IEmptySlotsService _emptySlots = emptySlots;
    private readonly IPDFNameService _pdfNameService = pdfNameService;
    private readonly ICustomCardService _customCardService = customCardService;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation($"{AppVersion.GetFull()} - PipelineService started");

        try
        {
            await RunPipelineAsync(stoppingToken);
            _logger.LogInformation("Pipeline completed successfully");
            Environment.Exit(0);
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
        _logger.LogInformation("\nStep {Step}/{Total}: {Message}\n", step++, total, message);
    }

    private async Task RunPipelineAsync(CancellationToken ct)
    {
        int step = 1;
        int totalSteps = 14;

        LogStep(ref step, totalSteps, "Cleaning working directories");

        bool downloaderPathCleaned = await _cleanup.CleanDirectoryAsync(AppConfig.ScryForgeDownloaderPath);
        _logger.LogInformation("Cleaning {Path} {Status}", AppConfig.ScryForgeDownloaderPath, downloaderPathCleaned ? "succeeded" : "failed");

        bool pdfImagesFolderCleaned = await _cleanup.CleanDirectoryAsync(AppConfig.PDFImagesFolder);
        _logger.LogInformation("Cleaning {Path} {Status}", AppConfig.PDFImagesFolder, pdfImagesFolderCleaned ? "succeeded" : "failed");

        LogStep(ref step, totalSteps, "Fetching cards");

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

        IReadOnlyList<CustomCard> customCards = await _customCardService.FetchCustomCardsAsync(AppConfig.CustomFolder);

        if (scryfallCards.Count == 0 && customCards.Count == 0)
        {
            _logger.LogWarning("No cards fetched from Scryfall or custom folder. Aborting pipeline.");

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"Warning: No cards were found. Check your cards.txt and/or custom cards folder.");
            Console.ResetColor();

            return;
        }

        _logger.LogInformation(
            "Fetched {ScryfallCount} Scryfall card(s) and {CustomCount} custom card(s).",
            scryfallCards.Count,
            customCards.Count);

        LogStep(ref step, totalSteps, "Analyzing empty card slots");
        var emptySlotsResult = await _emptySlots.AnalyzeAsync(scryfallCards, customCards, ct);
        if (HandleEmptySlots(emptySlotsResult))
        {
            return;
        }

        LogStep(ref step, totalSteps, "Determining PDF name");

        var pdfNameResult = await _pdfNameService.DeterminePdfNameAsync(AppConfig.CardsFile);
        _logger.LogInformation("Suggested PDF name: {Suggested}", pdfNameResult.BaseNameWithoutTimestamp);

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("Enter PDF name (press Enter to accept suggested name):");
        Console.ResetColor();

        Console.Write("> ");
        string? input = Console.ReadLine();

        string finalBaseName = string.IsNullOrWhiteSpace(input)
            ? pdfNameResult.BaseNameWithoutTimestamp
            : input.Trim();

        string fullName = $"{finalBaseName}_{pdfNameResult.Timestamp}";

        _logger.LogInformation("Using PDF base name: {FullName}", fullName);


        LogStep(ref step, totalSteps, "Downloading card images");
        try
        {
            await _downloader.DownloadImagesAsync(scryfallCards);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Downloading images failed");
        }

        LogStep(ref step, totalSteps, "Upscaling images");

        bool upscaled = await _upscaler.RunUpscalerForCardsAsync(scryfallCards);
        if (!upscaled)
        {
            _logger.LogInformation("No card images available to upscale. Skipping upscaling step.");
        }

        LogStep(ref step, totalSteps, "Copy custom cards");
        await _customCardService.CopyCustomCardsAsync(customCards, AppConfig.PDFImagesFolder);

        LogStep(ref step, totalSteps, "Parsing cards.txt");
        List<CardInfo> cards = [];
        try
        {
            cards = await _parser.ParseCardsAsync(AppConfig.CardsFile);
            if (cards.Count == 0)
            {
                _logger.LogWarning("No cards were found in {File}", AppConfig.CardsFile);
            }
            else
            {
                _logger.LogInformation("Parsed {Count} card(s) from {File}", cards.Count, AppConfig.CardsFile);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Parsing cards.txt failed");
        }

        LogStep(ref step, totalSteps, "Parsing custom cards");
        List<CardInfo> parsedCustomCards = await _parser.ParseCustomCardsAsync(customCards);
        if (parsedCustomCards.Count > 0)
        {
            cards.AddRange(parsedCustomCards);
        }
        else
        {
            _logger.LogInformation("No custom cards parsed");
        }


        LogStep(ref step, totalSteps, "Processing cards");
        try
        {
            var result = _cardCopy.ProcessCards(cards);
            _logger.LogInformation("Processed {Total} cards: {Flip} flip card(s), {Single} single-sided card(s)",
                result.TotalCards, result.FlipCardsProcessed, result.SingleCardsProcessed);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Processing cards failed");
        }

        LogStep(ref step, totalSteps, "Generating main PDF");

        bool mainPdfGenerated = false;
        try
        {
            mainPdfGenerated = await _pdf.GenerateMainPdfAsync(fullName, cards, false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Generating main PDF failed unexpectedly");
        }

        if (mainPdfGenerated)
        {
            _logger.LogInformation("Main PDF successfully generated: {Pdf}.pdf", fullName);
        }
        else
        {
            _logger.LogWarning("Main PDF was not generated: {Pdf}.pdf", fullName);
        }

        LogStep(ref step, totalSteps, "Cleaning upscaled folder (excluding flips)");

        bool cleanupSucceeded = false;
        try
        {
            cleanupSucceeded = await _cleanup.CleanDirectoryAsync(AppConfig.PDFImagesFolder, "flips");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Cleaning upscaled folder failed unexpectedly");
        }

        if (cleanupSucceeded)
        {
            _logger.LogInformation("Upscaled folder cleaned successfully (excluding flips).");
        }
        else
        {
            _logger.LogWarning("Upscaled folder cleanup did not complete or nothing to clean (excluding flips).");
        }

        LogStep(ref step, totalSteps, "Generating flips PDF if required");

        bool flipsPdfGenerated = false;
        try
        {
            flipsPdfGenerated = await _pdf.GenerateFlipsPdfAsync(fullName, false);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Generating flips PDF failed unexpectedly");
        }

        if (flipsPdfGenerated)
        {
            _logger.LogInformation("Flips PDF successfully generated: {Pdf}.pdf", $"{fullName}_flips");
        }
        else
        {
            _logger.LogInformation("No flips PDF was generated.");
        }

        try
        {
            _openfolder.OpenFolder(AppConfig.OutputFolder);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Opening folder failed");
        }

        LogStep(ref step, totalSteps, "Finalization");

        _logger.LogInformation("Pipeline finished");
        _logger.LogInformation("Thank you for using ScryForge!\n");

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("Press any key to exit...");
        Console.ResetColor();

        _ = Console.ReadKey(true);
        Environment.Exit(0);
    }

    private bool HandleEmptySlots(EmptySlotsResult result)
    {
        if (!result.HasEmptySlots)
        {
            _logger.LogInformation("No empty slots detected in default or double-faced cards.");
            return false;
        }

        if (AppConfig.AutoFillEmptySlots)
        {
            // Alleen loggen, geen console output
            _logger.LogInformation(
                "There are {EmptyDefault} empty slot(s) on the last page of default cards, " +
                "{EmptyFlips} empty slot(s) on the last page of double-faced cards. Auto-fill is enabled, continuing...",
                result.EmptySlotsDefault,
                result.EmptySlotsFlips);
            return false;
        }

        //TODO: Mogelijk later loggen in file alleen
        // Log voor je logbestand
        // _logger.LogWarning(
        //     "Empty slots detected: {EmptyDefault} in default cards, {EmptyFlips} in double-faced cards.",
        //     result.EmptySlotsDefault,
        //     result.EmptySlotsFlips);

        // Console output voor de gebruiker (kleur geel)
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(
            $"Warning: {result.EmptySlotsDefault} empty slot(s) in default cards, " +
            $"{result.EmptySlotsFlips} in double-faced cards.");
        Console.WriteLine("Press Enter to continue, or type 'Q' to quit.");
        Console.ResetColor();

        Console.Write("> ");
        string? input = Console.ReadLine();

        if (input?.Trim().Equals("Q", StringComparison.OrdinalIgnoreCase) == true)
        {
            _logger.LogInformation("User chose to quit due to empty slots.");
            return true;
        }

        return false;
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("PipelineService stopping");
        await base.StopAsync(cancellationToken);
    }
}