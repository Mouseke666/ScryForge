using Microsoft.Extensions.Logging;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using ScryForge.Services.Interfaces;

namespace ScryForge.Services;

public class CornerFillService(ILogger<CornerFillService> logger) : ICornerFillService
{
    private readonly ILogger<CornerFillService> _logger = logger;

    private enum Corner
    {
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight
    }

    private static readonly string[] SupportedExtensions =
    [
        ".jpg",
        ".jpeg",
        ".png",
        ".webp",
        ".bmp",
        ".tif",
        ".tiff"
    ];

    // MTG standaard formaat
    private const double CardWidthMm = 63.5;
    private const double CardHeightMm = 88.9;


    // public Task FillRoundedCornersAsync(
    //     string inputFolder,
    //     string outputFolder)
    // {
    //     return FillRoundedCornersAsync(
    //         inputFolder,
    //         outputFolder,
    //         radiusMm: 2.5,
    //         whiteThreshold: 235,
    //         alphaThreshold: 250,
    //         sampleInset: 1.5,
    //         overpaintMm: 1.0,
    //         force: false);
    // }


    public async Task FillRoundedCornersAsync(
    string inputFolder,
    string outputFolder)
    {
        await FillRoundedCornersAsync(
            inputFolder,
            outputFolder,
            radiusMm: 2.5,
            whiteThreshold: 235,
            alphaThreshold: 250,
            sampleInset: 1.5,
            overpaintMm: 1.0,
            force: false);
    }


    public async Task FillRoundedCornersAsync(
        string inputFolder,
        string outputFolder,
        double radiusMm,
        int whiteThreshold,
        int alphaThreshold,
        double sampleInset,
        double overpaintMm,
        bool force)
    {
        if (!Directory.Exists(inputFolder))
        {
            _logger.LogWarning(
                "Input folder does not exist: {Folder}",
                inputFolder);

            return;
        }

        Directory.CreateDirectory(outputFolder);


        var files = Directory
            .EnumerateFiles(inputFolder)
            .Where(x =>
                SupportedExtensions.Contains(
                    Path.GetExtension(x),
                    StringComparer.OrdinalIgnoreCase))
            .OrderBy(x => x)
            .ToList();


        if (files.Count == 0)
        {
            _logger.LogInformation(
                "No images found in {Folder}",
                inputFolder);

            return;
        }


        Console.WriteLine(
            $"Filling rounded corners on {files.Count} cards");


        int total = files.Count;
        int current = 0;


        foreach (string file in files)
        {
            current++;

            string cardName =
                Path.GetFileNameWithoutExtension(file);


            Console.WriteLine(
                $"Filling corners [{current}/{total}] - {cardName}");


            try
            {
                string output =
                    Path.Combine(
                        outputFolder,
                        Path.GetFileName(file));


                await ProcessImageAsync(
                    file,
                    output,
                    radiusMm,
                    whiteThreshold,
                    alphaThreshold,
                    sampleInset,
                    overpaintMm,
                    force);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Failed processing {File}",
                    file);
            }
        }


        Console.WriteLine(
            "Corner filling complete.");
    }



    private async Task ProcessImageAsync(
        string inputFile,
        string outputFile,
        double radiusMm,
        int whiteThreshold,
        int alphaThreshold,
        double sampleInset,
        double overpaintMm,
        bool force)
    {
        using Image<Rgba32> image =
            await Image.LoadAsync<Rgba32>(inputFile);


        int width = image.Width;
        int height = image.Height;


        int radius =
            CalculateRadiusPx(
                width,
                height,
                radiusMm);


        int overpaint =
            CalculateMmPx(
                width,
                height,
                overpaintMm);


        overpaint =
            Math.Min(
                overpaint,
                Math.Max(0, radius - 2));


        if (radius * 2 >= Math.Min(width, height))
        {
            throw new InvalidOperationException(
                $"Radius {radius}px too large for image {width}x{height}");
        }


        foreach (Corner corner in Enum.GetValues<Corner>())
        {
            ProcessCorner(
                image,
                corner,
                radius,
                overpaint,
                whiteThreshold,
                alphaThreshold,
                sampleInset,
                force);
        }


        Directory.CreateDirectory(
            Path.GetDirectoryName(outputFile)!);


        await image.SaveAsync(outputFile);
    }



    private static int CalculateRadiusPx(
        int width,
        int height,
        double radiusMm)
    {
        double pxPerMmX =
            width / CardWidthMm;

        double pxPerMmY =
            height / CardHeightMm;


        double pxPerMm =
            (pxPerMmX + pxPerMmY) / 2.0;


        return Math.Max(
            2,
            (int)Math.Round(radiusMm * pxPerMm));
    }



    private static int CalculateMmPx(
        int width,
        int height,
        double mm)
    {
        double pxPerMmX =
            width / CardWidthMm;

        double pxPerMmY =
            height / CardHeightMm;


        double pxPerMm =
            (pxPerMmX + pxPerMmY) / 2.0;


        return Math.Max(
            0,
            (int)Math.Round(mm * pxPerMm));
    }



    private static void ProcessCorner(
        Image<Rgba32> image,
        Corner corner,
        int radius,
        int overpaint,
        int whiteThreshold,
        int alphaThreshold,
        double sampleInset,
        bool force)
    {
        Rectangle box =
            GetCornerBox(
                corner,
                image.Width,
                image.Height,
                radius);


        // Wordt ingevuld in deel 2
        bool[,] originalMask =
            CreateGeometricMask(
                corner,
                radius,
                0);


        bool[,] geometricMask =
            CreateGeometricMask(
                corner,
                radius,
                overpaint);


        bool[,] fillMask =
            CreateFillMask(
                image,
                box,
                geometricMask,
                originalMask,
                whiteThreshold,
                alphaThreshold,
                force);



        if (!HasPixels(fillMask))
            return;


        RadialFill(
            image,
            box,
            corner,
            fillMask,
            radius,
            sampleInset,
            whiteThreshold,
            alphaThreshold);
    }

    private static Rectangle GetCornerBox(
    Corner corner,
    int width,
    int height,
    int radius)
    {
        return corner switch
        {
            Corner.TopLeft =>
                new Rectangle(
                    0,
                    0,
                    radius,
                    radius),

            Corner.TopRight =>
                new Rectangle(
                    width - radius,
                    0,
                    radius,
                    radius),

            Corner.BottomLeft =>
                new Rectangle(
                    0,
                    height - radius,
                    radius,
                    radius),

            Corner.BottomRight =>
                new Rectangle(
                    width - radius,
                    height - radius,
                    radius,
                    radius),

            _ => throw new ArgumentOutOfRangeException(nameof(corner))
        };
    }



    private static bool[,] CreateGeometricMask(
        Corner corner,
        int radius,
        int overpaintPx)
    {
        bool[,] mask =
            new bool[radius, radius];


        var center =
            GetArcCenter(
                corner,
                radius);


        double effectiveRadius =
            Math.Max(
                1,
                (radius - 1) - overpaintPx);



        for (int y = 0; y < radius; y++)
        {
            for (int x = 0; x < radius; x++)
            {
                double dx =
                    x - center.X;

                double dy =
                    y - center.Y;


                double distance =
                    Math.Sqrt(
                        dx * dx +
                        dy * dy);


                // Alles buiten de cirkel moet gevuld worden
                mask[y, x] =
                    distance > effectiveRadius;
            }
        }


        return mask;
    }



    private static PointF GetArcCenter(
        Corner corner,
        int radius)
    {
        float edge =
            radius - 1;


        return corner switch
        {
            Corner.TopLeft =>
                new PointF(
                    edge,
                    edge),


            Corner.TopRight =>
                new PointF(
                    0,
                    edge),


            Corner.BottomLeft =>
                new PointF(
                    edge,
                    0),


            Corner.BottomRight =>
                new PointF(
                    0,
                    0),


            _ => throw new ArgumentOutOfRangeException(nameof(corner))
        };
    }



    private static bool[,] CreateFillMask(
        Image<Rgba32> image,
        Rectangle box,
        bool[,] geometricMask,
        bool[,] originalMask,
        int whiteThreshold,
        int alphaThreshold,
        bool force)
    {
        int width =
            box.Width;

        int height =
            box.Height;


        bool[,] result =
            new bool[height, width];


        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                bool geometric =
                    geometricMask[y, x];


                if (!geometric)
                    continue;


                if (force)
                {
                    result[y, x] = true;
                    continue;
                }


                Rgba32 pixel =
                    image[
                        box.Left + x,
                        box.Top + y];


                bool transparent =
                    pixel.A < alphaThreshold;


                bool white =
                    pixel.R >= whiteThreshold &&
                    pixel.G >= whiteThreshold &&
                    pixel.B >= whiteThreshold;



                bool backgroundPart =
                    originalMask[y, x] &&
                    (transparent || white);



                // altijd de interne overpaint overschrijven
                bool inwardBand =
                    geometric &&
                    !originalMask[y, x];


                result[y, x] =
                    backgroundPart ||
                    inwardBand;
            }
        }


        return result;
    }



    private static bool HasPixels(
        bool[,] mask)
    {
        int height =
            mask.GetLength(0);

        int width =
            mask.GetLength(1);


        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (mask[y, x])
                    return true;
            }
        }


        return false;
    }

    private static void RadialFill(
    Image<Rgba32> image,
    Rectangle box,
    Corner corner,
    bool[,] fillMask,
    int radius,
    double sampleInset,
    int whiteThreshold,
    int alphaThreshold)
    {
        int height = fillMask.GetLength(0);
        int width = fillMask.GetLength(1);


        PointF center =
            GetArcCenter(
                corner,
                radius);



        // Maak een snapshot zodat we niet kleuren gebruiken
        // die al aangepast zijn tijdens het vullen.
        Rgba32[,] source =
            new Rgba32[height, width];


        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                source[y, x] =
                    image[
                        box.Left + x,
                        box.Top + y];
            }
        }



        bool[,] validSource =
            new bool[height, width];


        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (fillMask[y, x])
                    continue;


                Rgba32 pixel =
                    source[y, x];


                bool valid =
                    pixel.A >= alphaThreshold &&
                    Math.Min(
                        Math.Min(pixel.R, pixel.G),
                        pixel.B) < whiteThreshold;


                validSource[y, x] = valid;
            }
        }



        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (!fillMask[y, x])
                    continue;



                double dx =
                    x - center.X;

                double dy =
                    y - center.Y;


                double distance =
                    Math.Sqrt(
                        dx * dx +
                        dy * dy);


                if (distance < 0.001)
                    continue;


                double unitX =
                    dx / distance;

                double unitY =
                    dy / distance;



                (int X, int Y)? found =
                    FindSourcePixel(
                        validSource,
                        center,
                        unitX,
                        unitY,
                        distance);



                double sourceDistance;


                if (found.HasValue)
                {
                    // iets naar binnen om anti-aliasing te vermijden
                    double foundDx =
                        found.Value.X - center.X;

                    double foundDy =
                        found.Value.Y - center.Y;


                    sourceDistance =
                        Math.Sqrt(
                            foundDx * foundDx +
                            foundDy * foundDy)
                        - sampleInset;
                }
                else
                {
                    sourceDistance =
                        Math.Max(
                            0,
                            radius - 1 - sampleInset);
                }



                int sourceX =
                    Clamp(
                        (int)Math.Round(
                            center.X +
                            unitX * sourceDistance),
                        0,
                        width - 1);


                int sourceY =
                    Clamp(
                        (int)Math.Round(
                            center.Y +
                            unitY * sourceDistance),
                        0,
                        height - 1);



                Rgba32 color =
                    source[sourceY, sourceX];


                image[
                    box.Left + x,
                    box.Top + y] =
                    new Rgba32(
                        color.R,
                        color.G,
                        color.B,
                        255);
            }
        }
    }



    private static (int X, int Y)? FindSourcePixel(
        bool[,] validSource,
        PointF center,
        double unitX,
        double unitY,
        double startDistance)
    {
        int height =
            validSource.GetLength(0);

        int width =
            validSource.GetLength(1);



        double distance =
            startDistance - 0.5;


        while (distance >= 0)
        {
            int x =
                Clamp(
                    (int)Math.Round(
                        center.X +
                        unitX * distance),
                    0,
                    width - 1);


            int y =
                Clamp(
                    (int)Math.Round(
                        center.Y +
                        unitY * distance),
                    0,
                    height - 1);



            if (validSource[y, x])
            {
                return (x, y);
            }


            distance -= 0.5;
        }


        return null;
    }



    private static int Clamp(
        int value,
        int min,
        int max)
    {
        if (value < min)
            return min;

        if (value > max)
            return max;

        return value;
    }
}