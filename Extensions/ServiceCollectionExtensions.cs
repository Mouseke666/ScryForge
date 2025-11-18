using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using ScryForge.Services;

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
        services.AddSingleton<PDFOpenService>();     // of liever PdfOpener als je wilt hernoemen

        // PipelineService wordt een hosted service zodat je host.RunAsync() kunt gebruiken
        services.AddHostedService<PipelineService>();

        return services;
    }
}
