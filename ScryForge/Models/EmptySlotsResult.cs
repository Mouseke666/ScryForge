namespace ScryForge.Models
{
    public record EmptySlotsResult(
        bool HasEmptySlots,
        int EmptySlotsDefault,
        int EmptySlotsFlips
    );
}