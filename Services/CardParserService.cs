using ScryForge.Models;
using System.Text.RegularExpressions;

namespace ScryForge.Services
{
    public class CardParserService
    {
        private static readonly Regex CardLineRegex = new(
            @"^\s*(\d+)\s+(.+?)\s+\(([A-Z0-9]+)\)\s+(\d+)\s*$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public List<CardInfo> ParseCards(string filePath)
        {
            var cards = new List<CardInfo>();
            if (!File.Exists(filePath)) return cards;

            var folder = AppConfig.UpscaledFolder;
            var lines = File.ReadAllLines(filePath);

            foreach (var line in lines)
            {
                var match = CardLineRegex.Match(line);
                if (!match.Success) continue;

                var quantity = int.Parse(match.Groups[1].ValueSpan);
                var fullName = match.Groups[2].Value.Trim();
                var setCode = match.Groups[3].Value.Trim();
                var number = match.Groups[4].Value.Trim();

                var names = fullName.Split(" / ", 2, StringSplitOptions.TrimEntries);

                if (names.Length == 2)
                {
                    var frontFile = FindFile(folder, names[0], setCode, number);
                    var backFile = FindFile(folder, names[1], setCode, number);

                    if (frontFile != null && backFile != null)
                    {
                        cards.Add(new CardInfo
                        {
                            Quantity = quantity,
                            Name = fullName,
                            SetCode = setCode,
                            Number = number,
                            FrontFileName = Path.GetFileName(frontFile),
                            BackFileName = Path.GetFileName(backFile)
                        });
                        continue;
                    }
                }

                var singleFile = FindFile(folder, names[0], setCode, number);
                cards.Add(new CardInfo
                {
                    Quantity = quantity,
                    Name = fullName,
                    SetCode = setCode,
                    Number = number,
                    FrontFileName = singleFile != null ? Path.GetFileName(singleFile) : "",
                    BackFileName = ""
                });
            }

            return cards;
        }

        private static string? FindFile(string folder, string name, string setCode, string number)
        {
            var pattern = $"{name}*[{setCode}]*{{{number}}}*.png";
            var files = Directory.GetFiles(folder, pattern, SearchOption.TopDirectoryOnly);
            return files.FirstOrDefault();
        }
    }
}