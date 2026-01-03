using System.Text.Json.Serialization;

namespace ScryForge.Models
{
    public record CardRequest(
        string Name,
        string? SetCode = null,
        string? CollectorNumber = null
    );

    public record ImageUris
    (
        [property: JsonPropertyName("png")] string? Png,
        [property: JsonPropertyName("normal")] string? Normal,
        [property: JsonPropertyName("large")] string? Large,
        [property: JsonPropertyName("small")] string? Small = null,
        [property: JsonPropertyName("art_crop")] string? ArtCrop = null,
        [property: JsonPropertyName("border_crop")] string? BorderCrop = null
    );

    public record ScryfallCard
    (
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("set")] string Set,
        [property: JsonPropertyName("collector_number")] string CollectorNumber,
        [property: JsonPropertyName("layout")] string? Layout,
        [property: JsonPropertyName("image_uris")] ImageUris? ImageUris,
        [property: JsonPropertyName("card_faces")] List<CardFace>? CardFaces
    )
    {
        public bool IsDoubleFaced => Layout is "flip" or "transform" or "modal_dfc";
        public int Quantity { get; set; } = 1;

        // Nieuw: front/back voor flipcards
        public string? FrontImagePath { get; set; }
        public string? BackImagePath { get; set; }

        // Voor backward compatibiliteit
        public string? ImagePath
        {
            get => FrontImagePath; // normale kaarten gebruiken FrontImagePath
            set => FrontImagePath = value;
        }
    }

    public record CardFace
    (
        [property: JsonPropertyName("name")] string Name,
        [property: JsonPropertyName("image_uris")] ImageUris? ImageUris
    );

    public class ScryfallCardList
    {
        [JsonPropertyName("object")]
        public string Object { get; set; } = null!;

        [JsonPropertyName("total_cards")]
        public int TotalCards { get; set; }

        [JsonPropertyName("has_more")]
        public bool HasMore { get; set; }

        [JsonPropertyName("data")]
        public List<ScryfallCard> Data { get; set; } = new();
    }
}