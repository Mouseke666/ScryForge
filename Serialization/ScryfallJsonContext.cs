using ScryForge.Models;
using System.Text.Json.Serialization;

namespace ScryForge.Serialization
{
    [JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
    [JsonSerializable(typeof(ScryfallCard))]
    [JsonSerializable(typeof(List<ScryfallCard>))]
    internal partial class ScryfallJsonContext : JsonSerializerContext
    {
    }
}
