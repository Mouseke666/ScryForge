using ScryForge.Steps.Interfaces;
using Microsoft.Extensions.Logging;
using ScryForge.Services.Interfaces;

namespace ScryForge.Steps;

public class CopyCustomCardsStep(ICustomCardService customCards, ILogger<CopyCustomCardsStep> logger) : IPipelineStep
{
    public string Name => "Copy custom cards";

    public async Task ExecuteAsync(PipelineContext context, CancellationToken ct)
    {
        await customCards.CopyCustomCardsAsync(context.CustomCards, AppConfig.PDFImagesFolder);
    }
}
