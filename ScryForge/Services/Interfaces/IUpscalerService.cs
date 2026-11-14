using ScryForge.Models.Scryfall;

namespace ScryForge.Services.Interfaces
{
    public interface IUpscalerService
    {
        Task<bool> RunUpscalerForCardsAsync(IReadOnlyList<ScryfallCard> cards, string model, int scale);
        Task<bool> RunUpscalerForCardsAsync(IReadOnlyList<ScryfallCard> cards, string model, int scale, string outputFolder);
    }
}