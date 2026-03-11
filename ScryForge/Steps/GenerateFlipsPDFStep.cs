using ScryForge.Steps.Interfaces;
using Microsoft.Extensions.Logging;
using ScryForge.Services.Interfaces;

namespace ScryForge.Steps;

public class GenerateFlipsPDFStep(IPDFService pdfService, IOpenFolderService openFolderService, ILogger<GenerateFlipsPDFStep> logger) : IPipelineStep
{
    public string Name => "Generating flips PDF if required";

    public async Task ExecuteAsync(PipelineContext context, CancellationToken ct)
    {
        bool flipsPdfGenerated = false;
        try
        {
            flipsPdfGenerated = await pdfService.GenerateFlipsPdfAsync(context.FullName!, false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Generating flips PDF failed unexpectedly");
        }

        logger.LogInformation(flipsPdfGenerated ? $"Flips PDF successfully generated: {context.FullName!}_flips.pdf" : "No flips PDF was generated.");

        try
        {
            openFolderService.OpenFolder(AppConfig.OutputFolder);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Opening folder failed");
        }
    }
}