using ScryForge;
using ScryForge.Logging;
using ScryForge.Services;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ScryForge.Services.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Console;
using Microsoft.Extensions.DependencyInjection;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        builder.Services.AddSingleton<System.IO.Abstractions.IFileSystem, System.IO.Abstractions.FileSystem>();

        builder.Services.AddSingleton<ICleanupService, CleanupService>();
        builder.Services.AddSingleton<IOpenFolderService, OpenFolderService>();
        builder.Services.AddSingleton<ICardParserService, CardParserService>();
        builder.Services.AddSingleton<IDownloaderService, ScryFallDownloaderService>();
        builder.Services.AddSingleton<IUpscalerService, UpscalerService>();
        builder.Services.AddSingleton<ICopyService, CopyService>();
        builder.Services.AddSingleton<ICardCopyService, CardCopyService>();
        builder.Services.AddSingleton<IPDFService, PDFService>();
        builder.Services.AddSingleton<IPDFOpenService, PDFOpenService>();
        builder.Services.AddSingleton<IEmptySlotsService, EmptySlotsService>();
        builder.Services.AddSingleton<IPDFNameService, PDFNameService>();
        builder.Services.AddSingleton<ICustomCardService, CustomCardService>();
        builder.Services.AddHostedService<PipelineService>();

        builder.Services.AddHttpClient("Scryfall", client =>
        {
            client.BaseAddress = new Uri("https://api.scryfall.com/");
            client.Timeout = TimeSpan.FromMinutes(10);
        });

        builder.Logging.ClearProviders();

        builder.Logging.AddConsole(options =>
        {
            options.FormatterName = "clean";
        });
        builder.Logging.Services.AddSingleton<ConsoleFormatter, CleanConsoleFormatter>();

        builder.Logging.AddFilter("System.Net.Http.HttpClient", LogLevel.Warning);
        builder.Logging.AddFilter("Microsoft.Hosting.Lifetime", LogLevel.None);

        var configuration = new ConfigurationBuilder()
            .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .Build();

        AppConfig.Initialize(configuration);

        await builder.Build().RunAsync();
    }
}
