using ScryForge.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

internal class Program
{
    static async Task Main()
    {
        var host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                services.AddSingleton<CleanupService>();
                services.AddSingleton<CardParserService>();
                services.AddSingleton<DownloaderService>();
                services.AddSingleton<UpscalerService>();
                services.AddSingleton<CopyService>();
                services.AddSingleton<FlipService>();
                services.AddSingleton<PDFService>();
                services.AddSingleton<PDFOpenService>();
                services.AddSingleton<PipelineService>();
            })
            .Build();

        await host.Services.GetRequiredService<PipelineService>().RunAsync();
    }
}