
namespace MTGArtDownloader.Models
{
    public class CardInfo
    {
        public int Quantity { get; set; }
        public string Name { get; set; }
        public string SetCode { get; set; }
        public string Number { get; set; }
        public string FrontFileName { get; set; }
        public string BackFileName { get; set; }
        public bool IsFlip =>
            !string.IsNullOrEmpty(FrontFileName) &&
            !string.IsNullOrEmpty(BackFileName);
    }
}
