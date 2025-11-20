using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

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

        // ← dit is de enige regel die je verandert
        _outputFolder = AppConfig.ScryfallSource;

        Directory.CreateDirectory(_outputFolder); // zorgt dat de map bestaat
    }

    public async Task<bool> DownloadArtAsync()
    {
        var cardsFile = Path.Combine(AppConfig.BasePath, "cards.txt");
        if (!File.Exists(cardsFile))
        {
            _logger.LogError("cards.txt niet gevonden: {Path}", cardsFile);
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
                _logger.LogWarning("Regel overgeslagen (niet herkend): {Line}", line);
            }
        }

        if (requests.Count == 0)
        {
            _logger.LogWarning("Geen kaartregels gevonden in cards.txt");
            return false;
        }

        _logger.LogInformation("Start downloaden van {Count} kaart(en) in hoge kwaliteit...", requests.Count);

        var semaphore = new SemaphoreSlim(10);
        var tasks = requests.Select(r => DownloadCardAsync(r, semaphore));
        var results = await Task.WhenAll(tasks);

        var success = results.Count(r => r);
        _logger.LogInformation("Klaar! {Success}/{Total} succesvol gedownload → {Folder}", success, requests.Count, _outputFolder);

        return success == requests.Count;
    }

    private async Task<bool> DownloadCardAsync(CardRequest req, SemaphoreSlim semaphore)
    {
        await semaphore.WaitAsync();
        try
        {
            string url = req.SetCode != null && req.CollectorNumber != null
                ? $"cards/{req.SetCode.ToLower()}/{req.CollectorNumber}"
                : $"cards/named?fuzzy={Uri.EscapeDataString(req.Name)}";

            var response = await _http.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Niet gevonden: {Name} [{Set} {Cn}] → {Status}",
                    req.Name, req.SetCode, req.CollectorNumber, response.StatusCode);
                return false;
            }

            var card = await response.Content.ReadFromJsonAsync<ScryfallCard>(new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (card?.ImageUris == null)
            {
                _logger.LogWarning("Geen image_uris voor: {Name}", req.Name);
                return false;
            }

            string imageUrl = card.ImageUris.Png ?? card.ImageUris.Normal ?? card.ImageUris.Large!;

            string extension = imageUrl.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ? ".png" : ".jpg";
            string safeName = SanitizeFileName(card.Name);
            string fileName = $"{safeName}_{card.Set.ToUpper()}_{card.CollectorNumber}{extension}";
            string fullPath = Path.Combine(_outputFolder, fileName);

            if (File.Exists(fullPath))
                return true;

            var data = await _http.GetByteArrayAsync(imageUrl);
            await File.WriteAllBytesAsync(fullPath, data);

            _logger.LogInformation("Gedownload → {FileName}", fileName);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Download mislukt: {Name}", req.Name);
            return false;
        }
        finally
        {
            semaphore.Release();
        }
    }

    private static bool TryParseLine(string line, out string name, out string? setCode, out string? collectorNumber)
    {
        name = ""; setCode = null; collectorNumber = null;

        // Formaat: "1 Cavern of Souls (LTC) 362" of "Paths of the Dead (LTC) 362"
        var match = System.Text.RegularExpressions.Regex.Match(line, @"^(?:\d+\s+)?(.+?)\s+\(([A-Z0-9]{3,5})\)\s+(\d+)", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        if (match.Success)
        {
            name = match.Groups[1].Value.Trim();
            setCode = match.Groups[2].Value.Trim();
            collectorNumber = match.Groups[3].Value.Trim();
            return true;
        }

        // Alleen naam (fallback fuzzy search)
        name = line.Trim();
        return true;
    }

    private static string SanitizeFileName(string name) =>
        string.Join("_", name.Split(Path.GetInvalidFileNameChars()));

    // CORRECTE MODELLEN – dit was het probleem!
    record CardRequest(string Name, string? SetCode = null, string? CollectorNumber = null);

    record ImageUris(
        [property: JsonPropertyName("png")] string? Png,
        [property: JsonPropertyName("normal")] string? Normal,
        [property: JsonPropertyName("large")] string? Large,
        [property: JsonPropertyName("small")] string? Small = null,
        [property: JsonPropertyName("art_crop")] string? ArtCrop = null,
        [property: JsonPropertyName("border_crop")] string? BorderCrop = null);

    record ScryfallCard(
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("set")] string Set,
        [property: JsonPropertyName("collector_number")] string CollectorNumber,
        [property: JsonPropertyName("image_uris")] ImageUris? ImageUris);
}