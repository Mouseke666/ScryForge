using ScryForge.Models;
using ScryForge.Models.Scryfall;

namespace ScryForge.Steps;

public class PipelineContext
{
    public List<ScryfallCard> ScryfallCards { get; set; } = [];
    public List<CustomCard> CustomCards { get; set; } = [];
    public List<CardInfo> Cards { get; set; } = [];
    public double TotalElapsedSeconds { get; set; } = 0;


    public List<string> DeckListLines
    {
        get
        {
            return ScryfallCards.Select(c => c.Name).ToList();
        }
    }

    public List<ScryfallCard> CardsWithoutReleaseDate
    {
        get
        {
            return ScryfallCards.Where(c => !c.ReleasedAt.HasValue).ToList();
        }
    }

    public string PdfName { get; set; } = string.Empty;
    public string? FullName { get; internal set; }
}