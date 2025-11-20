using ScryForge.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);
        builder.Services.AddSingleton<CleanupService>();
        builder.Services.AddSingleton<OpenFolderService>();
        builder.Services.AddSingleton<CardParserService>();
        builder.Services.AddSingleton<IDownloaderService, DownloaderService>();
        builder.Services.AddSingleton<UpscalerService>();
        builder.Services.AddSingleton<CopyService>();
        builder.Services.AddSingleton<FlipService>();
        builder.Services.AddSingleton<PDFService>();
        builder.Services.AddSingleton<PDFOpenService>();
        builder.Services.AddHostedService<PipelineService>();
        builder.Services.AddHttpClient("Scryfall", client =>
        {
            client.BaseAddress = new Uri("https://api.scryfall.com/");
            client.Timeout = TimeSpan.FromMinutes(10);
        });
        builder.Logging.ClearProviders();
        builder.Logging.AddSimpleConsole(options =>
        {
            options.IncludeScopes = false;
            options.SingleLine = true;
            options.TimestampFormat = "HH:mm:ss ";
            options.UseUtcTimestamp = false;
        });

        builder.Logging.AddFilter("Microsoft.Hosting.Lifetime", LogLevel.None);

        await builder.Build().RunAsync();
    }
}