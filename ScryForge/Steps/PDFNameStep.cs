using System.IO;
using ScryForge.Steps.Interfaces;
using Microsoft.Extensions.Logging;
using ScryForge.Services.Interfaces;

namespace ScryForge.Steps;

public class PDFNameStep(IPDFNameService pdfNameService, ILogger<PDFNameStep> logger) : IPipelineStep
{
    public string Name => "Determining PDF name";

    public async Task ExecuteAsync(PipelineContext context, CancellationToken ct)
    {
        if (AppConfig.TestUpscalers)
        {
            // TestUpscalers mode: auto-generate name like TestUpscalers1, TestUpscalers2, etc.
            int number = 1;
            string candidatePath;

            do
            {
                candidatePath = Path.Combine(AppConfig.PdfPath, $"TestUpscalers{number}.pdf");
                number++;
            } while (File.Exists(candidatePath));

            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            context.FullName = $"TestUpscalers{number - 1}_{timestamp}";

            logger.LogInformation("Generated PDF name for TestUpscalers: {FullName}", context.FullName);
        }
        else
        {
            // Normal mode: interactive user input
            var pdfNameResult = await pdfNameService.DeterminePdfNameAsync(AppConfig.CardsFile);
            logger.LogInformation("Suggested PDF name: {Suggested}", pdfNameResult.BaseNameWithoutTimestamp);

            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Enter PDF name (press Enter to accept suggested name):");
            Console.ResetColor();

            Console.Write("> ");
            string? input = Console.ReadLine();
            string finalBaseName = string.IsNullOrWhiteSpace(input) ? pdfNameResult.BaseNameWithoutTimestamp : input.Trim();
            context.FullName = $"{finalBaseName}_{pdfNameResult.Timestamp}";

            logger.LogInformation("Using PDF base name: {FullName}", context.FullName);
        }
    }
}
