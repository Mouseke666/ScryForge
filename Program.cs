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
        builder.Services.AddSingleton<DownloaderService>();
        builder.Services.AddSingleton<UpscalerService>();
        builder.Services.AddSingleton<CopyService>();
        builder.Services.AddSingleton<FlipService>();
        builder.Services.AddSingleton<PDFService>();
        builder.Services.AddSingleton<PDFOpenService>();
        builder.Services.AddHostedService<PipelineService>();

        builder.Logging.ClearProviders();

        builder.Logging.AddSimpleConsole(options =>
        {
            options.IncludeScopes = false;
            options.SingleLine = true;
            options.TimestampFormat = "HH:mm:ss ";
            options.UseUtcTimestamp = false;
        });

        // ✨ dit onderdrukt category+level in output
        builder.Logging.AddFilter((category, level) =>
        {
            if (category.StartsWith("Microsoft.Hosting.Lifetime"))
                return false;

            if (category.StartsWith("ScryForge.Services"))
                return true; // laat de logger werken, maar formatter toont geen categorie/level

            return true;
        });

        await builder.Build().RunAsync();
    }
}
