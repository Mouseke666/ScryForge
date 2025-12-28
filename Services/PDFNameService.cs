using ScryForge.Models;
using Microsoft.Extensions.Logging;

namespace ScryForge.Services
{
    public class PDFNameService : IPDFNameService
    {
        private readonly ICardParserService _parser;
        private readonly ILogger<PDFNameService> _logger;

        public PDFNameService(ICardParserService parser, ILogger<PDFNameService> logger)
        {
            _parser = parser;
            _logger = logger;
        }

        public async Task<PdfNameResult> DeterminePdfNameAsync(string cardsFilePath)
        {
            string suggestedName = await _parser.GetSuggestedPdfNameAsync(cardsFilePath);

            // Sanitize suggestie (verwijder ongeldige filename karakters)
            suggestedName = SanitizeFileName(suggestedName);

            if (string.IsNullOrWhiteSpace(suggestedName))
            {
                suggestedName = "UntitledDeck";
                _logger.LogWarning("No suggested name found or invalid. Using fallback: {Fallback}", suggestedName);
            }

            string timestamp = DateTime.Now.ToString("dd-MM-yyyy_HH-mm-ss");

            _logger.LogInformation("Suggested PDF name: {Name}", suggestedName);
            _logger.LogInformation("Enter PDF name (press Enter to accept suggested name):");

            Console.Write("> ");
            string? input = Console.ReadLine();

            string finalBaseName = string.IsNullOrWhiteSpace(input)
                ? suggestedName
                : input.Trim();

            finalBaseName = SanitizeFileName(finalBaseName);

            if (string.IsNullOrWhiteSpace(finalBaseName))
            {
                _logger.LogWarning("User input was empty or invalid. Falling back to suggested name.");
                finalBaseName = suggestedName;
            }

            string fullName = $"{finalBaseName}_{timestamp}";

            _logger.LogInformation("Using PDF base name: {Name}", fullName);

            return new PdfNameResult(fullName, finalBaseName, timestamp);
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