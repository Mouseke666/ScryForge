namespace ScryForge.Services
{
    public class CleanupService
    {
        public void CleanDirectory(string path, string? excludeSubfolder = null)
        {
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
                return;
            }

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

            foreach (var dir in Directory.GetDirectories(path))
            {
                var dirName = new DirectoryInfo(dir).Name;
                if (!string.Equals(dirName, excludeSubfolder, StringComparison.OrdinalIgnoreCase))
                {
                    try
                    {
                        Directory.Delete(dir, true);
                        Console.WriteLine($"Subfolder deleted: {dir}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error deleting subfolder {dir}: {ex.Message}");
                    }
                }
            }
        }

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