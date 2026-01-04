using ScryForge.Models;

namespace ScryForge.Services.Interfaces
{
    public interface ICustomCardService
    {
        Task CopyCustomCardsAsync(IReadOnlyList<CustomCard> customCards, string targetFolder);
        Task<IReadOnlyList<CustomCard>> FetchCustomCardsAsync(string customFolder);
    }
}