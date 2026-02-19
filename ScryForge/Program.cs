using ScryForge;
using System.Text;
using System.Text.Json;
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
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        var builder = Host.CreateApplicationBuilder(args);

        ConfigureLogging(builder);

        using var loggerFactory = LoggerFactory.Create(logging =>
        {
            logging.AddConsole(options => options.FormatterName = "clean");
        });

        ILogger logger = loggerFactory.CreateLogger<Program>();

        var configuration = LoadConfiguration(builder.Logging, logger);
        AppConfig.Initialize(configuration, logger);
        RegisterServices(builder);

        await builder.Build().RunAsync();
    }


    private static void ConfigureLogging(HostApplicationBuilder builder)
    {
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole(options => options.FormatterName = "clean");
        builder.Logging.Services.AddSingleton<ConsoleFormatter, CleanConsoleFormatter>();

        builder.Logging.AddFilter("System.Net.Http.HttpClient", LogLevel.Warning);
        builder.Logging.AddFilter("Microsoft.Hosting.Lifetime", LogLevel.None);
    }

    private static IConfiguration LoadConfiguration(ILoggingBuilder loggingBuilder, ILogger logger)
    {
        string configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");

        if (!File.Exists(configPath))
        {
            logger.LogCritical("Configuration file not found: {Path}", configPath);
            throw new FileNotFoundException("Configuration file missing.", configPath);
        }

        string json = File.ReadAllText(configPath);

        try
        {
            using var _ = JsonDocument.Parse(json); // syntaxis check
        }
        catch (JsonException ex)
        {
            logger.LogCritical(
                "Configuration file contains invalid JSON. Line {Line}, Position {Pos}. Error: {Error}",
                ex.LineNumber,
                ex.BytePositionInLine,
                ex.Message
            );
            throw new InvalidDataException($"Invalid JSON in {configPath}", ex);
        }

        // Return config via stream zodat we JSON niet opnieuw hoeven in te lezen
        return new ConfigurationBuilder()
            .AddJsonStream(new MemoryStream(System.Text.Encoding.UTF8.GetBytes(json)))
            .Build();
    }

    private static void RegisterServices(HostApplicationBuilder builder)
    {
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
    }
}