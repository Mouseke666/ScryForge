using ScryForge.Models;
using Microsoft.Extensions.Logging;
using ScryForge.Services.Interfaces;

namespace ScryForge.Services
{
    public class CardCopyService(ILogger<CardCopyService> logger) : ICardCopyService
    {
        private readonly ILogger<CardCopyService> _logger = logger;

        public ProcessCardsResult ProcessCards(List<CardInfo> cards)
        {
            int flipCount = 0;
            int singleCount = 0;
            //int customCount = 0;
            int totalCards = 0;

            Directory.CreateDirectory(AppConfig.FlipsFolder);
            Directory.CreateDirectory(AppConfig.PDFImagesFolder);

            foreach (var card in cards)
            {
                totalCards += card.Quantity;

                // if (string.Equals(card.SetCode, "CUSTOM", StringComparison.OrdinalIgnoreCase))
                // {
                //     customCount += card.Quantity;
                //     continue;
                // }

                if (card.IsFlip)
                {
                    ProcessFlipCard(card);
                    flipCount += card.Quantity;
                }
                else if (card.Quantity > 1)
                {
                    ProcessSingleCard(card);
                    singleCount += card.Quantity;
                }
            }

            return new ProcessCardsResult(
                TotalCards: totalCards,
                FlipCardsProcessed: flipCount,
                SingleCardsProcessed: singleCount
            );
        }


        private void ProcessFlipCard(CardInfo card)
        {
            string frontSource = Path.Combine(AppConfig.PDFImagesFolder, card.FrontFileName);
            string backSource = Path.Combine(AppConfig.PDFImagesFolder, card.BackFileName);

            if (!File.Exists(frontSource) || !File.Exists(backSource))
            {
                _logger.LogWarning("Flip card files not found for: {Name}", card.Name);
                return;
            }

            try
            {
                string baseFrontName = Path.GetFileNameWithoutExtension(card.FrontFileName);
                if (baseFrontName.EndsWith("_front", StringComparison.OrdinalIgnoreCase))
                {
                    baseFrontName = baseFrontName[..^6];
                }

                string extension = Path.GetExtension(card.FrontFileName);

                for (int i = 1; i <= card.Quantity; i++)
                {
                    string frontDest = Path.Combine(AppConfig.FlipsFolder,
                        card.Quantity > 1
                            ? $"{baseFrontName} - {i}{extension}"
                            : $"{baseFrontName}{extension}"
                    );

                    string backDest = Path.Combine(AppConfig.FlipsFolder,
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
            string sourceFile = Path.Combine(AppConfig.PDFImagesFolder, card.FrontFileName);
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
                        AppConfig.PDFImagesFolder,
                        card.Quantity > 1
                            ? $"{baseName} - {i}{extension}"
                            : $"{baseName}{extension}"
                    );

                    File.Copy(sourceFile, dest, true);
                }

                if (card.Quantity > 1)
                {
                    File.Delete(sourceFile);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process single-sided card: {Name}", card.Name);
            }
        }
    }
}