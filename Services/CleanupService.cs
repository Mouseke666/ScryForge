namespace ScryForge.Services
{
    public class CleanupService
    {
        /// <summary>
        /// Verwijdert alle bestanden en subfolders in de opgegeven map, behalve de opgegeven subfolder en de bestanden daarin.
        /// </summary>
        /// <param name="path">Root folder om op te ruimen</param>
        /// <param name="excludeSubfolder">Naam van subfolder die niet verwijderd mag worden (optioneel)</param>
        public void CleanDirectory(string path, string? excludeSubfolder = null)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                return;
            }

            // --- Verwijder bestanden in root folder ---
            foreach (var file in Directory.GetFiles(path))
            {
                try
                {
                    File.Delete(file);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error deleting file {file}: {ex.Message}");
                }
            }

            // --- Verwijder subfolders behalve excludeSubfolder ---
            foreach (var dir in Directory.GetDirectories(path))
            {
                var dirName = new DirectoryInfo(dir).Name;
                if (!string.Equals(dirName, excludeSubfolder, StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        Directory.Delete(dir, true); // verwijder alles in de folder
                        Console.WriteLine($"Subfolder deleted: {dir}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error deleting subfolder {dir}: {ex.Message}");
                    }
                }
            }
        }

        /// <summary>
        /// Verwijdert een specifiek bestand als het bestaat.
        /// </summary>
        /// <param name="filePath">Volledig pad van het bestand dat verwijderd moet worden</param>
        public void DeleteFile(string filePath)
        {
            if (!File.Exists(filePath))
            {
                Console.WriteLine($"File does not exist: {filePath}");
                return;
            }

            try
            {
                File.Delete(filePath);
                Console.WriteLine($"File deleted: {filePath}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error deleting {filePath}: {ex.Message}");
            }
        }

    }
}
