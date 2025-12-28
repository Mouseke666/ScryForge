using ScryForge.Models;

namespace ScryForge.Services.Intefaces
{
    public interface IPDFNameService
    {
        Task<PdfNameResult> DeterminePdfNameAsync(string cardsFilePath);
    }
}