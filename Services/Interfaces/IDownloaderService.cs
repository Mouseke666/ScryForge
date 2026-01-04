using ScryForge.Models.Scryfall;

namespace ScryForge.Services.Intefaces
{
    public interface IDownloaderService
    {
        Task<IReadOnlyList<ScryfallCard>> FetchCardsAsync(CancellationToken ct = default);
        Task DownloadImagesAsync(IReadOnlyList<ScryfallCard> cards, CancellationToken ct = default);
    }
}