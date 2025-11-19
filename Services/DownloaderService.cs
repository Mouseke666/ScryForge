using System.Diagnostics;
using System.ComponentModel;

namespace ScryForge.Services
{
    public class DownloaderService
    {
        public async Task<bool> DownloadArtAsync()
        {
            var exe = AppConfig.ArtDownloaderExe;
            if (!File.Exists(exe))
            {
                Console.WriteLine("Downloader exe missing: " + exe);
                return false;
            }

            var psi = new ProcessStartInfo
            {
                FileName = exe,
                WorkingDirectory = AppConfig.ArtDownloaderPath,
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
            };

            try
            {
                using var process = Process.Start(psi);
                if (process == null)
                {
                    Console.WriteLine("Could not start the downloader process.");
                    return false;
                }

                await process.WaitForExitAsync();

                return process.ExitCode == 0;
            }
            catch (Win32Exception ex) when (ex.NativeErrorCode == 1223)
            {
                Console.WriteLine("Download cancelled by user (UAC/SmartScreen).");
                Console.WriteLine("To fix: Right-click MTG Art Downloader.exe → Properties → Unblock");

                if (Console.IsInputRedirected == false)
                {
                    Console.WriteLine("Press any key to continue...");
                    Console.ReadKey(true);
                }

                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error starting downloader: {ex.Message}");
                return false;
            }
        }
    }
}