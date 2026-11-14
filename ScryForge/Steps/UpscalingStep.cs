using ScryForge.Steps.Interfaces;
using Microsoft.Extensions.Logging;
using ScryForge.Services.Interfaces;

namespace ScryForge.Steps;

public class UpscalingStep(IUpscalerService upscalerService, ILogger<UpscalingStep> logger) : IPipelineStep
{
    public string Name => "Upscaling images";

    public async Task ExecuteAsync(PipelineContext context, CancellationToken ct)
    {
        if (AppConfig.TestUpscalers)
        {
            await RunTestUpscalerModeAsync(context, ct);
        }
        else
        {
            await RunNormalModeAsync(context, ct);
        }
    }

    private async Task RunNormalModeAsync(PipelineContext context, CancellationToken ct)
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

    private async Task RunTestUpscalerModeAsync(PipelineContext context, CancellationToken ct)
    {
        var cards = context.ScryfallCards.ToList();
        if (cards.Count == 0)
        {
            logger.LogInformation("No Scryfall cards to upscale in TestUpscalers mode. Skipping.");
            return;
        }

        // Discover models from the models folder instead of using config
        var upscalerModels = AppConfig.DiscoverUpscalerModels();
        if (upscalerModels.Count == 0)
        {
            logger.LogWarning("No upscaler models found in {ModelPath}. Skipping TestUpscalers mode.", AppConfig.UpscalerPath);
            return;
        }

        int totalImages = cards.Sum(c =>
        {
            int count = 0;
            if (!string.IsNullOrWhiteSpace(c.FrontImagePath)) count++;
            if (!string.IsNullOrWhiteSpace(c.BackImagePath)) count++;
            return count;
        });

        int totalUpscalerRuns = totalImages * upscalerModels.Count;
        int currentRun = 0;

        logger.LogInformation(
            "TestUpscalers mode: upscaling {TotalImages} image(s) with {UpscalerCount} upscaler model(s). Total runs: {Total}",
            totalImages, upscalerModels.Count, totalUpscalerRuns);

        foreach (var card in cards)
        {
            if (ct.IsCancellationRequested) break;

            var imagePaths = new List<string>();
            if (!string.IsNullOrWhiteSpace(card.FrontImagePath))
                imagePaths.Add(card.FrontImagePath);
            if (!string.IsNullOrWhiteSpace(card.BackImagePath))
                imagePaths.Add(card.BackImagePath);

            foreach (var imagePath in imagePaths)
            {
                foreach (var model in upscalerModels)
                {
                    currentRun++;

                    string modelSubfolder = Path.Combine(AppConfig.PDFImagesFolder, model.Model);
                    Directory.CreateDirectory(modelSubfolder);

                    logger.LogInformation(
                        "TestUpscalers [{Current}/{Total}] - Card: {CardName}, Upscaler: {Model} (scale {Scale})",
                        currentRun, totalUpscalerRuns, card.Name, model.Model, model.Scale);

                    bool upscaled = await upscalerService.RunUpscalerForCardsAsync(
                        new[] { card }, model.Model, model.Scale, modelSubfolder);

                    if (!upscaled)
                    {
                        logger.LogWarning(
                            "TestUpscalers [{Current}/{Total}] - Failed to upscale {CardName} with {Model}",
                            currentRun, totalUpscalerRuns, card.Name, model.Model);
                    }
                }
            }
        }

        // Copy upscaled images from model subfolders to root so the CLI tool can find them.
        // Files are prefixed with model name to preserve all variants (e.g., 
        // "uniscale_restore_MIS_aetherize.png", "digital-art-4x_MIS_aetherize.png").
        // Track original filename -> upscaled filenames mapping for context.Cards update.
        var originalToUpscaled = new Dictionary<string, List<string>>(StringComparer.OrdinalIgnoreCase);

        foreach (var model in upscalerModels)
        {
            string modelFolder = Path.Combine(AppConfig.PDFImagesFolder, model.Model);
            if (!Directory.Exists(modelFolder)) continue;

            foreach (var file in Directory.EnumerateFiles(modelFolder, "*.*", SearchOption.TopDirectoryOnly))
            {
                try
                {
                    string originalFileName = Path.GetFileName(file); // e.g., "Wheel of Fortune_LEB_184.png"
                    string baseName = Path.GetFileNameWithoutExtension(file);
                    string extension = Path.GetExtension(file);
                    string upscaledFileName = $"{model.Model}_{baseName}{extension}"; // e.g., "digital-art-4x_Wheel of Fortune_LEB_184.png"
                    File.Copy(file, Path.Combine(AppConfig.PDFImagesFolder, upscaledFileName), true);

                    // Track mapping from original filename to upscaled filenames.
                    if (!originalToUpscaled.TryGetValue(originalFileName, out var list))
                    {
                        list = new List<string>();
                        originalToUpscaled[originalFileName] = list;
                    }
                    list.Add(upscaledFileName);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to copy upscaled image from {Model} subfolder: {FileName}", model.Model, Path.GetFileName(file));
                }
            }
        }

        // Populate context.Cards from ScryfallCards for PDF generation.
        // FrontFileName/BackFileName point to the first (alphabetically) model-prefixed upscaled file
        // so the CLI tool can find them in PDF/images/.
        foreach (var card in cards)
        {
            string? GetUpscaledFileName(string imagePath, string side)
            {
                if (string.IsNullOrEmpty(imagePath)) return null;

                var originalFileName = Path.GetFileName(imagePath);
                if (originalToUpscaled.TryGetValue(originalFileName, out var upscaledList))
                {
                    // Return first alphabetically for consistency.
                    return upscaledList.OrderBy(f => f).FirstOrDefault();
                }

                logger.LogWarning(
                    "No upscaled variant found for card {Card} {Side} file: {FileName}",
                    card.Name, side, originalFileName);
                return null;
            }

            var frontFileName = GetUpscaledFileName(card.FrontImagePath!, "front");
            var backFileName = GetUpscaledFileName(card.BackImagePath!, "back");

            var cardInfo = new ScryForge.Models.CardInfo
            {
                Quantity = 1,
                Name = card.Name,
                SetCode = card.Set ?? "UNKNOWN",
                Number = card.CollectorNumber ?? "0",
                FrontFileName = frontFileName ?? (card.FrontImagePath != null ? Path.GetFileName(card.FrontImagePath) : string.Empty),
                BackFileName = backFileName ?? (card.BackImagePath != null ? Path.GetFileName(card.BackImagePath) : string.Empty)
            };
            context.Cards.Add(cardInfo);
        }

        logger.LogInformation("TestUpscalers mode completed. All {TotalImages} image(s) processed.", totalImages);
    }
}
