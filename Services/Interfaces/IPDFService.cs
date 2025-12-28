namespace ScryForge.Services.Intefaces
{
    public interface IPDFService
    {
        Task RunAsync(string project, string pdfFileName, bool showOutput = true);
        Task<int> GetMaxCardsPerPage(string jsonFilePath);
    }
}