using ScryForge.Models;

namespace ScryForge.Services.Interfaces
{
    public interface ICardCopyService
    {
        void ProcessCards(List<CardInfo> cards);
    }
}