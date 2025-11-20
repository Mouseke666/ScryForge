using ScryForge.Models;
using Microsoft.Extensions.Logging;

namespace ScryForge.Services
{
    public class FlipService
    {
        private readonly ILogger<FlipService> _logger;

        public FlipService(ILogger<FlipService> logger)
        {
            _logger = logger;
        }

        public void ProcessFlipCards(List<CardInfo> cards)
        {
            Directory.CreateDirectory(AppConfig.FlipsFolder);

            var flipCards = cards.Where(c => c.IsFlip);

            foreach (var card in flipCards)
            {
                string frontSource = Path.Combine(AppConfig.UpscaledFolder, card.FrontFileName);
                string backSource = Path.Combine(AppConfig.UpscaledFolder, card.BackFileName);

                string frontDest = Path.Combine(AppConfig.FlipsFolder, card.FrontFileName);
                string backDest = Path.Combine(AppConfig.FlipsFolder, card.BackFileName);

                try
                {
                    File.Copy(frontSource, frontDest, true);
                    File.Copy(backSource, backDest, true);

                    File.Delete(frontSource);
                    File.Delete(backSource);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to copy flip card: {Name}", card.Name);
                }
            }
        }
    }
}
