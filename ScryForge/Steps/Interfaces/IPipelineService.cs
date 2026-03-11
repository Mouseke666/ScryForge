namespace ScryForge.Steps.Interfaces;

public interface IPipelineStep
{
    string Name { get; }
    Task ExecuteAsync(PipelineContext context, CancellationToken ct);
}