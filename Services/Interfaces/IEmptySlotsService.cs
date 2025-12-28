using ScryForge.Models;

namespace ScryForge.Services;

public interface IEmptySlotsService
{
    Task<EmptySlotsResult> AnalyzeAsync(IReadOnlyList<ScryfallCard> cards, CancellationToken ct);
}