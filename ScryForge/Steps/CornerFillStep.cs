using ScryForge.Steps.Interfaces;
using Microsoft.Extensions.Logging;
using ScryForge.Services.Interfaces;

namespace ScryForge.Steps
{
    public class CornerFillStep(ICornerFillService cornerFillService, ILogger<CornerFillStep> logger) : IPipelineStep
    {
        public string Name => "Fill the corners";

        public async Task ExecuteAsync(PipelineContext context, CancellationToken ct)
        {
            try
            {
                await cornerFillService.FillRoundedCornersAsync(AppConfig.PDFImagesFolder, AppConfig.PDFImagesFolder);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Filling corners failed");
            }
        }
    }
}