using System.Text.Json.Serialization;

namespace ScryForge.Models.Spellbook;

public sealed class CardEntry
{
    [JsonPropertyName("card")]
    public string Card { get; init; } = string.Empty;

    [JsonPropertyName("quantity")]
    public int Quantity { get; init; }
}
