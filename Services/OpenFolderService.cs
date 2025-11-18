using System.Diagnostics;

namespace ScryForge.Services
{
    public class OpenFolderService
    {
        public void OpenFolder(string? path = null)
        {
            var folder = string.IsNullOrWhiteSpace(path)
                ? AppConfig.BasePath
                : path;

            if (Directory.Exists(folder))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = folder,
                    UseShellExecute = true
                });
            }
            else
            {
                Console.WriteLine($"Map bestaat niet: {folder}");
            }
        }
    }
