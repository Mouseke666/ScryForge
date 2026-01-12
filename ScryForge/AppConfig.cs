using Microsoft.Extensions.Logging;
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
        public static readonly string PDFExe = Path.Combine(PdfPath, "proxy_pdf_cli.exe");

        public static List<UpscalerConfig> Upscalers { get; private set; } = [];
        public static bool AutoFillEmptySlots { get; private set; } = false;
        public static bool AutoUseSuggestedName { get; private set; } = false;

        public static void Initialize(IConfiguration config, ILogger? logger = null)
        {
            string? autoFillValue = config["Pdf:AutoFillEmptySlots"];
            if (!string.IsNullOrEmpty(autoFillValue))
            {
                if (bool.TryParse(autoFillValue, out var autoFill))
                {
                    AutoFillEmptySlots = autoFill;
                }
                else
                {
                    AutoFillEmptySlots = false;
                    logger?.LogWarning(
                        "Invalid boolean value for Pdf:AutoFillEmptySlots: '{Value}'. Defaulting to false.",
                        autoFillValue
                    );
                }
            }

            string? autoUseNameValue = config["Pdf:AutoUseSuggestedName"];
            if (!string.IsNullOrEmpty(autoUseNameValue))
            {
                if (bool.TryParse(autoUseNameValue, out var autoName))
                {
                    AutoUseSuggestedName = autoName;
                }
                else
                {
                    AutoUseSuggestedName = false;
                    logger?.LogWarning(
                        "Invalid boolean value for Pdf:AutoUseSuggestedName: '{Value}'. Defaulting to false.",
                        autoUseNameValue
                    );
                }
            }

            var upscalerSection = config.GetSection("Upscalers");
            if (upscalerSection.Exists())
            {
                Upscalers = upscalerSection.Get<List<UpscalerConfig>>() ?? new List<UpscalerConfig>();
            }

            ValidateUpscalerConfigs(logger);
        }

        private static void ValidateUpscalerConfigs(ILogger? logger)
        {
            if (Upscalers == null || Upscalers.Count == 0)
                throw new InvalidOperationException("At least one UpscalerConfig must be defined.");

            var ordered = Upscalers
                .OrderBy(u => u.YearRange.From ?? int.MinValue)
                .ToList();

            var first = ordered[0];
            if (first.YearRange.From != null)
                throw new InvalidOperationException(
                    $"The first UpscalerConfig ('{first.Name}') must have YearRange.From = null."
                );

            for (int i = 0; i < ordered.Count; i++)
            {
                var current = ordered[i];

                if (current.YearRange.From.HasValue && current.YearRange.To.HasValue &&
                    current.YearRange.From.Value > current.YearRange.To.Value)
                {
                    throw new InvalidOperationException(
                        $"Invalid year range in '{current.Name}': From cannot be greater than To."
                    );
                }

                if (i > 0)
                {
                    var prev = ordered[i - 1];

                    if (!prev.YearRange.To.HasValue)
                    {
                        throw new InvalidOperationException(
                            $"'{prev.Name}' ends at null (open-ended). No ranges may follow after an open-ended range."
                        );
                    }

                    int expectedFrom = prev.YearRange.To.Value + 1;

                    if (!current.YearRange.From.HasValue || current.YearRange.From.Value != expectedFrom)
                    {
                        throw new InvalidOperationException(
                            $"Range of '{current.Name}' must start at {expectedFrom}, but starts at " +
                            (current.YearRange.From?.ToString() ?? "null") + "."
                        );
                    }
                }
            }

            logger?.LogInformation("Upscaler configuration validated successfully.");
        }
    }

    public class UpscalerConfig
    {
        public string Name { get; set; } = string.Empty;
        public string Model { get; set; } = "digital-art-4x";
        public int Scale { get; set; } = 4;
        public YearRangeConfig YearRange { get; set; } = new YearRangeConfig();
    }

    public class YearRangeConfig
    {
        public int? From { get; set; }
        public int? To { get; set; }
    }
}