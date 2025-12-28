using ScryForge.Models;

namespace ScryForge.Services.Intefaces
{
    public interface ICardParserService
    {
        Task<List<CardInfo>> ParseCardsAsync(string filePath);

        Task<string> GetSuggestedPdfNameAsync(string filePath);
    }
}