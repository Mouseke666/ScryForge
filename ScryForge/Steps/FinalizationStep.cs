using Microsoft.Extensions.Logging;
using ScryForge.Steps.Interfaces;

namespace ScryForge.Steps;

public class FinalizationStep(ILogger<FinalizationStep> logger) : IPipelineStep
{
    public string Name => "Finalization";

    public async Task ExecuteAsync(PipelineContext context, CancellationToken ct)
    {
        logger.LogInformation("Pipeline finished in {TotalElapsedSeconds}s\nThank you for using ScryForge!",
            Math.Round(context.TotalElapsedSeconds, 2));

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine("Press any key to exit...");
        Console.ResetColor();

        _ = Console.ReadKey(true);
        Environment.Exit(0);
    }
}