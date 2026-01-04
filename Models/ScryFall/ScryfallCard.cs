using System.Text.Json.Serialization;

namespace ScryForge.Models.Scryfall
{
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
        public string? FrontImagePath { get; set; }
        public string? BackImagePath { get; set; }
        public string? ImagePath
        {
            get => FrontImagePath;
            set => FrontImagePath = value;
        }
    }
}