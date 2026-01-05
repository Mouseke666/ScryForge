namespace ScryForge.Services.Interfaces
{
    public interface ICleanupService
    {
        Task CleanDirectoryAsync(string path, string? excludeSubfolder = null, CancellationToken ct = default);
    }
}