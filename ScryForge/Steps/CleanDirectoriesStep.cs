using ScryForge.Models;
using ScryForge.Steps.Interfaces;
using Microsoft.Extensions.Logging;
using ScryForge.Services.Interfaces;

namespace ScryForge.Steps;

public class CleanDirectoriesStep(ICleanupService cleanup, ILogger<CleanDirectoriesStep> logger) : IPipelineStep
{
    public string Name => "Cleaning working directories";

    public async Task ExecuteAsync(PipelineContext context, CancellationToken ct)
    {
        bool downloaderPathCleaned = await cleanup.CleanDirectoryAsync(AppConfig.ScryForgeDownloaderPath, ct: ct);
        logger.LogInformation("Cleaning {Path} {Status}", AppConfig.ScryForgeDownloaderPath, downloaderPathCleaned ? "succeeded" : "failed");

        bool pdfImagesFolderCleaned = await cleanup.CleanDirectoryAsync(AppConfig.PDFImagesFolder, ct: ct);
        logger.LogInformation("Cleaning {Path} {Status}", AppConfig.PDFImagesFolder, pdfImagesFolderCleaned ? "succeeded" : "failed");

        var failedDirectories = new List<string>();
        if (!downloaderPathCleaned) failedDirectories.Add(AppConfig.ScryForgeDownloaderPath);
        if (!pdfImagesFolderCleaned) failedDirectories.Add(AppConfig.PDFImagesFolder);

        if (failedDirectories.Any())
            throw new PipelineAbortException(
                $"The following directories could not be cleaned: {string.Join(", ", failedDirectories)}");
    }
}