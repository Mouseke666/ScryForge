using ScryForge.Models;
using Microsoft.Extensions.Logging;
using ScryForge.Services.Interfaces;

namespace ScryForge.Services;

public class CustomCardService(ILogger<CustomCardService> logger) : ICustomCardService
{
    private readonly ILogger<CustomCardService> _logger = logger;

    public Task<IReadOnlyList<CustomCard>> FetchCustomCardsAsync(string customFolder)
    {
        if (string.IsNullOrWhiteSpace(customFolder))
        {
            _logger.LogWarning("Custom folder path is null or empty.");
            return Task.FromResult<IReadOnlyList<CustomCard>>(Array.Empty<CustomCard>());
        }

        if (!Directory.Exists(customFolder))
        {
            _logger.LogWarning("Custom folder does not exist: {CustomFolder}", customFolder);
            return Task.FromResult<IReadOnlyList<CustomCard>>(Array.Empty<CustomCard>());
        }

        var allFiles = Directory
            .EnumerateFiles(customFolder, "*.*", SearchOption.AllDirectories)
            .Where(f => f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
                     || f.EndsWith(".png", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var frontFiles = allFiles
            .Where(f => !Path.GetFileName(f).StartsWith("__back_", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var backFiles = allFiles
            .Where(f => Path.GetFileName(f).StartsWith("__back_", StringComparison.OrdinalIgnoreCase))
            .ToDictionary(
                f => Path.GetFileName(f).Substring(7),
                f => f,
                StringComparer.OrdinalIgnoreCase);

        var cards = new List<CustomCard>();

        foreach (var front in frontFiles)
        {
            string frontFileName = Path.GetFileName(front);

            backFiles.TryGetValue(frontFileName, out var backFile);

            cards.Add(new CustomCard
            {
                FrontLocation = front,
                BackLocation = backFile
            });
        }

        _logger.LogInformation("Loaded {Count} custom cards", cards.Count);
        return Task.FromResult<IReadOnlyList<CustomCard>>(cards);
    }

    public Task CopyCustomCardsAsync(IReadOnlyList<CustomCard> customCards, string targetFolder)
    {
        if (string.IsNullOrWhiteSpace(targetFolder))
        {
            throw new ArgumentException("Target folder path is null or empty.", nameof(targetFolder));
        }

        if (customCards == null || customCards.Count == 0)
        {
            _logger.LogInformation("No custom cards to copy.");
            return Task.CompletedTask;
        }

        Directory.CreateDirectory(targetFolder);
        string flipsFolder = Path.Combine(targetFolder, "flips");
        Directory.CreateDirectory(flipsFolder);

        foreach (var card in customCards)
        {
            if (!File.Exists(card.FrontLocation))
            {
                _logger.LogWarning("File does not exist: {FilePath}", card.FrontLocation);
                continue;
            }

            string frontFileName = Path.GetFileName(card.FrontLocation);
            string frontDest = card.BackLocation != null
                ? Path.Combine(flipsFolder, frontFileName)
                : Path.Combine(targetFolder, frontFileName);

            try
            {
                File.Copy(card.FrontLocation, frontDest, overwrite: true);
                _logger.LogInformation("Copied {File} to {Destination}", frontFileName, frontDest);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to copy {File} to {Destination}", frontFileName, frontDest);
            }

            if (card.BackLocation != null && File.Exists(card.BackLocation))
            {
                string backFileName = Path.GetFileName(card.BackLocation);
                string backDest = Path.Combine(flipsFolder, backFileName);

                try
                {
                    File.Copy(card.BackLocation, backDest, overwrite: true);
                    _logger.LogInformation("Copied back {File} to {Destination}", backFileName, backDest);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to copy back {File} to {Destination}", backFileName, backDest);
                }
            }
        }

        return Task.CompletedTask;
    }
}