using System.Text.Json;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Text;
using System.Globalization;

namespace ScryForge.Services;

public class ScryForgeDownloaderService : IDownloaderService
{
    private readonly HttpClient _http;
    private readonly ILogger<ScryForgeDownloaderService> _logger;
    private readonly string _outputFolder;

    public ScryForgeDownloaderService(IHttpClientFactory httpClientFactory, ILogger<ScryForgeDownloaderService> logger)
    {
        _logger = logger;

        _http = httpClientFactory.CreateClient("Scryfall");
        _http.DefaultRequestHeaders.UserAgent.ParseAdd("ScryForge/1.0 (jouw@email.com)");

        _outputFolder = AppConfig.ScryForgeDownloaderPath;

        Directory.CreateDirectory(_outputFolder);
    }

    public async Task<bool> DownloadArtAsync()
    {
        var cardsFile = Path.Combine(AppConfig.BasePath, "cards.txt");
        if (!File.Exists(cardsFile))
        {
            _logger.LogError("cards.txt not found: {Path}", cardsFile);
            return false;
        }

        var lines = await File.ReadAllLinesAsync(cardsFile);
        var requests = new List<CardRequest>();

        foreach (var line in lines.Select(l => l.Trim()).Where(l => !string.IsNullOrWhiteSpace(l)))
        {
            if (TryParseLine(line, out var name, out var set, out var cn))
            {
                requests.Add(new CardRequest(name, set, cn));
            }
            else
            {
                _logger.LogWarning("Line skipped (not recognized): {Line}", line);
            }
        }

        if (requests.Count == 0)
        {
            _logger.LogWarning("No card lines found in cards.txt");
            return false;
        }

        _logger.LogInformation("Starting download of {Count} card(s) in high resolution...", requests.Count);

        var semaphore = new SemaphoreSlim(10);
        var tasks = requests.Select(r => DownloadCardAsync(r, semaphore));
        var results = await Task.WhenAll(tasks);

        var success = results.Count(r => r);
        _logger.LogInformation("Done! {Success}/{Total} successfully downloaded → {Folder}", success, requests.Count, _outputFolder);

        return success > 0;
    }

    private async Task<bool> DownloadCardAsync(CardRequest req, SemaphoreSlim semaphore)
    {
        await semaphore.WaitAsync();
        try
        {
            var json = await FetchCardJsonAsync(req);
            if (json == null)
                return false;

            //await SaveScryfallJsonAsync(json, req.Name);

            var card = await ParseCardAsync(json);
            if (card == null)
            {
                _logger.LogWarning("Failed to parse JSON for {Name}", req.Name);
                return false;
            }

            return await ProcessCardAsync(card);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Download failed: {Name}", req.Name);
            return false;
        }
        finally
        {
            semaphore.Release();
        }
    }

    private async Task<string?> FetchCardJsonAsync(CardRequest req)
    {
        string url = req.SetCode != null && req.CollectorNumber != null
            ? $"cards/{req.SetCode.ToLower()}/{req.CollectorNumber}"
            : $"cards/named?fuzzy={Uri.EscapeDataString(req.Name)}";

        var response = await _http.GetAsync(url);
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Not found: {Name} [{Set} {Cn}] → {Status}",
                req.Name, req.SetCode, req.CollectorNumber, response.StatusCode);
            return null;
        }

        return await response.Content.ReadAsStringAsync();
    }

    private async Task<ScryfallCard?> ParseCardAsync(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<ScryfallCard>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch
        {
            return null;
        }
    }

    private async Task<bool> ProcessCardAsync(ScryfallCard card)
    {
        if (card.ImageUris != null)
        {
            await DownloadSingleImageAsync(card.ImageUris, card.Name, card.Set, card.CollectorNumber);
            return true;
        }

        if (card.CardFaces != null && card.CardFaces.Count > 0)
        {
            int index = 0;
            foreach (var face in card.CardFaces)
            {
                if (face.ImageUris == null)
                {
                    _logger.LogWarning("Missing image_uris for face {FaceName}", face.Name);
                    continue;
                }

                string suffix = index == 0 ? "front" : "back";

                await DownloadSingleImageAsync(
                    face.ImageUris,
                    face.Name,
                    card.Set,
                    card.CollectorNumber,
                    suffix);

                index++;
            }

            return true;
        }

        _logger.LogWarning("No image_uris found for {Name}", card.Name);
        return false;
    }

    private async Task DownloadSingleImageAsync(
        ImageUris imageUris, string cardName, string setCode, string collectorNumber, string? faceSuffix = null)
    {
        string imageUrl = GetBestImageUrl(imageUris);

        string extension =
            imageUrl.EndsWith(".png", StringComparison.OrdinalIgnoreCase)
                ? ".png"
                : ".jpg";

        string safeName = SanitizeFileName(cardName);

        string fileName = faceSuffix == null
            ? $"{safeName}_{setCode.ToUpper()}_{collectorNumber}{extension}"
            : $"{safeName}_{setCode.ToUpper()}_{collectorNumber}_{faceSuffix}{extension}";

        string fullPath = Path.Combine(_outputFolder, fileName);

        if (File.Exists(fullPath))
            return;

        var bytes = await _http.GetByteArrayAsync(imageUrl);
        await File.WriteAllBytesAsync(fullPath, bytes);

        _logger.LogInformation("Downloaded → {FileName}", fileName);
    }


    // private async Task DownloadImageAsync(ImageUris imageUris, string cardName, string setCode, string collectorNumber, string? faceSuffix = null)
    // {
    //     string imageUrl = GetBestImageUrl(imageUris);
    //     string extension = imageUrl.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ? ".png" : ".jpg";

    //     string safeName = SanitizeFileName(cardName);

    //     string fileName = faceSuffix == null
    //         ? $"{safeName}_{setCode.ToUpper()}_{collectorNumber}{extension}"
    //         : $"{safeName}_{setCode.ToUpper()}_{collectorNumber}_{faceSuffix}{extension}";

    //     string fullPath = Path.Combine(_outputFolder, fileName);

    //     if (File.Exists(fullPath))
    //         return;

    //     var bytes = await _http.GetByteArrayAsync(imageUrl);
    //     await File.WriteAllBytesAsync(fullPath, bytes);

    //     _logger.LogInformation("Downloaded → {FileName}", fileName);
    // }

    private static string GetBestImageUrl(ImageUris u)
    {
        return u.Png
            ?? u.BorderCrop
            ?? u.ArtCrop
            ?? u.Large
            ?? u.Normal
            ?? u.Small
            ?? throw new Exception("No valid Scryfall image URL found.");
    }

    private static async Task SaveScryfallJsonAsync(string jsonString, string cardName)
    {
        var invalid = Path.GetInvalidFileNameChars();
        string safeFileName = string.Join("_", cardName.Split(invalid, StringSplitOptions.RemoveEmptyEntries)).Trim();

        if (safeFileName.Length > 80)
            safeFileName = safeFileName.Substring(0, 80);

        string fileName = $"scryfall_{safeFileName}_{DateTime.Now:yyyyMMdd_HHmmss}.json";
        string filePath = Path.Combine(AppConfig.LogPath, fileName);

        var prettyJson = JsonSerializer.Serialize(
            JsonSerializer.Deserialize<object>(jsonString),
            new JsonSerializerOptions { WriteIndented = true });

        await File.WriteAllTextAsync(filePath, prettyJson);
    }

    // private static bool TryParseLine(string line, out string name, out string? setCode, out string? collectorNumber)
    // {
    //     name = "";
    //     setCode = null;
    //     collectorNumber = null;

    //     if (string.IsNullOrWhiteSpace(line))
    //         return false;

    //     var match = Regex.Match(line, @"^(.+?)\s+\(\s*([A-Z0-9]{3,5})\s*\)\s*([0-9]+[a-zA-Z]*)", RegexOptions.IgnoreCase);

    //     if (match.Success)
    //     {
    //         name = match.Groups[1].Value.Trim();
    //         setCode = match.Groups[2].Value.Trim().ToUpper();
    //         collectorNumber = match.Groups[3].Value.Trim();
    //         return true;
    //     }

    //     name = line.Trim();
    //     return true;
    // }

    private static bool TryParseLine(string line, out string name, out string? setCode, out string? collectorNumber)
    {
        name = "";
        setCode = null;
        collectorNumber = null;

        if (string.IsNullOrWhiteSpace(line))
            return false;

        // verwijder *F* of andere foil-markeringen aan het einde
        line = Regex.Replace(line, @"\*F\*\s*$", "", RegexOptions.IgnoreCase).Trim();

        // Regex voor formaat: [aantal] Naam (SET) COLLECTOR
        var match = Regex.Match(line, @"^\s*(?:\d+\s+)?(.+?)\s*(?:\(\s*([A-Z0-9]{2,5})\s*\))?\s*([0-9A-Z\-]+)?\s*$", RegexOptions.IgnoreCase);

        if (match.Success)
        {
            name = match.Groups[1].Value.Trim();

            if (match.Groups[2].Success)
                setCode = match.Groups[2].Value.Trim().ToUpper();

            if (match.Groups[3].Success)
                collectorNumber = match.Groups[3].Value.Trim();

            return true;
        }

        // fallback: alleen naam
        name = line.Trim();
        return true;
    }


    private static string SanitizeFileName(string name) =>
        string.Join("_", name.Split(Path.GetInvalidFileNameChars()));

    record CardRequest(string Name, string? SetCode = null, string? CollectorNumber = null);

    record ImageUris
    (
        [property: JsonPropertyName("png")] string? Png,
        [property: JsonPropertyName("normal")] string? Normal,
        [property: JsonPropertyName("large")] string? Large,
        [property: JsonPropertyName("small")] string? Small = null,
        [property: JsonPropertyName("art_crop")] string? ArtCrop = null,
        [property: JsonPropertyName("border_crop")] string? BorderCrop = null
    );

    record ScryfallCard
    (
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("set")] string Set,
        [property: JsonPropertyName("collector_number")] string CollectorNumber,
        [property: JsonPropertyName("image_uris")] ImageUris? ImageUris,
        [property: JsonPropertyName("card_faces")] List<CardFace>? CardFaces
    );

    record CardFace
    (
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("image_uris")] ImageUris? ImageUris
    );
}