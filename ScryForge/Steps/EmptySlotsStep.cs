using ScryForge.Models;
using ScryForge.Steps.Interfaces;
using Microsoft.Extensions.Logging;
using ScryForge.Services.Interfaces;

namespace ScryForge.Steps;

public class EmptySlotsStep(IEmptySlotsService emptySlotsService, ILogger<EmptySlotsStep> logger) : IPipelineStep
{
    public string Name => "Analyzing empty card slots";

    public async Task ExecuteAsync(PipelineContext context, CancellationToken ct)
    {
        var emptySlotsResult = await emptySlotsService.AnalyzeAsync(context.ScryfallCards, context.CustomCards, ct);
        if (HandleEmptySlots(emptySlotsResult))
        {
            return;
        }
    }

    private bool HandleEmptySlots(EmptySlotsResult result)
    {
        if (!result.HasEmptySlots)
        {
            logger.LogInformation("No empty slots detected in default or double-faced cards.");
            return false;
        }

        if (AppConfig.AutoFillEmptySlots)
        {
            logger.LogInformation(
                "There are {EmptyDefault} empty slot(s) on the last page of default cards, " +
                "{EmptyFlips} empty slot(s) on the last page of double-faced cards. Auto-fill is enabled, continuing...",
                result.EmptySlotsDefault, result.EmptySlotsFlips);
            return false;
        }

        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.WriteLine(
            $"Warning: {result.EmptySlotsDefault} empty slot(s) in default cards, {result.EmptySlotsFlips} empty slot(s) in double-faced cards.");
        Console.WriteLine("Press Enter to continue, or type 'Q' to quit.");
        Console.ResetColor();

        Console.Write("> ");
        string? input = Console.ReadLine();
        if (input?.Trim().Equals("Q", StringComparison.OrdinalIgnoreCase) == true)
        {
            logger.LogInformation("User chose to quit due to empty slots.");
            return true;
        }
        return false;
    }
}
