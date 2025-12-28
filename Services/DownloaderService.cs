using System.Net;
using ScryForge.Models;
using System.Text.Json;
using ScryForge.Serialization;
using Microsoft.Extensions.Logging;
using ScryForge.Services.Intefaces;
using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace ScryForge.Services;

public class DownloaderService : IDownloaderService
{
    private readonly HttpClient _http;
    private readonly ILogger<DownloaderService> _logger;
    private readonly string _outputFolder;
    private readonly int _maxConcurrentDownloads = 10;

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

    public async Task<IReadOnlyList<ScryfallCard>> FetchScryfallCardsAsync(CancellationToken ct = default)
    {
        var result = new ConcurrentBag<ScryfallCard>();
        var cardsFile = Path.Combine(AppConfig.BasePath, "cards.txt");

        if (!File.Exists(cardsFile))
        {
            _logger.LogError("cards.txt not found at: {Path}", cardsFile);
            return Array.Empty<ScryfallCard>();
        }

        string[] lines = await File.ReadAllLinesAsync(cardsFile, ct);

        var fetchTasks = new List<Task>();

        foreach (string rawLine in lines)
        {
            ct.ThrowIfCancellationRequested();

            string line = rawLine.Trim();
            if (string.IsNullOrWhiteSpace(line))
                continue;

            if (!TryParseLine(line, out var name, out var set, out var cn))
            {
                _logger.LogWarning("Could not parse line: {Line}", rawLine);
                continue;
            }

            fetchTasks.Add(FetchAndAddCardAsync(name, set, cn, result, ct));
        }

        await Task.WhenAll(fetchTasks);
        return result.ToList();
    }

    public async Task DownloadImagesAsync(IReadOnlyList<ScryfallCard> cards, CancellationToken ct = default)
    {
        if (!cards.Any())
        {
            _logger.LogInformation("No cards to download images for.");
            return;
        }

        using var semaphore = new SemaphoreSlim(_maxConcurrentDownloads);

        var downloadTasks = new List<Task>();

        foreach (var card in cards)
        {
            await semaphore.WaitAsync(ct);

            downloadTasks.Add(
                Task.Run(async () =>
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

        await Task.WhenAll(downloadTasks);
    }

    private async Task FetchAndAddCardAsync(
        string name,
        string? set,
        string? cn,
        ConcurrentBag<ScryfallCard> result,
        CancellationToken ct)
    {
        string? json = await FetchCardJsonWithRetryAsync(new CardRequest(name, set, cn), ct);

        if (json == null)
            return;

        var card = ParseCard(json);
        if (card != null)
        {
            result.Add(card);
        }
    }

    private async Task<string?> FetchCardJsonWithRetryAsync(CardRequest req, CancellationToken ct, int maxRetries = 3)
    {
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            ct.ThrowIfCancellationRequested();

            string url =
                req.SetCode != null && req.CollectorNumber != null
                    ? $"cards/{req.SetCode.ToLowerInvariant()}/{req.CollectorNumber.ToLowerInvariant()}"
                        : $"cards/named?fuzzy={WebUtility.UrlEncode(req.Name.ToLowerInvariant())}";

            try
            {
                using var response = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, ct);

                if (response.IsSuccessStatusCode)
                {
                    return await response.Content.ReadAsStringAsync(ct);
                }

                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    _logger.LogWarning("Card not found: {Name} [{Set} {Cn}]", req.Name, req.SetCode, req.CollectorNumber);
                    return null;
                }

                if ((int)response.StatusCode == 429 && attempt < maxRetries)
                {
                    _logger.LogWarning("Rate limited by Scryfall (attempt {Attempt}/{Max}). Waiting 100ms...", attempt, maxRetries);
                    await Task.Delay(100, ct);
                    continue;
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning(ex, "Network error fetching card data (attempt {Attempt}/{Max}): {Name}", attempt, maxRetries, req.Name);
                if (attempt < maxRetries)
                {
                    await Task.Delay(200 * attempt, ct);
                    continue;
                }
            }
            catch (TaskCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
        }

        _logger.LogError("Failed to fetch card after {MaxRetries} attempts: {Name} [{Set} {Cn}]", maxRetries, req.Name, req.SetCode, req.CollectorNumber);
        return null;
    }

    private static ScryfallCard? ParseCard(string json)
    {
        try
        {
            return JsonSerializer.Deserialize(json, ScryfallJsonContext.Default.ScryfallCard);
        }
        catch
        {
            return null;
        }
    }

    private async Task ProcessCardAsync(ScryfallCard card, CancellationToken ct)
    {
        if (card.ImageUris != null)
        {
            await DownloadSingleImageAsync(card.ImageUris, card.Name, card.Set, card.CollectorNumber, null, ct);
            return;
        }

        if (card.Layout == "adventure" && card.CardFaces?.Any() == true)
        {
            var front = card.CardFaces.First();
            if (front.ImageUris != null)
            {
                await DownloadSingleImageAsync(front.ImageUris, front.Name, card.Set, card.CollectorNumber, "front", ct);
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

                string suffix = index == 0 ? "front" : "back";
                await DownloadSingleImageAsync(face.ImageUris, face.Name, card.Set, card.CollectorNumber, suffix, ct);
                index++;
            }
        }
    }

    private async Task DownloadSingleImageAsync(
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

        if (File.Exists(fullPath))
            return;

        try
        {
            byte[] bytes = await _http.GetByteArrayAsync(imageUrl, ct);
            await File.WriteAllBytesAsync(fullPath, bytes, ct);

            _logger.LogInformation("Downloaded → {File}", fileName);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to download image for {Name} ({Url})", cardName, imageUrl);
        }
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
            setCode = match.Groups[2].Value.Trim().ToUpperInvariant();

        if (match.Groups[3].Success)
            collectorNumber = match.Groups[3].Value.Trim().ToUpperInvariant();

        return true;
    }

    private static string SanitizeFileName(string name) =>
        string.Join("_", name.Split(Path.GetInvalidFileNameChars()));
}