using ScryForge.Models;
using System.Text.RegularExpressions;

namespace ScryForge.Services
{
    public class FlipService
    {
        public void ProcessFlipCards(List<CardInfo> cards)
        {
            Directory.CreateDirectory(AppConfig.FlipsFolder);

            var flipCards = cards
                .Where(c => c.Name.Contains("/"))
                .Select(c =>
                {
                    var parts = c.Name.Split('/');
                    return new
                    {
                        FrontName = parts[0].Trim(),
                        BackName = parts[1].Trim(),
                        c.SetCode,
                        c.Number,
                        c.Quantity
                    };
                });

            var upscaledFiles = Directory.GetFiles(AppConfig.UpscaledFolder)
                                         .Select(f => new FileInfo(f))
                                         .ToList();

            foreach (var flipCard in flipCards)
            {
                Console.WriteLine($"Verifying flip card: {flipCard.FrontName} / {flipCard.BackName} ({flipCard.SetCode}) {flipCard.Number}");

                string EscapePattern(string name) => Regex.Escape(name).Replace(@"\ ", ".*").Replace(@"\&", ".*");

                var frontPattern = new Regex(
                    $"^{EscapePattern(flipCard.FrontName)}(\\s*\\(.*\\))?\\s*\\[{flipCard.SetCode}\\]\\s*\\{{{flipCard.Number}\\}}",
                    RegexOptions.IgnoreCase);

                var backPattern = new Regex(
                    $"^{EscapePattern(flipCard.BackName)}(\\s*\\(.*\\))?\\s*\\[{flipCard.SetCode}\\]\\s*\\{{{flipCard.Number}\\}}",
                    RegexOptions.IgnoreCase);

                var frontFile = upscaledFiles.FirstOrDefault(f => frontPattern.IsMatch(f.Name));
                var backFile = upscaledFiles.FirstOrDefault(f => backPattern.IsMatch(f.Name));

                if (frontFile != null && backFile != null)
                {
                    for (int i = 0; i < flipCard.Quantity; i++)
                    {
                        // Nieuwe bestandsnamen
                        string frontCopyName = $"{flipCard.FrontName}{frontFile.Extension}";
                        string backCopyName = $"__back_{flipCard.FrontName}{backFile.Extension}";

                        string frontCopyPath = Path.Combine(AppConfig.FlipsFolder, frontCopyName);
                        string backCopyPath = Path.Combine(AppConfig.FlipsFolder, backCopyName);

                        try
                        {
                            File.Copy(frontFile.FullName, frontCopyPath, true);
                            File.Copy(backFile.FullName, backCopyPath, true);

                            Console.WriteLine($"Flip copy created: {frontCopyPath}");
                            Console.WriteLine($"Flip copy created: {backCopyPath}");

                            if (i == 0)
                            {
                                File.Delete(frontFile.FullName);
                                File.Delete(backFile.FullName);
                                Console.WriteLine($"Original deleted: {frontFile.FullName}");
                                Console.WriteLine($"Original deleted: {backFile.FullName}");
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Failed to create/delete flip copy: {ex.Message}");
                        }
                    }

                    // Vernieuw de lijst na verwijdering
                    upscaledFiles = Directory.GetFiles(AppConfig.UpscaledFolder)
                                             .Select(f => new FileInfo(f))
                                             .ToList();
                }
                else
                {
                    Console.WriteLine($"Not all sides found; treating as a normal card: {flipCard.FrontName} / {flipCard.BackName}");
                    if (frontFile == null) Console.WriteLine($"Front side missing: {flipCard.FrontName} [{flipCard.SetCode}] {{{flipCard.Number}}}");
                    if (backFile == null) Console.WriteLine($"Back side missing: {flipCard.BackName} [{flipCard.SetCode}] {{{flipCard.Number}}}");
                }
            }
        }
    }
}
