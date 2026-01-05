namespace ScryForge.Models
{

    public class CustomCard
    {
        public string? FrontLocation { get; set; }
        public string? BackLocation { get; set; }
        public bool IsFlip
        {
            get
            {
                return !string.IsNullOrEmpty(FrontLocation) && !string.IsNullOrEmpty(BackLocation);
            }
        }

    }
}