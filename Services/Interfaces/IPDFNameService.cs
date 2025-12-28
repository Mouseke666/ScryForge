using ScryForge.Models;

namespace ScryForge.Services
{
    public interface IPDFNameService
    {
        Task<PdfNameResult> DeterminePdfNameAsync(string cardsFilePath);
    }
}