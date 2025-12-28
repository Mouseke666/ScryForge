namespace ScryForge.Models
{
    public record EmptySlotsResult(bool ShouldStopPipeline, int EmptySlotsDefault, int EmptySlotsFlips, bool HasEmptySlots);
}