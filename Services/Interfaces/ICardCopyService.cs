using ScryForge.Models;

namespace ScryForge.Services.Intefaces
{
    public interface ICardCopyService
    {
        void ProcessCards(List<CardInfo> cards);
    }
}