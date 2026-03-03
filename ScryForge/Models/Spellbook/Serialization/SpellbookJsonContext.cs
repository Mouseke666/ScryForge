using System.Text.Json.Serialization;

namespace ScryForge.Models.Spellbook.Serialization;

[JsonSerializable(typeof(EstimateBracketRequest))]
public partial class SpellbookJsonContext : JsonSerializerContext
{
}