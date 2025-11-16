
namespace MTGArtDownloader.Services
{
    public class UpscalerService
    {
        public async Task RunUpscalerAsync()
        {
            var exe = AppConfig.UpscalerExe;
            if (!File.Exists(exe))
            {
                Console.WriteLine("Upscaler missing.");
                return;
            }

            var args = $"-i \"{AppConfig.ScryfallSource}\" -o \"{AppConfig.UpscaledFolder}\" -n {AppConfig.UpscaleModel} -s {AppConfig.UpscaleScale}";

            var p = System.Diagnostics.Process.Start(exe, args);
            p.WaitForExit();
        }
    }
}
