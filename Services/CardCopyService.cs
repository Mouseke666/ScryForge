using ScryForge.Models;
using Microsoft.Extensions.Logging;
using ScryForge.Services.Interfaces;

namespace ScryForge.Services
{
    public class CardCopyService : ICardCopyService
    {
        private readonly ILogger<CardCopyService> _logger;

        public CardCopyService(ILogger<CardCopyService> logger)
        {
            _logger = logger;
        }

        public void ProcessCards(List<CardInfo> cards)
        {
            Directory.CreateDirectory(AppConfig.FlipsFolder);
            Directory.CreateDirectory(AppConfig.UpscaledFolder); // voor single-sided copies

            // 🔹 Flip cards
            var flipCards = cards.Where(c => c.IsFlip && !string.Equals(c.SetCode, "CUSTOM", StringComparison.OrdinalIgnoreCase));
            foreach (var card in flipCards)
            {
                ProcessFlipCard(card);
            }

            // 🔹 Single-sided cards (Quantity > 1)
            var singleCards = cards.Where(c => !c.IsFlip && !string.Equals(c.SetCode, "CUSTOM", StringComparison.OrdinalIgnoreCase) && c.Quantity > 1);
            foreach (var card in singleCards)
            {
                ProcessSingleCard(card);
            }
        }

        private void ProcessFlipCard(CardInfo card)
        {
            string frontSource = Path.Combine(AppConfig.UpscaledFolder, card.FrontFileName);
            string backSource = Path.Combine(AppConfig.UpscaledFolder, card.BackFileName);

            if (!File.Exists(frontSource) || !File.Exists(backSource))
            {
                _logger.LogWarning("Flip card files not found for: {Name}", card.Name);
                return;
            }

            try
            {
                // Verwijder eventueel "_front" uit de basisnaam
                string baseFrontName = Path.GetFileNameWithoutExtension(card.FrontFileName);
                if (baseFrontName.EndsWith("_front", StringComparison.OrdinalIgnoreCase))
                    baseFrontName = baseFrontName[..^6]; // "_front" is 6 tekens

                string extension = Path.GetExtension(card.FrontFileName);

                for (int i = 1; i <= card.Quantity; i++)
                {
                    string frontDest = Path.Combine(
                        AppConfig.FlipsFolder,
                        card.Quantity > 1
                            ? $"{baseFrontName} - {i}{extension}"
                            : $"{baseFrontName}{extension}"
                    );

                    string backDest = Path.Combine(
                        AppConfig.FlipsFolder,
                        card.Quantity > 1
                            ? $"__back_{baseFrontName} - {i}{extension}"
                            : $"__back_{baseFrontName}{extension}"
                    );

                    File.Copy(frontSource, frontDest, true);
                    File.Copy(backSource, backDest, true);
                }

                File.Delete(frontSource);
                File.Delete(backSource);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process flip card: {Name}", card.Name);
            }
        }

        private void ProcessSingleCard(CardInfo card)
        {
            string sourceFile = Path.Combine(AppConfig.UpscaledFolder, card.FrontFileName);
            if (!File.Exists(sourceFile))
            {
                _logger.LogWarning("Single-sided card file not found for: {Name}", card.Name);
                return;
            }

            string baseName = Path.GetFileNameWithoutExtension(card.FrontFileName);
            string extension = Path.GetExtension(card.FrontFileName);

            try
            {
                for (int i = 1; i <= card.Quantity; i++)
                {
                    string dest = Path.Combine(
                        AppConfig.UpscaledFolder,
                        card.Quantity > 1
                            ? $"{baseName} - {i}{extension}"
                            : $"{baseName}{extension}"
                    );

                    File.Copy(sourceFile, dest, true);
                }

                // verwijder originele file als er kopieën zijn gemaakt
                if (card.Quantity > 1)
                    File.Delete(sourceFile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process single-sided card: {Name}", card.Name);
            }
        }
    }
}