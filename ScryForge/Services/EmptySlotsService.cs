using ScryForge.Models;
using ScryForge.Models.Scryfall;
using Microsoft.Extensions.Logging;
using ScryForge.Services.Interfaces;

namespace ScryForge.Services
{
    public class EmptySlotsService(IPDFService pdf, ILogger<EmptySlotsService> logger) : IEmptySlotsService
    {
        private readonly IPDFService _pdf = pdf;
        private readonly ILogger<EmptySlotsService> _logger = logger;

        public async Task<EmptySlotsResult> AnalyzeAsync(
            IReadOnlyList<ScryfallCard> cards,
            IReadOnlyList<CustomCard> customCards,
            CancellationToken ct)
        {
            if ((cards == null || !cards.Any()) && (customCards == null || !customCards.Any()))
            {
                return new EmptySlotsResult(
                    HasEmptySlots: false,
                    EmptySlotsDefault: 0,
                    EmptySlotsFlips: 0
                );
            }

            string defaultConfigPath = Path.Combine(AppConfig.PdfPath, "default.json");
            string flipsConfigPath = Path.Combine(AppConfig.PdfPath, "flips.json");

            int maxDefault = await _pdf.GetMaxCardsPerPage(defaultConfigPath);
            int maxFlips = await _pdf.GetMaxCardsPerPage(flipsConfigPath);

            if (maxDefault <= 0) maxDefault = 9;
            if (maxFlips <= 0) maxFlips = 8;

            int defaultCount = cards?.Where(c => !c.IsDoubleFaced).Sum(c => c.Quantity) ?? 0;
            int flipsCount = cards?.Where(c => c.IsDoubleFaced).Sum(c => c.Quantity) ?? 0;

            int customCount = customCards?.Count ?? 0;
            defaultCount += customCount;

            int emptyDefault = CalculateEmptySlots(defaultCount, maxDefault);
            int emptyFlips = CalculateEmptySlots(flipsCount, maxFlips);

            bool hasEmptySlots = emptyDefault > 0 || emptyFlips > 0;
            return new EmptySlotsResult(hasEmptySlots, emptyDefault, emptyFlips);
        }

        private static int CalculateEmptySlots(int cardCount, int slotsPerPage)
        {
            if (slotsPerPage <= 0)
                return 0;

            int remaining = cardCount % slotsPerPage;
            return remaining > 0 ? slotsPerPage - remaining : 0;
        }
    }
}