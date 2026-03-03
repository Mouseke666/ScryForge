
using System.Text.Json.Serialization;

namespace ScryForge.Models.Spellbook;

public sealed class EstimateBracketRequest
{
    [JsonPropertyName("main")]
    public List<CardEntry> Main { get; init; } = new();

    [JsonPropertyName("commanders")]
    public List<CardEntry> Commanders { get; init; } = new();
}
