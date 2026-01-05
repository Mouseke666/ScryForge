using System.Text.Json.Serialization;

namespace ScryForge.Models.Scryfall
{
    public record CardFace
    (
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("image_uris")] ImageUris? ImageUris
    );
}