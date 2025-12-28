namespace ScryForge.Services
{
    public interface ICleanupService
    {
        Task CleanDirectoryAsync(string path, string? excludeSubfolder = null, CancellationToken ct = default);
    }
}