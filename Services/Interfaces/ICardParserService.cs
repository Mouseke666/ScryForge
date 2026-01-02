using ScryForge.Models;

namespace ScryForge.Services.Intefaces
{
    public interface ICardParserService
    {
        Task<List<CardInfo>> ParseCardsAsync(string filePath);
        Task<List<CardInfo>> ParseCustomCardsAsync(IReadOnlyList<CustomCard> customCards);

        Task<string> GetSuggestedPdfNameAsync(string filePath);
    }
}