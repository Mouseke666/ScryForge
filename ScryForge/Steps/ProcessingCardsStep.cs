using ScryForge.Steps.Interfaces;
using Microsoft.Extensions.Logging;
using ScryForge.Services.Interfaces;

namespace ScryForge.Steps;

public class ProcessingCardsStep(ICardCopyService cardCopyService, ILogger<ProcessingCardsStep> logger) : IPipelineStep
{
    public string Name => "Processing cards";

    public async Task ExecuteAsync(PipelineContext context, CancellationToken ct)
    {
        try
        {
            var result = cardCopyService.ProcessCards(context.Cards);
            logger.LogInformation("Processed {Total} cards: {Flip} flip card(s), {Single} single-sided card(s)",
                result.TotalCards, result.FlipCardsProcessed, result.SingleCardsProcessed);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Processing cards failed");
        }
    }
}