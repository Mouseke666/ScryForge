
using System.Text.Json.Serialization;

namespace ScryForge.Models
{
    record CardRequest(string Name, string? SetCode = null, string? CollectorNumber = null);

    record ImageUris
    (
        [property: JsonPropertyName("png")] string? Png,
        [property: JsonPropertyName("normal")] string? Normal,
        [property: JsonPropertyName("large")] string? Large,
        [property: JsonPropertyName("small")] string? Small = null,
        [property: JsonPropertyName("art_crop")] string? ArtCrop = null,
        [property: JsonPropertyName("border_crop")] string? BorderCrop = null
    );

    record ScryfallCard
    (
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("set")] string Set,
        [property: JsonPropertyName("collector_number")] string CollectorNumber,
        [property: JsonPropertyName("layout")] string? Layout,                 // ← HIER!
        [property: JsonPropertyName("image_uris")] ImageUris? ImageUris,
        [property: JsonPropertyName("card_faces")] List<CardFace>? CardFaces
    );

    record CardFace
    (
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("image_uris")] ImageUris? ImageUris
    );
}