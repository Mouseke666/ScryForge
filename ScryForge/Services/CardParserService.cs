using ScryForge.Models;
using Microsoft.Extensions.Logging;
using ScryForge.Services.Interfaces;
using System.Text.RegularExpressions;

namespace ScryForge.Services
{
    public class CardParserService(ILogger<CardParserService> logger) : ICardParserService
    {
        private readonly ILogger<CardParserService> _logger = logger;

        public async Task<List<CardInfo>> ParseCardsAsync(string filePath)
        {
            var result = new List<CardInfo>();

            if (!File.Exists(filePath))
            {
                _logger.LogWarning("File not found: {FilePath}", filePath);
                return result;
            }

            var folder = AppConfig.PDFImagesFolder;
            var lines = await File.ReadAllLinesAsync(filePath);

            var cardLineRegex = new Regex(
                @"^\s*(?:(\d+)\s+)?(.+?)\s*(?:\(\s*([A-Z0-9]{2,5})\s*\))?\s*([0-9A-Z\-]+)?\s*$",
                RegexOptions.IgnoreCase | RegexOptions.Compiled
            );

            var aggregated = new Dictionary<(string Name, string Set, string Number), int>();

            foreach (var rawLine in lines)
            {
                var line = Regex.Replace(rawLine, @"\*F\*\s*$", "", RegexOptions.IgnoreCase).Trim();
                if (string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                var match = cardLineRegex.Match(line);
                if (!match.Success)
                {
                    _logger.LogWarning("Line could not be parsed: {Line}", line);
                    continue;
                }

                var quantity = match.Groups[1].Success ? int.Parse(match.Groups[1].ValueSpan) : 1;
                var name = match.Groups[2].Value.Trim();
                var set = match.Groups[3].Success ? match.Groups[3].Value.Trim().ToUpperInvariant() : null;
                var number = match.Groups[4].Success ? match.Groups[4].Value.Trim() : null;

                if (set == null || number == null)
                {
                    _logger.LogWarning("Missing set code or number for: {Name}", name);
                    continue;
                }

                var key = (name, set, number);
                aggregated[key] = aggregated.TryGetValue(key, out var existing) ? existing + quantity : quantity;
            }

            foreach (var entry in aggregated)
            {
                var (fullName, setCode, number) = entry.Key;
                var quantity = entry.Value;

                List<string> files = [];

                foreach (var cn in GetCollectorNumberVariants(number))
                {
                    files = FindFiles(folder, setCode, cn);
                    if (files.Count > 0)
                    {
                        break;
                    }
                }

                if (files.Count == 2)
                {
                    var frontFile = files[0];
                    var backFile = files[1];

                    var card = new CardInfo
                    {
                        Quantity = quantity,
                        Name = fullName,
                        SetCode = setCode,
                        Number = number,
                        FrontFileName = Path.GetFileName(frontFile),
                        BackFileName = Path.GetFileName(backFile)
                    };

                    result.Add(card);
                }
                else if (files.Count == 1)
                {
                    await AddCardCopiesAsync(result, files[0], fullName, setCode, number, quantity);
                }
                else
                {
                    _logger.LogWarning(
                        "Card files not found: {Name} [{SetCode}] {Number}",
                        fullName,
                        setCode,
                        number
                    );
                }
            }

            return result;
        }

        private static IEnumerable<string> GetCollectorNumberVariants(string number)
        {
            yield return number;
            yield return number.ToUpperInvariant();
            if (number.EndsWith("p", StringComparison.OrdinalIgnoreCase))
                yield return number[..^1];
        }

        private static async Task AddCardCopiesAsync(
            List<CardInfo> cards,
            string baseFile,
            string fullName,
            string setCode,
            string number,
            int quantity,
            bool isBackSide = false)
        {
            if (string.IsNullOrWhiteSpace(baseFile))
                return;

            cards.Add(new CardInfo
            {
                Quantity = quantity,
                Name = fullName,
                SetCode = setCode,
                Number = number,
                FrontFileName = isBackSide ? "" : Path.GetFileName(baseFile),
                BackFileName = isBackSide ? Path.GetFileName(baseFile) : ""
            });
            await Task.CompletedTask;
        }

        private static List<string> FindFiles(string folder, string setCode, string number)
        {
            if (!Directory.Exists(folder))
                return [];

            var pattern = $@"(?<![A-Za-z0-9]){Regex.Escape(setCode)}[_-]{Regex.Escape(number)}(?![A-Za-z0-9])";

            return Directory.GetFiles(folder, "*.png", SearchOption.TopDirectoryOnly)
                .Where(f => Regex.IsMatch(Path.GetFileName(f), pattern, RegexOptions.IgnoreCase))
                .ToList();
        }

        public async Task<string> GetSuggestedPdfNameAsync(string filePath)
        {
            if (!File.Exists(filePath))
            {
                _logger.LogWarning("cards.txt not found for PDF name suggestion.");
                return "cards";
            }

            var lines = await File.ReadAllLinesAsync(filePath);

            var firstValidLine = lines
                .Select(l => Regex.Replace(l, @"\*F\*\s*$", "", RegexOptions.IgnoreCase).Trim())
                .FirstOrDefault(l => !string.IsNullOrWhiteSpace(l));

            if (string.IsNullOrWhiteSpace(firstValidLine))
                return "cards";

            var match = Regex.Match(
                firstValidLine,
                @"^\s*(?:(\d+)\s+)?(.+?)(?:\s*\(|$)",
                RegexOptions.IgnoreCase);

            var name = match.Success ? match.Groups[2].Value.Trim() : firstValidLine;

            foreach (var c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');

            return name;
        }

        public async Task<List<CardInfo>> ParseCustomCardsAsync(IReadOnlyList<CustomCard> customCards)
        {
            var cards = new List<CardInfo>();
            if (customCards == null || customCards.Count == 0)
            {
                return cards;
            }

            foreach (var custom in customCards)
            {
                if (string.IsNullOrWhiteSpace(custom.FrontLocation) || !File.Exists(custom.FrontLocation))
                {
                    _logger.LogWarning("Custom card file not found or empty: {File}", custom?.FrontLocation);
                    continue;
                }

                var frontFileName = Path.GetFileName(custom.FrontLocation);
                var backFileName = !string.IsNullOrWhiteSpace(custom.BackLocation) && File.Exists(custom.BackLocation)
                    ? Path.GetFileName(custom.BackLocation)
                    : string.Empty;

                var cardInfo = new CardInfo
                {
                    Quantity = 1,
                    Name = Path.GetFileNameWithoutExtension(frontFileName),
                    SetCode = "CUSTOM",
                    Number = "0",
                    FrontFileName = frontFileName,
                    BackFileName = backFileName
                };

                cards.Add(cardInfo);
            }

            _logger.LogInformation("Converted {Count} custom cards into CardInfo format.", cards.Count);
            return cards;
        }
    }
}