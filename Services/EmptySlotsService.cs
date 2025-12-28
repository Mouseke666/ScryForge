using ScryForge.Models;
using Microsoft.Extensions.Logging;

namespace ScryForge.Services
{
    public class EmptySlotsService : IEmptySlotsService
    {
        private readonly IPDFService _pdf;
        private readonly ILogger<EmptySlotsService> _logger;

        public EmptySlotsService(IPDFService pdf, ILogger<EmptySlotsService> logger)
        {
            _pdf = pdf;
            _logger = logger;
        }

        public async Task<EmptySlotsResult> AnalyzeAsync(IReadOnlyList<ScryfallCard> cards, CancellationToken ct)
        {
            if (cards == null || !cards.Any())
            {
                return new EmptySlotsResult(false, 0, 0, false);
            }

            string defaultConfigPath = Path.Combine(AppConfig.PdfPath, "default.json");
            string flipsConfigPath = Path.Combine(AppConfig.PdfPath, "flips.json");

            int maxDefault = await _pdf.GetMaxCardsPerPage(defaultConfigPath);
            int maxFlips = await _pdf.GetMaxCardsPerPage(flipsConfigPath);

            if (maxDefault <= 0) maxDefault = 9;
            if (maxFlips <= 0) maxFlips = 8;

            int defaultCount = cards.Count(c => !c.IsDoubleFaced);
            int flipsCount = cards.Count(c => c.IsDoubleFaced);

            int emptyDefault = CalculateEmptySlots(defaultCount, maxDefault);
            int emptyFlips = CalculateEmptySlots(flipsCount, maxFlips);

            bool hasEmptySlots = emptyDefault > 0 || emptyFlips > 0;

            if (!hasEmptySlots)
            {
                return new EmptySlotsResult(false, 0, 0, false);
            }

            if (emptyDefault > 0)
            {
                _logger.LogInformation("There are {EmptySlots} empty slot(s) on the last page of default cards.", emptyDefault);
            }

            if (emptyFlips > 0)
            {
                _logger.LogInformation("There are {EmptySlots} empty slot(s) on the last page of double-faced cards.", emptyFlips);
            }

            _logger.LogInformation("Do you want to fill these empty slots? Press Enter to continue, or type 'Q' to quit.");

            Console.Write("> ");
            string? input = Console.ReadLine();

            bool shouldStop = input?.Trim().Equals("Q", StringComparison.OrdinalIgnoreCase) == true;

            if (shouldStop)
            {
                _logger.LogInformation("User chose to quit due to empty slots.");
            }

            return new EmptySlotsResult(shouldStop, emptyDefault, emptyFlips, true);
        }

        private static int CalculateEmptySlots(int cardCount, int slotsPerPage)
        {
            if (slotsPerPage <= 0)
            {
                return 0;
            }

            int remaining = cardCount % slotsPerPage;
            return remaining > 0 ? slotsPerPage - remaining : 0;
        }

    }
}