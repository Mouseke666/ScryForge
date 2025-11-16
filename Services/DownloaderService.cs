using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MTGArtDownloader.Services
{
    public class DownloaderService
    {
        public async Task DownloadArtAsync()
        {
            var exe = AppConfig.ArtDownloaderExe;
            if (!File.Exists(exe))
            {
                Console.WriteLine("Downloader exe missing: " + exe);
                return;
            }

            var psi = new ProcessStartInfo
            {
                FileName = exe,
                WorkingDirectory = AppConfig.ArtDownloaderPath,
                UseShellExecute = true, // belangrijk voor GUI apps
            };

            using var process = Process.Start(psi);
            if (process == null)
            {
                Console.WriteLine("Could not start the process.");
                return;
            }

            // Wacht async tot het proces klaar is
            await process.WaitForExitAsync();

            // --- Post-processing: afbeeldingen verplaatsen ---
            var imageExtensions = new[] { ".png", ".jpg", ".webp" };
            var allFiles = Directory.GetFiles(AppConfig.ScryfallSource, "*.*", SearchOption.AllDirectories)
                                    .Where(f => imageExtensions.Contains(Path.GetExtension(f).ToLower()));

            foreach (var file in allFiles)
            {
                var parentDir = Path.GetDirectoryName(file);
                if (!string.Equals(parentDir, AppConfig.ScryfallSource, StringComparison.OrdinalIgnoreCase))
                {
                    var dest = Path.Combine(AppConfig.ScryfallSource, Path.GetFileName(file));
                    try
                    {
                        // Forceer overschrijven
                        if (File.Exists(dest)) File.Delete(dest);
                        File.Move(file, dest);
                        Console.WriteLine($"Moved: {file} -> {dest}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error moving {file} to {dest}: {ex.Message}");
                    }
                }
            }
        }
    }
}
