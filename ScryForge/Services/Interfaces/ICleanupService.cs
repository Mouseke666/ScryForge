namespace ScryForge.Services.Interfaces
{
    public interface ICleanupService
    {
        Task<bool> CleanDirectoryAsync(string path, string? excludeSubfolder = null, CancellationToken ct = default);
    }
}