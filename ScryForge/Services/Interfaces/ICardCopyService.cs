using ScryForge.Models;

namespace ScryForge.Services.Interfaces
{
    public interface ICardCopyService
    {
        ProcessCardsResult ProcessCards(List<CardInfo> cards);
    }
}