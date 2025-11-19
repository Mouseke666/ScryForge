using ScryForge.Models;

namespace ScryForge.Services
{
    public class CopyService
    {
        public void CopyFilesToRoot(string path)
        {
            if (!Directory.Exists(path))
            {
                Console.WriteLine("De opgegeven map bestaat niet.");
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

                    File.Copy(file, destinationPath);
                    Console.WriteLine($"Gekopieerd: {file} -> {destinationPath}");
                }
            }

            Console.WriteLine("Alle bestanden uit subfolders zijn gekopieerd naar de rootfolder.");
        }

        public void DuplicateCards(List<CardInfo> cards)
        {
            string folder = AppConfig.UpscaledFolder;

            foreach (var card in cards)
            {
                var files = Directory.GetFiles(folder, $"{card.FrontFileName}");

                if (files.Length == 0)
                {
                    Console.WriteLine($"Geen bronbestand gevonden voor kaart: {card.FrontFileName}");
                    continue;
                }

                var src = files[0];

                for (int i = 2; i <= card.Quantity; i++)
                {
                    var dest = Path.Combine(folder, $"{Path.GetFileNameWithoutExtension(src)}_{i}.png");
                    File.Copy(src, dest, true);
                }
            }
        }

        public void CopyFolderFiles(string sourceFolder, string destinationFolder, bool overwrite = true)
        {
            if (!Directory.Exists(sourceFolder))
            {
                Console.WriteLine($"Source folder already exists: {sourceFolder}");
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
                    Console.WriteLine($"File copied: {fileName}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error while copying {fileName}: {ex.Message}");
                }
            }
        }

        public void MoveFile(string sourceFile, string destinationFolder, bool overwrite = true)
        {
            if (!File.Exists(sourceFile))
            {
                Console.WriteLine($"Sourcefile does not exist: {sourceFile}");
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
                Console.WriteLine($"File moved: {sourceFile} -> {destinationFolder}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while moving from {sourceFile} to {destinationFolder}: {ex.Message}");
            }
        }

        public void CopyFile(string sourceFile, string destinationFolder, bool overwrite = true)
        {
            if (!File.Exists(sourceFile))
            {
                Console.WriteLine($"Source file does not exist: {sourceFile}");
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
                    Console.WriteLine($"File copied: {sourceFile} -> {destPath}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error copying {sourceFile} to {destPath}: {ex.Message}");
                }
            }
        }
    }
}