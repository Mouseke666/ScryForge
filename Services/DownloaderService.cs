using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using ScryForge.Models;
using ScryForge.Serialization;

namespace ScryForge.Services;

public class DownloaderService : IDownloaderService
{
    private readonly HttpClient _http;
    private readonly ILogger<DownloaderService> _logger;
    private readonly string _outputFolder;

    public DownloaderService(
        IHttpClientFactory httpClientFactory,
        ILogger<DownloaderService> logger)
    {
        _logger = logger;

        _http = httpClientFactory.CreateClient("Scryfall");
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("ScryForge/1.0 (jouw@email.com)");

        _outputFolder = AppConfig.ScryForgeDownloaderPath;
        Directory.CreateDirectory(_outputFolder);
    }

    // ==========================
    // PUBLIC API
    // ==========================
    public async Task<IReadOnlyList<ScryfallCard>> FetchScryfallCardsAsync()
    {
        var result = new List<ScryfallCard>();
        var cardsFile = Path.Combine(AppConfig.BasePath, "cards.txt");

        if (!File.Exists(cardsFile))
        {
            _logger.LogError("cards.txt not found: {Path}", cardsFile);
            return result;
        }

        var lines = await File.ReadAllLinesAsync(cardsFile);

        foreach (var line in lines.Select(l => l.Trim()).Where(l => !string.IsNullOrWhiteSpace(l)))
        {
            if (!TryParseLine(line, out var name, out var set, out var cn))
            {
                _logger.LogWarning("Line skipped: {Line}", line);
                continue;
            }

            var json = await FetchCardJsonAsync(new CardRequest(name, set, cn));
            if (json == null)
                continue;

            var card = ParseCard(json);
            if (card != null)
                result.Add(card);
        }

        return result;
    }

    public async Task DownloadImagesAsync(IEnumerable<ScryfallCard> cards)
    {
        foreach (var card in cards)
        {
            await ProcessCardAsync(card);
        }
    }

    // ==========================
    // JSON FETCHING
    // ==========================
    private async Task<string?> FetchCardJsonAsync(CardRequest req)
    {
        var url = req.SetCode != null && req.CollectorNumber != null
            ? $"cards/{req.SetCode.ToLower()}/{req.CollectorNumber}"
            : $"cards/named?fuzzy={Uri.EscapeDataString(req.Name)}";

        var response = await _http.GetAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning(
                "Not found: {Name} [{Set} {Cn}] → {Status}",
                req.Name, req.SetCode, req.CollectorNumber, response.StatusCode);
            return null;
        }

        return await response.Content.ReadAsStringAsync();
    }

    private static ScryfallCard? ParseCard(string json)
    {
        try
        {
            return JsonSerializer.Deserialize(
                json,
                ScryfallJsonContext.Default.ScryfallCard);
        }
        catch
        {
            return null;
        }
    }

    // ==========================
    // IMAGE DOWNLOADING
    // ==========================
    private async Task ProcessCardAsync(ScryfallCard card)
    {
        if (card.ImageUris != null)
        {
            await DownloadSingleImageAsync(
                card.ImageUris,
                card.Name,
                card.Set,
                card.CollectorNumber);
            return;
        }

        if (card.Layout == "adventure" && card.CardFaces?.Any() == true)
        {
            var front = card.CardFaces.First();
            if (front.ImageUris != null)
            {
                await DownloadSingleImageAsync(
                    front.ImageUris,
                    front.Name,
                    card.Set,
                    card.CollectorNumber,
                    "front");
            }
            return;
        }

        if (card.CardFaces != null)
        {
            int index = 0;
            foreach (var face in card.CardFaces)
            {
                if (face.ImageUris == null)
                    continue;

                var suffix = index == 0 ? "front" : "back";

                await DownloadSingleImageAsync(
                    face.ImageUris,
                    face.Name,
                    card.Set,
                    card.CollectorNumber,
                    suffix);

                index++;
            }
        }
    }

    private async Task DownloadSingleImageAsync(
        ImageUris imageUris,
        string cardName,
        string setCode,
        string collectorNumber,
        string? faceSuffix = null)
    {
        var imageUrl = GetBestImageUrl(imageUris);
        var extension = Path.GetExtension(new Uri(imageUrl).AbsolutePath);

        var safeName = SanitizeFileName(cardName);

        var fileName = faceSuffix == null
            ? $"{safeName}_{setCode.ToUpper()}_{collectorNumber}{extension}"
            : $"{safeName}_{setCode.ToUpper()}_{collectorNumber}_{faceSuffix}{extension}";

        var fullPath = Path.Combine(_outputFolder, fileName);

        if (File.Exists(fullPath))
            return;

        var bytes = await _http.GetByteArrayAsync(imageUrl);
        await File.WriteAllBytesAsync(fullPath, bytes);

        _logger.LogInformation("Downloaded → {File}", fileName);
    }

    // ==========================
    // HELPERS
    // ==========================
    private static string GetBestImageUrl(ImageUris u) =>
        u.Png
        ?? u.BorderCrop
        ?? u.ArtCrop
        ?? u.Large
        ?? u.Normal
        ?? u.Small
        ?? throw new InvalidOperationException("No valid Scryfall image URL");

    private static bool TryParseLine(
        string line,
        out string name,
        out string? setCode,
        out string? collectorNumber)
    {
        name = "";
        setCode = null;
        collectorNumber = null;

        line = Regex.Replace(line, @"\*F\*\s*$", "", RegexOptions.IgnoreCase).Trim();

        var match = Regex.Match(
            line,
            @"^\s*(?:\d+\s+)?(.+?)\s*(?:\(\s*([A-Z0-9]{2,5})\s*\))?\s*([0-9A-Z\-]+)?\s*$",
            RegexOptions.IgnoreCase);

        if (!match.Success)
            return false;

        name = match.Groups[1].Value.Trim();

        if (match.Groups[2].Success)
            setCode = match.Groups[2].Value.Trim().ToUpper();

        if (match.Groups[3].Success)
            collectorNumber = match.Groups[3].Value.Trim();

        return true;
    }

    private static string SanitizeFileName(string name) =>
        string.Join("_", name.Split(Path.GetInvalidFileNameChars()));
}
