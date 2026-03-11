using ScryForge.Steps.Interfaces;
using Microsoft.Extensions.Logging;
using ScryForge.Services.Interfaces;

namespace ScryForge.Steps;

public class CleanUpscaledFolderStep(ICleanupService cleanupService, ILogger<CleanUpscaledFolderStep> logger) : IPipelineStep
{
    public string Name => "Cleaning upscaled folder (excluding flips)";

    public async Task ExecuteAsync(PipelineContext context, CancellationToken ct)
    {
        bool cleanupSucceeded = false;
        try
        {
            cleanupSucceeded = await cleanupService.CleanDirectoryAsync(AppConfig.PDFImagesFolder, "flips");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Cleaning upscaled folder failed unexpectedly");
        }

        logger.LogInformation(cleanupSucceeded ? "Upscaled folder cleaned successfully (excluding flips)." : "Upscaled folder cleanup did not complete or nothing to clean (excluding flips).");
    }
}