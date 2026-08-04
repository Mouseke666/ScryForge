namespace ScryForge.Services.Interfaces
{
    public interface ICornerFillService
    {
        Task FillRoundedCornersAsync(
            string inputFolder,
            string outputFolder);


        Task FillRoundedCornersAsync(
            string inputFolder,
            string outputFolder,
            double radiusMm,
            int whiteThreshold,
            int alphaThreshold,
            double sampleInset,
            double overpaintMm,
            bool force);
    }
}