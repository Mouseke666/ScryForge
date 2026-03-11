using ScryForge.Steps.Interfaces;
using Microsoft.Extensions.Logging;
using ScryForge.Services.Interfaces;

namespace ScryForge.Steps;

public class GenerateMainPDFStep(IPDFService pdfService, ILogger<GenerateMainPDFStep> logger) : IPipelineStep
{
    public string Name => "Generating main PDF";

    public async Task ExecuteAsync(PipelineContext context, CancellationToken ct)
    {
        bool mainPdfGenerated = false;
        try
        {
            mainPdfGenerated = await pdfService.GenerateMainPdfAsync(context.FullName!, context.Cards, false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Generating main PDF failed unexpectedly");
        }

        if (mainPdfGenerated)
        {
            logger.LogInformation("Main PDF successfully generated: {Pdf}.pdf", context.FullName);
        }
        else
        {
            logger.LogWarning("Main PDF was not generated: {Pdf}.pdf", context.FullName);
        }
    }
}