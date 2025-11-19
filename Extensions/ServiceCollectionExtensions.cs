using ScryForge.Services;
using Microsoft.Extensions.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddScryForgeServices(this IServiceCollection services)
    {
        services.AddSingleton<CleanupService>();
        services.AddSingleton<OpenFolderService>();
        services.AddSingleton<CardParserService>();
        services.AddSingleton<DownloaderService>();
        services.AddSingleton<UpscalerService>();
        services.AddSingleton<CopyService>();
        services.AddSingleton<FlipService>();
        services.AddSingleton<PDFService>();
        services.AddSingleton<PDFOpenService>();

        services.AddHostedService<PipelineService>();

        return services;
    }
}