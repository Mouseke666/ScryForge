
namespace ScryForge.Models
{
    public class CardInfo
    {
        public int Quantity { get; set; }
        public required string Name { get; set; }
        public required string SetCode { get; set; }
        public required string Number { get; set; }
        public required string FrontFileName { get; set; }
        public required string BackFileName { get; set; }
        public bool IsFlip =>
            !string.IsNullOrEmpty(FrontFileName) &&
            !string.IsNullOrEmpty(BackFileName);
    }
}
