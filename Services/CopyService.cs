using MTGArtDownloader.Models;
using System;
using System.Collections.Generic;
using System.IO;

namespace MTGArtDownloader.Services
{
    public class CopyService
    {
        /// <summary>
        /// Dupliceert individuele kaarten op basis van hun quantity.
        /// </summary>
        public void DuplicateCards(List<CardInfo> cards)
        {
            string folder = AppConfig.UpscaledFolder;

            foreach (var card in cards)
            {
                // Zoek het bronbestand dat begint met de kaartnaam
                var files = Directory.GetFiles(folder, $"{card.FrontFileName}");

                if (files.Length == 0)
                {
                    Console.WriteLine($"Geen bronbestand gevonden voor kaart: {card.FrontFileName}");
                    continue;
                }

                // Gebruik het eerste gevonden bestand als bron
                var src = files[0];

                // Maak duplicaten vanaf _2 tot Quantity
                for (int i = 2; i <= card.Quantity; i++)
                {
                    var dest = Path.Combine(folder, $"{Path.GetFileNameWithoutExtension(src)}_{i}.png");

                    // Altijd overschrijven zodat er geen bestanden worden overgeslagen
                    File.Copy(src, dest, true);
                }
            }
        }

        /// <summary>
        /// Kopieert alle bestanden van een bronfolder naar een doelfolder.
        /// </summary>
        /// <param name="sourceFolder">Folder waarvan de bestanden gekopieerd moeten worden</param>
        /// <param name="destinationFolder">Folder waar de bestanden naartoe gekopieerd worden</param>
        /// <param name="overwrite">True = overschrijf bestaande bestanden, False = sla over</param>
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

        /// <summary>
        /// Verplaatst een enkel bestand van source naar destination.
        /// </summary>
        /// <param name="sourceFile">Volledige pad van het bronbestand</param>
        /// <param name="destinationFolder">Folder waar het bestand naartoe moet</param>
        /// <param name="overwrite">True = overschrijf bestaande bestanden</param>
        public void MoveFile(string sourceFile, string destinationFolder, bool overwrite = true)
        {
            if (!File.Exists(sourceFile))
            {
                Console.WriteLine($"Sourcefile does not exist: {sourceFile}");
                return;
            }

            try
            {
                // Als overwrite = true, verwijder het doelbestand eerst
                if (overwrite && File.Exists(destinationFolder))
                    File.Delete(destinationFolder);

                File.Copy(sourceFile, destinationFolder, overwrite);
                File.Delete(sourceFile); // verwijder origineel
                Console.WriteLine($"File moved: {sourceFile} -> {destinationFolder}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error while moving from {sourceFile} to {destinationFolder}: {ex.Message}");
            }
        }

        /// <summary>
        /// Kopieert een enkel bestand van source naar destination zonder het origineel te verwijderen.
        /// </summary>
        /// <param name="sourceFile">Volledig pad van het bronbestand</param>
        /// <param name="destinationFolder">Folder waar het bestand naartoe moet</param>
        /// <param name="overwrite">True = overschrijf bestaande bestanden</param>
        public void CopyFile(string sourceFile, string destinationFolder, bool overwrite = true)
        {
            if (!File.Exists(sourceFile))
            {
                Console.WriteLine($"Source file does not exist: {sourceFile}");
                return;
            }

            var destPath = Path.Combine(Path.GetDirectoryName(destinationFolder), Path.GetFileName(sourceFile));

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
