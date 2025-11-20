using Microsoft.Extensions.Logging;
using ScryForge.Models;

namespace ScryForge.Services
{
    public class CopyService
    {
        private readonly ILogger<CopyService> _logger;

        public CopyService(ILogger<CopyService> logger)
        {
            _logger = logger;
        }

        public void CopyFilesToRoot(string path)
        {
            if (!Directory.Exists(path))
            {
                _logger.LogWarning("De opgegeven map bestaat niet: {Path}", path);
                return;
            }

            string[] subDirs = Directory.GetDirectories(path);

            foreach (string subDir in subDirs)
            {
                string[] files = Directory.GetFiles(subDir, "*.*", SearchOption.AllDirectories);

                foreach (string file in files)
                {
                    string fileName = Path.GetFileName(file);
                    string destinationPath = Path.Combine(path, fileName);

                    int counter = 1;
                    while (File.Exists(destinationPath))
                    {
                        string fileNameWithoutExt = Path.GetFileNameWithoutExtension(file);
                        string extension = Path.GetExtension(file);
                        destinationPath = Path.Combine(path, $"{fileNameWithoutExt}_{counter}{extension}");
                        counter++;
                    }

                    try
                    {
                        File.Copy(file, destinationPath);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Fout bij kopiëren van {Source} naar {Destination}", file, destinationPath);
                    }
                }
            }
        }

        public void DuplicateCards(List<CardInfo> cards)
        {
            string folder = AppConfig.UpscaledFolder;

            foreach (var card in cards)
            {
                var files = Directory.GetFiles(folder, $"{card.FrontFileName}");

                if (files.Length == 0)
                {
                    _logger.LogWarning("Geen bronbestand gevonden voor kaart: {Card}", card.FrontFileName);
                    continue;
                }

                var src = files[0];

                for (int i = 2; i <= card.Quantity; i++)
                {
                    var dest = Path.Combine(folder, $"{Path.GetFileNameWithoutExtension(src)}_{i}.png");
                    try
                    {
                        File.Copy(src, dest, true);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Fout bij dupliceren van {Source} naar {Destination}", src, dest);
                    }
                }
            }
        }

        public void CopyFolderFiles(string sourceFolder, string destinationFolder, bool overwrite = true)
        {
            if (!Directory.Exists(sourceFolder))
            {
                _logger.LogWarning("Source folder does not exist: {Folder}", sourceFolder);
                return;
            }

            Directory.CreateDirectory(destinationFolder);

            foreach (var file in Directory.GetFiles(sourceFolder))
            {
                var fileName = Path.GetFileName(file);
                var destPath = Path.Combine(destinationFolder, fileName);

                try
                {
                    File.Copy(file, destPath, overwrite);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error while copying {Source} to {Destination}", file, destPath);
                }
            }
        }

        public void MoveFile(string sourceFile, string destinationFolder, bool overwrite = true)
        {
            if (!File.Exists(sourceFile))
            {
                _logger.LogWarning("Source file does not exist: {File}", sourceFile);
                return;
            }

            try
            {
                if (overwrite && File.Exists(destinationFolder))
                {
                    File.Delete(destinationFolder);
                }

                File.Copy(sourceFile, destinationFolder, overwrite);
                File.Delete(sourceFile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while moving from {Source} to {Destination}", sourceFile, destinationFolder);
            }
        }

        public void CopyFile(string sourceFile, string destinationFolder, bool overwrite = true)
        {
            if (!File.Exists(sourceFile))
            {
                _logger.LogWarning("Source file does not exist: {File}", sourceFile);
                return;
            }

            var path1 = Path.GetDirectoryName(destinationFolder);
            var path2 = Path.GetFileName(sourceFile);

            if (!string.IsNullOrEmpty(path1) && !string.IsNullOrEmpty(path2))
            {
                var destPath = Path.Combine(path1, path2);
                try
                {
                    File.Copy(sourceFile, destPath, overwrite);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error copying {Source} to {Destination}", sourceFile, destPath);
                }
            }
        }
    }
}