using ScryForge.Models;
using Microsoft.Extensions.Logging;
using System.Text.RegularExpressions;
using ScryForge.Services.Intefaces;

namespace ScryForge.Services
{
    public class CardParserService : ICardParserService
    {
        private readonly ILogger<CardParserService> _logger;

        private static readonly Regex CardLineRegex = new(
            @"^\s*(\d+)\s+(.+?)\s+\(([A-Z0-9]+)\)\s+([^\s()]+)\s*$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public CardParserService(ILogger<CardParserService> logger)
        {
            _logger = logger;
        }

        public async Task<List<CardInfo>> ParseCardsAsync(string filePath)
        {
            var cards = new List<CardInfo>();

            if (!File.Exists(filePath))
            {
                _logger.LogWarning("File not found: {FilePath}", filePath);
                return cards;
            }

            var folder = AppConfig.UpscaledFolder;
            var lines = await File.ReadAllLinesAsync(filePath);

            // Remove foil markers + trim
            var cleanedLines = lines.Select(l => Regex.Replace(l, @"\*F\*\s*$", "", RegexOptions.IgnoreCase).Trim());

            var cardLineRegex = new Regex(
                @"^\s*(?:(\d+)\s+)?(.+?)\s*(?:\(\s*([A-Z0-9]{2,5})\s*\))?\s*([0-9A-Z\-]+)?\s*$",
                RegexOptions.IgnoreCase | RegexOptions.Compiled
            );

            foreach (var line in cleanedLines)
            {
                var match = cardLineRegex.Match(line);
                if (!match.Success)
                {
                    _logger.LogWarning("Line could not be parsed: {Line}", line);
                    continue;
                }

                var quantity = match.Groups[1].Success ? int.Parse(match.Groups[1].ValueSpan) : 1;
                var fullName = match.Groups[2].Value.Trim();
                var setCode = match.Groups[3].Success ? match.Groups[3].Value.Trim().ToUpper() : null;
                var number = match.Groups[4].Success ? match.Groups[4].Value.Trim() : null;

                if (setCode == null || number == null)
                {
                    _logger.LogWarning("Missing set code or number for: {Name}", fullName);
                    continue;
                }

                string[] names = fullName.Split(" / ", 2, StringSplitOptions.TrimEntries);

                var files = FindFiles(folder, setCode, number);

                if (files.Count == 2)
                {
                    var frontFile = files[0];
                    var backFile = files[1];

                    for (int i = 1; i <= quantity; i++)
                    {
                        var cardInfo = await CopyDoubleSidedAsync(frontFile, backFile, fullName, setCode, number, i);
                        cards.Add(cardInfo);
                    }

                    File.Delete(frontFile);
                    File.Delete(backFile);
                    continue;
                }
                else if (files.Count == 1)
                {
                    await AddCardCopiesAsync(cards, files[0], fullName, setCode, number, quantity);
                }
                else
                {
                    _logger.LogWarning("Card files not found: {Name} [{SetCode}] {Number}", fullName, setCode, number);
                }
            }

            return cards;
        }

        private async Task<CardInfo> CopyDoubleSidedAsync(
            string frontFile,
            string backFile,
            string fullName,
            string setCode,
            string number,
            int index)
        {
            string folder = Path.GetDirectoryName(frontFile)!;
            string ext = Path.GetExtension(frontFile);
            string baseName = Path.GetFileNameWithoutExtension(frontFile);

            string frontCopy = Path.Combine(folder, $"{baseName} - {index}{ext}");
            if (!File.Exists(frontCopy))
                await CopyFileAsync(frontFile, frontCopy);

            string backCopy = Path.Combine(folder, $"__back_{baseName} - {index}{ext}");
            if (!File.Exists(backCopy))
                await CopyFileAsync(backFile, backCopy);

            return new CardInfo
            {
                Quantity = 1,
                Name = fullName,
                SetCode = setCode,
                Number = number,
                FrontFileName = Path.GetFileName(frontCopy),
                BackFileName = Path.GetFileName(backCopy)
            };
        }

        private async Task AddCardCopiesAsync(
            List<CardInfo> cards,
            string baseFile,
            string fullName,
            string setCode,
            string number,
            int quantity,
            bool isBackSide = false)
        {
            if (baseFile == null) return;

            var folder = Path.GetDirectoryName(baseFile)!;
            var baseName = Path.GetFileNameWithoutExtension(baseFile);
            var ext = Path.GetExtension(baseFile);

            for (int i = 1; i <= quantity; i++)
            {
                string copyPath = Path.Combine(folder, $"{baseName} - {i}{ext}");

                if (!File.Exists(copyPath))
                    await CopyFileAsync(baseFile, copyPath, overwrite: false);

                cards.Add(new CardInfo
                {
                    Quantity = 1,
                    Name = fullName,
                    SetCode = setCode,
                    Number = number,
                    FrontFileName = isBackSide ? "" : Path.GetFileName(copyPath),
                    BackFileName = isBackSide ? Path.GetFileName(copyPath) : ""
                });
            }

            if (File.Exists(baseFile))
                File.Delete(baseFile);
        }

        private static async Task CopyFileAsync(string source, string destination, bool overwrite = false)
        {
            if (File.Exists(destination) && !overwrite)
                return;

            const int bufferSize = 81920;
            using var sourceStream = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, useAsync: true);
            using var destinationStream = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, useAsync: true);
            await sourceStream.CopyToAsync(destinationStream);
        }

        private static List<string> FindFiles(string folder, string setCode, string number)
        {
            if (!Directory.Exists(folder))
                return new List<string>();

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

            // Gebruik alleen de kaartnaam (zonder quantity / set / number)
            var match = Regex.Match(
                firstValidLine,
                @"^\s*(?:(\d+)\s+)?(.+?)(?:\s*\(|$)",
                RegexOptions.IgnoreCase);

            var name = match.Success
                ? match.Groups[2].Value.Trim()
                : firstValidLine;

            // Bestandsnaam veilig maken
            foreach (var c in Path.GetInvalidFileNameChars())
                name = name.Replace(c, '_');

            return name;
        }


    }
}