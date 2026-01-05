namespace ScryForge.Models.Scryfall
{
    public record CardRequest
    (
        string Name,
        string? SetCode = null,
        string? CollectorNumber = null
    );
}