using ScryForge.Steps.Interfaces;
using Microsoft.Extensions.Logging;
using ScryForge.Services.Interfaces;

namespace ScryForge.Steps;

public class UpscalingStep(IUpscalerService upscalerService, ILogger<UpscalingStep> logger) : IPipelineStep
{
    public string Name => "Upscaling images";

    public async Task ExecuteAsync(PipelineContext context, CancellationToken ct)
    {
        var lastUpscaler = AppConfig.Upscalers.Last();

        int count = 0;
        bool anyUpscaled = false;
        foreach (var upscaler in AppConfig.Upscalers)
        {
            if (count > 0) Console.Write(Environment.NewLine);
            var cardsForThisUpscaler = context.ScryfallCards
                .Where(c =>
                    c.ReleasedAt.HasValue &&
                    (!upscaler.YearRange.From.HasValue || c.ReleasedAt.Value.Year >= upscaler.YearRange.From.Value) &&
                    (!upscaler.YearRange.To.HasValue || c.ReleasedAt.Value.Year <= upscaler.YearRange.To.Value))
                .ToList();
            if (upscaler == lastUpscaler)
                cardsForThisUpscaler.AddRange(context.CardsWithoutReleaseDate);

            if (cardsForThisUpscaler.Count == 0) continue;

            logger.LogInformation("Running upscaler {Model} on {Count} cards\n", upscaler.Model, cardsForThisUpscaler.Count);
            bool upscaled = await upscalerService.RunUpscalerForCardsAsync(cardsForThisUpscaler, upscaler.Model, upscaler.Scale);
            if (upscaled) anyUpscaled = true;
            count++;
        }
        if (!anyUpscaled)
            logger.LogInformation("No card images available to upscale. Skipping upscaling step.");
    }
}
