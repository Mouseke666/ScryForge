using ScryForge.Models;

namespace ScryForge.Services.Interfaces
{
    public interface IPDFNameService
    {
        Task<PdfNameResult> DeterminePdfNameAsync(string cardsFilePath);
    }
}