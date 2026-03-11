using ScryForge.Models;
using ScryForge.Steps.Interfaces;
using Microsoft.Extensions.Logging;
using ScryForge.Services.Interfaces;

namespace ScryForge.Steps;

public class ParsingCustomCardsStep(ICardParserService cardParserService, ILogger<ParsingCustomCardsStep> logger) : IPipelineStep
{
    public string Name => "Parsing custom cards";

    public async Task ExecuteAsync(PipelineContext context, CancellationToken ct)
    {
        List<CardInfo> parsedCustomCards = await cardParserService.ParseCustomCardsAsync(context.CustomCards);
        if (parsedCustomCards.Count > 0)
        {
            logger.LogInformation($"Parsed {parsedCustomCards.Count} custom card(s) to parse");
            context.Cards.AddRange(parsedCustomCards);
        }
        else
        {
            logger.LogInformation("No custom card(s) to parse");
        }
    }
}