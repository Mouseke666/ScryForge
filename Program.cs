using ScryForge.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using System;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        // Services
        builder.Services.AddSingleton<CleanupService>();
        builder.Services.AddSingleton<OpenFolderService>();
        builder.Services.AddSingleton<CardParserService>();
        builder.Services.AddSingleton<IDownloaderService, ScryForgeDownloaderService>();
        //builder.Services.AddSingleton<IDownloaderService, DownloaderService>();
        builder.Services.AddSingleton<UpscalerService>();
        builder.Services.AddSingleton<CopyService>();
        builder.Services.AddSingleton<FlipService>();
        builder.Services.AddSingleton<PDFService>();
        builder.Services.AddSingleton<PDFOpenService>();
        builder.Services.AddHostedService<PipelineService>();

        // Named HttpClient voor Scryfall
        builder.Services.AddHttpClient("Scryfall", client =>
        {
            client.BaseAddress = new Uri("https://api.scryfall.com/");
            client.Timeout = TimeSpan.FromMinutes(10);
        });

        // Logging
        builder.Logging.ClearProviders();
        builder.Logging.AddSimpleConsole(options =>
        {
            options.IncludeScopes = false;
            options.SingleLine = true;
            options.TimestampFormat = "HH:mm:ss ";
            options.UseUtcTimestamp = false;
        });

        // Filters
        builder.Logging.AddFilter("System.Net.Http.HttpClient", LogLevel.Warning); // Geen info/debug van HttpClient
        builder.Logging.AddFilter("Microsoft.Hosting.Lifetime", LogLevel.None);    // Geen host lifetime logs

        await builder.Build().RunAsync();
    }
}
