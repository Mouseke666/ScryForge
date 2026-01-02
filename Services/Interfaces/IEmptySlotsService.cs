using ScryForge.Models;

namespace ScryForge.Services.Intefaces
{
    public interface IEmptySlotsService
    {
        Task<EmptySlotsResult> AnalyzeAsync(IReadOnlyList<ScryfallCard> cards, IReadOnlyList<CustomCard> customCards, CancellationToken ct);
    }
}