namespace ScryForge.Services
{
    public class PipelineService
    {
        private readonly CleanupService cleanup;
        private readonly OpenFolderService openfolder;
        private readonly CardParserService parser;
        private readonly DownloaderService downloader;
        private readonly UpscalerService upscaler;
        private readonly CopyService copy;
        private readonly FlipService flips;
        private readonly PDFService pdf;
        private readonly PDFOpenService openPdf;

        public PipelineService(
            CleanupService cleanup,
            OpenFolderService openfolder,
            CardParserService parser,
            DownloaderService downloader,
            UpscalerService upscaler,
            CopyService copy,
            FlipService flips,
            PDFService pdf,
            PDFOpenService openPdf)
        {
            this.cleanup = cleanup;
            this.openfolder = openfolder;
            this.parser = parser;
            this.downloader = downloader;
            this.upscaler = upscaler;
            this.copy = copy;
            this.flips = flips;
            this.pdf = pdf;
            this.openPdf = openPdf;
        }

        public async Task RunAsync()
        {
            Console.WriteLine(@"
            _________                    ___________                         
            /   _____/ ___________ ___.__.\_   _____/__________  ____   ____  
            \_____  \_/ ___\_  __ <   |  | |    __)/  _ \_  __ \/ ___\_/ __ \ 
            /        \  \___|  | \/\___  | |     \(  <_> )  | \/ /_/  >  ___/ 
            /_______  /\___  >__|   / ____| \___  / \____/|__|  \___  / \___  >
                    \/     \/       \/          \/             /_____/      \/ 
        ");

            Console.WriteLine("Cleaning...");
            cleanup.CleanDirectory(AppConfig.DownloadedFolder);
            cleanup.CleanDirectory(AppConfig.UpscaledFolder);
            cleanup.DeleteFile(Path.Combine(AppConfig.BasePath, "default.pdf"));
            cleanup.DeleteFile(Path.Combine(AppConfig.BasePath, "flips.pdf"));

            copy.CopyFile(
                Path.Combine(AppConfig.BasePath, "cards.txt"),
                Path.Combine(AppConfig.ArtDownloaderPath, "cards.txt")
            );

            Console.WriteLine("Downloading...");
            bool downloadSucceeded = await downloader.DownloadArtAsync();

            if (!downloadSucceeded)
            {
                Console.WriteLine("Download failed or was cancelled. Stopping application.");
                return;
            }

            copy.CopyFilesToRoot(AppConfig.ScryfallSource);

            Console.WriteLine("Upscaling...");
            await upscaler.RunUpscalerAsync();

            Console.WriteLine("Parsing cards...");
            var cards = parser.ParseCards(AppConfig.CardsFile);

            Console.WriteLine("Duplicating cards...");
            copy.DuplicateCards(cards);

            Console.WriteLine("Handling Flips...");
            flips.ProcessFlipCards(cards);

            Console.WriteLine("Generating PDF");
            await pdf.RunAsync("default");

            Console.WriteLine("Cleanup default PDF files");
            cleanup.CleanDirectory(AppConfig.UpscaledFolder, "flips");

            Console.WriteLine("Copy finished PDF to root");
            copy.MoveFile(
                Path.Combine(AppConfig.PdfPath, "default.pdf"),
                Path.Combine(AppConfig.BasePath, "default.pdf")
            );

            if (Directory.Exists(AppConfig.FlipsFolder) &&
                Directory.GetFiles(AppConfig.FlipsFolder).Length > 0)
            {
                Console.WriteLine("Copy flip cards to main");
                copy.CopyFolderFiles(AppConfig.FlipsFolder, AppConfig.UpscaledFolder);

                Console.WriteLine("Generating flips PDF");
                await pdf.RunAsync("flips");

                copy.MoveFile(
                    Path.Combine(AppConfig.PdfPath, "flips.pdf"),
                    Path.Combine(AppConfig.BasePath, "flips.pdf")
                );

                cleanup.CleanDirectory(AppConfig.UpscaledFolder);
            }
            else
            {
                Console.WriteLine("No flip cards found, skipping flip PDF generation.");
            }

            Console.WriteLine("Mission was a great success!");

            //openPdf.OpenPdf(Path.Combine(AppConfig.BasePath, "default.pdf"));
            openfolder.OpenFolder(AppConfig.BasePath);
        }
    }
}