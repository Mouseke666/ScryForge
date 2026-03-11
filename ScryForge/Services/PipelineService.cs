using ScryForge.Steps;
using ScryForge.Models;
using System.Diagnostics;
using ScryForge.Steps.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ScryForge.Services;

public class PipelineService(IEnumerable<IPipelineStep> steps, ILogger<PipelineService> logger, IHostApplicationLifetime lifetime) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var context = new PipelineContext();
        var stepList = steps.ToList();
        int stepNumber = 1;
        int total = stepList.Count;

        // Stopwatch voor totale pipeline
        var totalSw = Stopwatch.StartNew();

        foreach (var step in stepList)
        {
            if (stoppingToken.IsCancellationRequested)
            {
                logger.LogInformation("Pipeline cancelled");
                return;
            }

            logger.LogInformation("\nStep {Step}/{Total}: {Name}\n", stepNumber++, total, step.Name);

            var sw = Stopwatch.StartNew();
            try
            {
                await step.ExecuteAsync(context, stoppingToken);
                sw.Stop();

                var elapsedSeconds = Math.Round(sw.ElapsedMilliseconds / 1000.0, 2);
                context.TotalElapsedSeconds += elapsedSeconds;
                logger.LogInformation("\nStep {Name} completed in {Elapsed}s", step.Name, elapsedSeconds);
            }
            catch (OperationCanceledException)
            {
                logger.LogInformation("Pipeline cancelled during step {Step}", step.Name);
                return;
            }
            catch (PipelineAbortException ex)
            {
                logger.LogWarning("Pipeline aborted: {Message}", ex.Message);
                lifetime.StopApplication();
                return;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Pipeline failed in step {Step}", step.Name);
                lifetime.StopApplication();
                return;
            }
        }

        totalSw.Stop();
        var totalElapsed = Math.Round(totalSw.ElapsedMilliseconds / 1000.0, 2);
        logger.LogInformation("\nPipeline completed successfully in {TotalElapsed}s", totalElapsed);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("PipelineService stopping");
        await base.StopAsync(cancellationToken);
    }
}