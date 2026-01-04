using ScryForge.Models;
using ScryForge.Models.Scryfall;

namespace ScryForge.Services.Interfaces
{
    public interface IEmptySlotsService
    {
        Task<EmptySlotsResult> AnalyzeAsync(IReadOnlyList<ScryfallCard> cards, IReadOnlyList<CustomCard> customCards, CancellationToken ct);
    }
}