using ScryForge.Steps.Interfaces;
using Microsoft.Extensions.Logging;
using ScryForge.Services.Interfaces;

namespace ScryForge.Steps;

public class DownloadCardImagesStep(IDownloaderService downloaderService, ILogger<DownloadCardImagesStep> logger) : IPipelineStep
{
    public string Name => "Downloading card images";

    public async Task ExecuteAsync(PipelineContext context, CancellationToken ct)
    {
        try
        {
            await downloaderService.DownloadImagesAsync(context.ScryfallCards);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Downloading images failed");
        }
    }
}