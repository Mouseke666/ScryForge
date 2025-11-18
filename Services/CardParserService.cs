using ScryForge.Models;
using System.Text.RegularExpressions;

namespace ScryForge.Services
{
    public class CardParserService
    {
        public List<CardInfo> ParseCards(string filePath)
        {
            var list = new List<CardInfo>();
            if (!File.Exists(filePath)) return list;

            var regex = new Regex(@"^\s*(\d+)\s+(.+?)\s+\(([A-Z0-9]+)\)\s+(\d+)\s*$");

            string folder = AppConfig.UpscaledFolder;

            foreach (var line in File.ReadAllLines(filePath))
            {
                var m = regex.Match(line);
                if (!m.Success) continue;

                int qty = int.Parse(m.Groups[1].Value);
                string fullName = m.Groups[2].Value.Trim();  // "Birgi ... / Harnfel ..."
                string setCode = m.Groups[3].Value.Trim();
                string number = m.Groups[4].Value.Trim();

                string[] names = fullName.Split(" / ");

                // Mogelijke flipcard
                if (names.Length > 1)
                {
                    string frontName = names[0];
                    string backName = names[1];

                    string frontPattern = $"{frontName}*[{setCode}]*{{{number}}}*.png";
                    string backPattern = $"{backName}*[{setCode}]*{{{number}}}*.png";

                    var frontMatches = Directory.GetFiles(folder, frontPattern);
                    var backMatches = Directory.GetFiles(folder, backPattern);

                    if (frontMatches.Length > 0 && backMatches.Length > 0)
                    {
                        // --- echte flipcard ---
                        list.Add(new CardInfo
                        {
                            Quantity = qty,
                            Name = fullName,
                            SetCode = setCode,
                            Number = number,
                            FrontFileName = Path.GetFileName(frontMatches[0]),
                            BackFileName = Path.GetFileName(backMatches[0])
                        });

                        continue; // flipcard afgehandeld
                    }
                }
                else
                // --- reguliere kaart ---
                {
                    string pattern = $"{fullName}*[{setCode}]*{{{number}}}*.png";
                    var matches = Directory.GetFiles(folder, pattern);

                    string fileName = matches.Length > 0 ? Path.GetFileName(matches[0]) : "";

                    list.Add(new CardInfo
                    {
                        Quantity = qty,
                        Name = fullName,
                        SetCode = setCode,
                        Number = number,
                        FrontFileName = fileName,
                        BackFileName = ""
                    });
                }
            }

            return list;
        }
    }
}
