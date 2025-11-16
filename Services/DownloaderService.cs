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
        public async Task<bool> DownloadArtAsync()
        {
            var exe = AppConfig.ArtDownloaderExe;
            if (!File.Exists(exe))
            {
                Console.WriteLine("Downloader exe missing: " + exe);
                return false; // teruggeven dat het niet gelukt is
            }

            var psi = new ProcessStartInfo
            {
                FileName = exe,
                WorkingDirectory = AppConfig.ArtDownloaderPath,
                UseShellExecute = true,
            };

            try
            {
                using var process = Process.Start(psi);
                if (process == null)
                {
                    Console.WriteLine("Could not start the process.");
                    return false;
                }

                await process.WaitForExitAsync();
            }
            catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
            {
                Console.WriteLine("The external application was blocked by Windows SmartScreen or cancelled.");
                Console.WriteLine("To allow it to run:");
                Console.WriteLine("1. Open File Explorer and navigate to: " + AppConfig.ArtDownloaderPath);
                Console.WriteLine("2. Right-click 'MTG Art Downloader.exe' → Properties → check 'Unblock' → Apply → OK");
                Console.WriteLine("The application will now stop.");
                return false; // geef false terug zodat Program kan stoppen
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error starting external process: " + ex.Message);
                return false;
            }

            return true; // proces is succesvol uitgevoerd
        }
    }
}
