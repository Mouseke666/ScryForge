using ScryForge.Models;

namespace ScryForge.Services.Intefaces
{
    public interface IFlipService
    {
        void ProcessFlipCards(List<CardInfo> cards);
    }
}