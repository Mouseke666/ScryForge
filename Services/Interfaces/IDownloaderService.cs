namespace ScryForge.Services
{
    public interface IDownloaderService
    {
        Task<bool> DownloadArtAsync();
    }

}