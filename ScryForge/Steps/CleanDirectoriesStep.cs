using ScryForge.Steps.Interfaces;
using ScryForge.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace ScryForge.Steps;

public class CleanDirectoriesStep(ICleanupService cleanup, ILogger<CleanDirectoriesStep> logger) : IPipelineStep
{
    public string Name => "Cleaning working directories";

    public async Task ExecuteAsync(PipelineContext context, CancellationToken ct)
    {
        bool downloaderPathCleaned = await cleanup.CleanDirectoryAsync(AppConfig.ScryForgeDownloaderPath);
        logger.LogInformation("Cleaning {Path} {Status}", AppConfig.ScryForgeDownloaderPath, downloaderPathCleaned ? "succeeded" : "failed");

        bool pdfImagesFolderCleaned = await cleanup.CleanDirectoryAsync(AppConfig.PDFImagesFolder);
        logger.LogInformation("Cleaning {Path} {Status}", AppConfig.PDFImagesFolder, pdfImagesFolderCleaned ? "succeeded" : "failed");
    }
}