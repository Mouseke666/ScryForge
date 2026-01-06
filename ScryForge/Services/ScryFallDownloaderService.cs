using System.Net;
using System.Text.Json;
using ScryForge.Models.Scryfall;
using Microsoft.Extensions.Logging;
using ScryForge.Services.Interfaces;
using System.Text.RegularExpressions;
using ScryForge.Models.Scryfall.Serialization;

namespace ScryForge.Services;

public class ScryFallDownloaderService : IDownloaderService
{
    private readonly HttpClient _http;
    private readonly ILogger<ScryFallDownloaderService> _logger;
    private readonly string _outputFolder;
    private readonly int _maxConcurrentDownloads = 10;

    private int _downloadedCount = 0;
    private int _totalCount = 0;

    public ScryFallDownloaderService(
        IHttpClientFactory httpClientFactory,
        ILogger<ScryFallDownloaderService> logger)
    {
        _logger = logger;

        _http = httpClientFactory.CreateClient("Scryfall");
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("ScryForge/1.0 (jouw@email.com)");

        _outputFolder = AppConfig.ScryForgeDownloaderPath;
        Directory.CreateDirectory(_outputFolder);
    }

    public async Task<IReadOnlyList<ScryfallCard>> FetchCardsAsync(CancellationToken ct = default)
    {
        var cardsFile = Path.Combine(AppConfig.BasePath, "cards.txt");

        if (!File.Exists(cardsFile))
        {
            _logger.LogError("cards.txt not found at: {Path}", cardsFile);
            return [];
        }

        string[] lines = await File.ReadAllLinesAsync(cardsFile, ct);

        var aggregatedCards = new Dictionary<string, (int Quantity, string Name, string? Set, string? CN)>(
            StringComparer.OrdinalIgnoreCase);

        foreach (string rawLine in lines)
        {
            ct.ThrowIfCancellationRequested();

            string line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line))
                continue;

            if (!TryParseLine(line, out var quantity, out var name, out var set, out var cn))
            {
                _logger.LogWarning("Could not parse line: {Line}", rawLine);
                continue;
            }

            string key = $"{name}||{set}||{cn}";

            if (aggregatedCards.TryGetValue(key, out var existing))
            {
                aggregatedCards[key] = (existing.Quantity + quantity, name, set, cn);
            }
            else
            {
                aggregatedCards[key] = (quantity, name, set, cn);
            }
        }

        var result = new List<ScryfallCard>();

        var fetchTasks = aggregatedCards.Values.Select(entry =>
            FetchAndAddCardAsync(entry.Quantity, entry.Name, entry.Set, entry.CN, result, ct));

        await Task.WhenAll(fetchTasks);

        return result;
    }

    public async Task DownloadImagesAsync(IReadOnlyList<ScryfallCard> cards, CancellationToken ct = default)
    {
        if (!cards.Any())
        {
            _logger.LogInformation("No cards to download images for.");
            return;
        }

        _totalCount = cards.Sum(c =>
            (c.ImageUris != null ? 1 : 0) +
            (c.CardFaces?.Count(cf => cf.ImageUris != null) ?? 0)
        );

        using var semaphore = new SemaphoreSlim(_maxConcurrentDownloads);
        var tasks = new List<Task>();

        foreach (var card in cards)
        {
            await semaphore.WaitAsync(ct);

            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    await ProcessCardAsync(card, ct);
                }
                finally
                {
                    semaphore.Release();
                }
            }, ct));
        }

        await Task.WhenAll(tasks);
    }

    private async Task FetchAndAddCardAsync(
        int quantity,
        string name,
        string? set,
        string? cn,
        List<ScryfallCard> result,
        CancellationToken ct)
    {
        string? json = await FetchCardJsonWithRetryAsync(new CardRequest(name, set, cn), ct);
        if (json == null)
            return;

        var card = ParseCard(json);
        if (card != null)
        {
            card.Quantity = quantity;
            lock (result) result.Add(card);
        }
    }

    private static ScryfallCard? ParseCard(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            if (root.TryGetProperty("id", out _))
            {
                return JsonSerializer.Deserialize(json, ScryfallJsonContext.Default.ScryfallCard);
            }

            if (root.TryGetProperty("data", out var dataElement) &&
                dataElement.ValueKind == JsonValueKind.Array &&
                dataElement.GetArrayLength() > 0)
            {
                var firstCardJson = dataElement[0].GetRawText();
                return JsonSerializer.Deserialize(firstCardJson, ScryfallJsonContext.Default.ScryfallCard);
            }

            return null;
        }
        catch
        {
            return null;
        }
    }

    private async Task<string?> FetchCardJsonWithRetryAsync(
        CardRequest req,
        CancellationToken ct,
        int maxRetries = 3)
    {
        ct.ThrowIfCancellationRequested();

        var urls = BuildCandidateUrls(req).ToList();

        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            foreach (var url in urls)
            {
                try
                {
                    using var response = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);

                    if (response.IsSuccessStatusCode)
                        return await response.Content.ReadAsStringAsync(ct);

                    if (response.StatusCode == HttpStatusCode.NotFound)
                        continue;

                    if ((int)response.StatusCode == 429 && attempt < maxRetries)
                    {
                        await Task.Delay(200, ct);
                        break;
                    }
                }
                catch (HttpRequestException) when (attempt < maxRetries)
                {
                    await Task.Delay(200 * attempt, ct);
                    break;
                }
            }
        }

        _logger.LogError(
            "Failed to fetch card after {MaxRetries} attempts: {Name} [{Set} {Cn}]",
            maxRetries,
            req.Name,
            req.SetCode,
            req.CollectorNumber);

        return null;
    }

    private async Task ProcessCardAsync(ScryfallCard card, CancellationToken ct)
    {
        if (card.ImageUris != null)
        {
            await DownloadSingleImageAsync(card, card.ImageUris, card.Name, card.Set, card.CollectorNumber, null, ct);
            return;
        }

        if (card.CardFaces != null)
        {
            int index = 0;
            foreach (var face in card.CardFaces)
            {
                if (face.ImageUris == null)
                    continue;

                string suffix = index == 0 ? "Front" : "Back";

                await DownloadSingleImageAsync(card, face.ImageUris, face.Name, card.Set, card.CollectorNumber, suffix, ct);
                index++;
            }
        }
    }

    private async Task DownloadSingleImageAsync(
        ScryfallCard card,
        ImageUris imageUris,
        string cardName,
        string setCode,
        string collectorNumber,
        string? faceSuffix,
        CancellationToken ct)
    {
        string imageUrl = GetBestImageUrl(imageUris);
        string extension = Path.GetExtension(new Uri(imageUrl).AbsolutePath);
        string safeName = SanitizeFileName(cardName);

        string fileName = faceSuffix == null
            ? $"{safeName}_{setCode.ToUpper()}_{collectorNumber}{extension}"
            : $"{safeName}_{setCode.ToUpper()}_{collectorNumber}_{faceSuffix}{extension}";

        string fullPath = Path.Combine(_outputFolder, fileName);

        if (!File.Exists(fullPath))
        {
            byte[] bytes = await _http.GetByteArrayAsync(imageUrl, ct);
            await File.WriteAllBytesAsync(fullPath, bytes, ct);
        }

        if (faceSuffix == "Back")
            card.BackImagePath = fullPath;
        else
            card.FrontImagePath = fullPath;

        int current = Interlocked.Increment(ref _downloadedCount);

        string displayName = faceSuffix == null ? card.Name : $"{card.Name} ({faceSuffix})";
        _logger.LogInformation("Downloaded [{Current}/{Total}] — {Name}", current, _totalCount, displayName);
    }

    private static string GetBestImageUrl(ImageUris u) =>
        u.Png
        ?? u.BorderCrop
        ?? u.ArtCrop
        ?? u.Large
        ?? u.Normal
        ?? u.Small
        ?? throw new InvalidOperationException("No valid Scryfall image URL found");

    private static bool TryParseLine(
        string line,
        out int quantity,
        out string name,
        out string? setCode,
        out string? collectorNumber)
    {
        quantity = 1;
        name = "";
        setCode = null;
        collectorNumber = null;

        line = Regex.Replace(line, @"\*F\*\s*$", "", RegexOptions.IgnoreCase).Trim();

        var match = Regex.Match(
            line,
            @"^\s*(?:(\d+)\s+)?(.+?)\s*(?:\(\s*([A-Z0-9]{2,5})\s*\))?\s*([0-9A-Z\-]+)?\s*$",
            RegexOptions.IgnoreCase);

        if (!match.Success)
            return false;

        if (match.Groups[1].Success)
            quantity = int.Parse(match.Groups[1].Value);

        name = match.Groups[2].Value.Trim();

        if (match.Groups[3].Success)
            setCode = match.Groups[3].Value.Trim().ToUpperInvariant();

        if (match.Groups[4].Success)
            collectorNumber = match.Groups[4].Value.Trim().ToUpperInvariant();

        return true;
    }

    private static string SanitizeFileName(string name) =>
        string.Join("_", name.Split(Path.GetInvalidFileNameChars()));

    private IEnumerable<string> BuildCandidateUrls(CardRequest req)
    {
        if (!string.IsNullOrWhiteSpace(req.SetCode) &&
            !string.IsNullOrWhiteSpace(req.CollectorNumber))
        {
            yield return
                $"cards/{req.SetCode.ToLowerInvariant()}/" +
                $"{WebUtility.UrlEncode(req.CollectorNumber)}";

            yield return
                $"cards/search?q=" +
                $"set:{req.SetCode.ToLowerInvariant()}+" +
                $"cn:\"{EscapeQuery(req.CollectorNumber)}\"";
        }
    }

    private static string EscapeQuery(string value) =>
        value.Replace("\"", "\\\"");
}
