
public static class AppConfig
{
    public static readonly string BasePath = AppDomain.CurrentDomain.BaseDirectory;
    public static readonly string LogPath = Path.Combine(BasePath, "Log");
    public static readonly string ScryForgeDownloaderPath = Path.Combine(BasePath, "Download");
    public static readonly string CardsFile = Path.Combine(BasePath, "cards.txt");
    public static readonly string PdfPath = Path.Combine(BasePath, "PDF");
    public static readonly string UpscaledFolder = Path.Combine(PdfPath, "images");
    public static readonly string FlipsFolder = Path.Combine(UpscaledFolder, "flips");
    public static readonly string UpscalerPath = Path.Combine(BasePath, "Upscaler");
    public static readonly string UpscalerExe = Path.Combine(UpscalerPath, "realesrgan-ncnn-vulkan.exe");
    public static readonly string PDFPath = Path.Combine(BasePath, "PDF");
    public static readonly string PDFExe = Path.Combine(PDFPath, "proxy_pdf_cli.exe");
    public const string UpscaleModel = "digital-art-4x";
    public const int UpscaleScale = 4;
}