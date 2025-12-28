using ScryForge.Models;

namespace ScryForge.Services.Intefaces
{
    public interface IDownloaderService
    {
        Task<IReadOnlyList<ScryfallCard>> FetchScryfallCardsAsync(CancellationToken ct = default);
        Task DownloadImagesAsync(IReadOnlyList<ScryfallCard> cards, CancellationToken ct = default);
    }
}