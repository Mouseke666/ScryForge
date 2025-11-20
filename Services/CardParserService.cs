using ScryForge.Models;
using System.Text.RegularExpressions;

namespace ScryForge.Services
{
    public class CardParserService
    {
        private static readonly Regex CardLineRegex = new(
            @"^\s*(\d+)\s+(.+?)\s+\(([A-Z0-9]+)\)\s+([^\s()]+)\s*$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public async Task<List<CardInfo>> ParseCardsAsync(string filePath)
        {
            var cards = new List<CardInfo>();
            if (!File.Exists(filePath)) return cards;

            var folder = AppConfig.UpscaledFolder;
            var lines = await File.ReadAllLinesAsync(filePath);

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
                        for (int i = 1; i <= quantity; i++)
                        {
                            var cardInfo = await CopyDoubleSidedAsync(frontFile, backFile, fullName, setCode, number, i);
                            cards.Add(cardInfo);
                        }

                        File.Delete(frontFile);
                        File.Delete(backFile);

                        continue;
                    }
                }

                var baseFile = FindFile(folder, names[0], setCode, number);
                if (baseFile != null)
                    await AddCardCopiesAsync(cards, baseFile, fullName, setCode, number, quantity);
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

            const int bufferSize = 81920; // standaard buffer
            using var sourceStream = new FileStream(source, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize, useAsync: true);
            using var destinationStream = new FileStream(destination, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize, useAsync: true);
            await sourceStream.CopyToAsync(destinationStream);
        }

        private static string? FindFile(string folder, string name, string setCode, string number)
        {
            if (!Directory.Exists(folder)) return null;

            var files = Directory.GetFiles(folder, "*.png", SearchOption.TopDirectoryOnly);

            return files.FirstOrDefault(f =>
                Path.GetFileName(f).Contains(name, StringComparison.OrdinalIgnoreCase) &&
                Path.GetFileName(f).Contains(setCode, StringComparison.OrdinalIgnoreCase) &&
                Path.GetFileName(f).Contains(number, StringComparison.OrdinalIgnoreCase));
        }
    }
}