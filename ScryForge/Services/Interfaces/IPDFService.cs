using ScryForge.Models;

namespace ScryForge.Services.Interfaces
{
    public interface IPDFService
    {
        Task RunAsync(string project, string pdfFileName, bool showOutput = true);
        Task<int> GetMaxCardsPerPage(string jsonFilePath);
        Task GenerateMainPdfAsync(string baseName, IEnumerable<CardInfo> cards);
        Task GenerateFlipsPdfAsync(string baseName);
    }
}