using ScryForge.Steps.Interfaces;
using ScryForge.Services.Interfaces;
using Microsoft.Extensions.Logging;
using ScryForge.Models.Scryfall;
using ScryForge.Models;

namespace ScryForge.Steps;

public class FetchCardsStep(IDownloaderService downloader, ICustomCardService customCardService, ILogger<FetchCardsStep> logger) : IPipelineStep
{
    public string Name => "Fetching cards";

    public async Task ExecuteAsync(PipelineContext context, CancellationToken ct)
    {
        List<ScryfallCard> scryfallCards;

        try
        {
            scryfallCards = (await downloader.FetchCardsAsync()).ToList();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fetching Scryfall cards failed");
            throw;
        }

        IReadOnlyList<CustomCard> customCards;

        try
        {
            customCards = await customCardService.FetchCustomCardsAsync(AppConfig.CustomFolder);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Fetching custom cards failed");
            throw;
        }

        if (scryfallCards.Count == 0 && customCards.Count == 0)
        {
            logger.LogWarning("No cards fetched from Scryfall or custom folder. Aborting pipeline.");

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Warning: No cards were found. Check your cards.txt and/or custom cards folder.");
            Console.ResetColor();

            throw new PipelineAbortException("No cards found.");
        }

        context.ScryfallCards = scryfallCards;
        context.CustomCards = customCards.ToList();

        logger.LogInformation("Fetched {ScryfallCount} Scryfall card(s) and {CustomCount} custom card(s).", scryfallCards.Count, customCards.Count);
    }
}