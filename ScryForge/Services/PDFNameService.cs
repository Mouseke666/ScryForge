using ScryForge.Models;
using Microsoft.Extensions.Logging;
using ScryForge.Services.Interfaces;

namespace ScryForge.Services
{
    public class PDFNameService(ICardParserService parser, ILogger<PDFNameService> logger) : IPDFNameService
    {
        private readonly ICardParserService _parser = parser;
        private readonly ILogger<PDFNameService> _logger = logger;

        public async Task<PdfNameResult> DeterminePdfNameAsync(string cardsFilePath)
        {
            string suggestedName = await _parser.GetSuggestedPdfNameAsync(cardsFilePath);
            suggestedName = SanitizeFileName(suggestedName);

            if (string.IsNullOrWhiteSpace(suggestedName))
            {
                suggestedName = "UntitledDeck";
            }

            string timestamp = DateTime.Now.ToString("dd-MM-yyyy_HH-mm-ss");
            string fullName = $"{suggestedName}_{timestamp}";
            return new PdfNameResult(fullName, suggestedName, timestamp);
        }

        private static string SanitizeFileName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return string.Empty;

            var invalid = Path.GetInvalidFileNameChars();
            return string.Join("_", name.Split(invalid, StringSplitOptions.RemoveEmptyEntries)).Trim();
        }
    }
}