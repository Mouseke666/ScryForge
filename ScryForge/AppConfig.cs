using Microsoft.Extensions.Configuration;

namespace ScryForge
{
    public static class AppConfig
    {
        public static readonly string BasePath = AppDomain.CurrentDomain.BaseDirectory;
        public static readonly string LogPath = Path.Combine(BasePath, "Log");
        public static readonly string ScryForgeDownloaderPath = Path.Combine(BasePath, "Download");
        public static readonly string CardsFile = Path.Combine(BasePath, "cards.txt");
        public static readonly string OutputFolder = Path.Combine(BasePath, "Output");
        public static readonly string CustomFolder = Path.Combine(BasePath, "Custom");
        public static readonly string PdfPath = Path.Combine(BasePath, "PDF");
        public static readonly string PDFImagesFolder = Path.Combine(PdfPath, "images");
        public static readonly string FlipsFolder = Path.Combine(PDFImagesFolder, "flips");
        public static readonly string UpscalerPath = Path.Combine(BasePath, "Upscaler");
        public static readonly string UpscalerExe = Path.Combine(UpscalerPath, "realesrgan-ncnn-vulkan.exe");
        public static readonly string PDFPath = Path.Combine(BasePath, "PDF");
        public static readonly string PDFExe = Path.Combine(PDFPath, "proxy_pdf_cli.exe");
        public static string UpscaleModel { get; private set; } = "digital-art-4x";
        public static int UpscaleScale { get; private set; } = 4;
        public static int UpscalerThreads { get; private set; } = Environment.ProcessorCount;
        public static bool AutoFillEmptySlots { get; private set; } = false;
        public static bool AutoUseSuggestedName { get; private set; } = false;

        public static void Initialize(IConfiguration config)
        {
            UpscaleModel = config["Upscaler:Model"] ?? UpscaleModel;

            if (int.TryParse(config["Upscaler:Scale"], out var scale))
            {
                UpscaleScale = scale;
            }

            if (int.TryParse(config["Upscaler:Threads"], out var threads))
            {
                UpscalerThreads = threads;
            }

            if (bool.TryParse(config["Pdf:AutoFillEmptySlots"], out var autoFill))
            {
                AutoFillEmptySlots = autoFill;
            }

            if (bool.TryParse(config["Pdf:AutoUseSuggestedName"], out var autoName))
            {
                AutoUseSuggestedName = autoName;
            }
        }
    }
}