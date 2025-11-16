using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.ComponentModel;

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

            try
            {
                using var process = Process.Start(psi);
                if (process == null)
                {
                    Console.WriteLine("Could not start the process.");
                    return;
                }

                // Wacht async tot het proces klaar is
                await process.WaitForExitAsync();
            }
            catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
            {
                // 1223 = The operation was canceled by the user (UAC/SmartScreen)
                Console.WriteLine("The external application was blocked by Windows SmartScreen or cancelled.");
                Console.WriteLine("To allow it to run:");
                Console.WriteLine("1. Open File Explorer and navigate to the folder: " + AppConfig.ArtDownloaderPath);
                Console.WriteLine("2. Right-click 'MTG Art Downloader.exe' and select 'Properties'.");
                Console.WriteLine("3. At the bottom of the 'General' tab, check 'Unblock' if present, then click 'Apply' and 'OK'.");
                Console.WriteLine("4. Run ScryForge again to start the downloader.");
                return;
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error starting external process: " + ex.Message);
                return;
            }

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
