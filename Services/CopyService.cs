using ScryForge.Models;
using Microsoft.Extensions.Logging;
using ScryForge.Services.Interfaces;

namespace ScryForge.Services
{
    public class CopyService(ILogger<CopyService> logger) : ICopyService
    {
        private readonly ILogger<CopyService> _logger = logger;

        public void CopyFilesToRoot(string path)
        {
            if (!Directory.Exists(path))
            {
                _logger.LogWarning("Specified folder does not exist: {Path}", path);
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
                        _logger.LogError(ex, "Error copying from {Source} to {Destination}", file, destinationPath);
                    }
                }
            }
        }

        public void DuplicateCards(List<CardInfo> cards)
        {
            string folder = AppConfig.PDFImagesFolder;

            foreach (var card in cards)
            {
                var files = Directory.GetFiles(folder, $"{card.FrontFileName}");

                if (files.Length == 0)
                {
                    _logger.LogWarning("No source file found for card: {Card}", card.FrontFileName);
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
                        _logger.LogError(ex, "Error duplicating from {Source} to {Destination}", src, dest);
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
                    _logger.LogError(ex, "Error copying from {Source} to {Destination}", file, destPath);
                }
            }
        }

        public void MoveFile(string sourceFile, string destinationFile, bool overwrite = true)
        {
            if (!File.Exists(sourceFile))
            {
                _logger.LogWarning("Source file does not exist: {File}", sourceFile);
                return;
            }

            try
            {
                if (overwrite && File.Exists(destinationFile))
                {
                    File.Delete(destinationFile);
                }

                File.Copy(sourceFile, destinationFile, overwrite);
                File.Delete(sourceFile);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error moving from {Source} to {Destination}", sourceFile, destinationFile);
            }
        }

        public void CopyFile(string sourceFile, string destinationFile, bool overwrite = true)
        {
            if (!File.Exists(sourceFile))
            {
                _logger.LogWarning("Source file does not exist: {File}", sourceFile);
                return;
            }

            try
            {
                File.Copy(sourceFile, destinationFile, overwrite);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error copying from {Source} to {Destination}", sourceFile, destinationFile);
            }
        }
    }
}