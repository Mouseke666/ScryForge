namespace ScryForge.Models.Spellbook;

public sealed class EstimateBracketRequest
{
    public List<CardEntry> Main { get; init; } = new();
    public List<CardEntry> Commanders { get; init; } = new();
}
