using System.Text.Json.Serialization;

namespace ScryForge.Models.Scryfall
{
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