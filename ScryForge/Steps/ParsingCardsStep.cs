using ScryForge.Models;
using ScryForge.Steps.Interfaces;
using Microsoft.Extensions.Logging;
using ScryForge.Services.Interfaces;

namespace ScryForge.Steps;

public class ParsingCardsStep(ICardParserService cardParserService, ILogger<ParsingCardsStep> logger) : IPipelineStep
{
    public string Name => "Parsing cards.txt";

    public async Task ExecuteAsync(PipelineContext context, CancellationToken ct)
    {
        List<CardInfo> cards = [];
        try
        {
            context.Cards = await cardParserService.ParseCardsAsync(AppConfig.CardsFile);
            logger.LogInformation("Parsed {Count} card(s) from {File}", cards.Count, AppConfig.CardsFile);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Parsing cards.txt failed");
        }
    }
}