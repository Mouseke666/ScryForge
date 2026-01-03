using ScryForge.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ScryForge.Services.Intefaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System;
using ScryForge.Logging;
using Microsoft.Extensions.Logging.Console;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        // ---- Services ----
        builder.Services.AddSingleton<ICleanupService, CleanupService>();
        builder.Services.AddSingleton<OpenFolderService>();
        builder.Services.AddSingleton<ICardParserService, CardParserService>();
        builder.Services.AddSingleton<IDownloaderService, DownloaderService>();
        builder.Services.AddSingleton<UpscalerService>();
        builder.Services.AddSingleton<ICopyService, CopyService>();
        builder.Services.AddSingleton<ICardCopyService, CardCopyService>();
        builder.Services.AddSingleton<IPDFService, PDFService>();
        builder.Services.AddSingleton<PDFOpenService>();
        builder.Services.AddSingleton<IEmptySlotsService, EmptySlotsService>();
        builder.Services.AddSingleton<IPDFNameService, PDFNameService>();
        builder.Services.AddSingleton<ICustomCardService, CustomCardService>();
        builder.Services.AddHostedService<PipelineService>();

        // ---- HTTP Client ----
        builder.Services.AddHttpClient("Scryfall", client =>
        {
            client.BaseAddress = new Uri("https://api.scryfall.com/");
            client.Timeout = TimeSpan.FromMinutes(10);
        });

        // ---- Clean Logging ----
        builder.Logging.ClearProviders();

        // Voeg onze custom formatter toe
        builder.Logging.AddConsole(options =>
        {
            options.FormatterName = "clean";
        });
        builder.Logging.Services.AddSingleton<ConsoleFormatter, CleanConsoleFormatter>();

        // Filter externe logging weg
        builder.Logging.AddFilter("System.Net.Http.HttpClient", LogLevel.Warning);
        builder.Logging.AddFilter("Microsoft.Hosting.Lifetime", LogLevel.None);

        // ---- Configuration ----
        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .Build();

        AppConfig.Initialize(configuration);

        await builder.Build().RunAsync();
    }
}
