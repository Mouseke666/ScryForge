using ScryForge.Models;

namespace ScryForge.Services;

public interface IDownloaderService
{
    Task<IReadOnlyList<ScryfallCard>> FetchScryfallCardsAsync();
    Task DownloadImagesAsync(IEnumerable<ScryfallCard> cards);
}