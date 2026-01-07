using ScryForge.Models;

namespace ScryForge.Services.Interfaces
{
    public interface IPDFService
    {
        Task<int> GetMaxCardsPerPage(string jsonFilePath);
        Task<bool> GenerateMainPdfAsync(string baseName, IEnumerable<CardInfo> cards, bool showOutput = true);
        Task<bool> GenerateFlipsPdfAsync(string baseName, bool showOutput = true);
    }
}