using System.Diagnostics;

namespace ScryForge.Services
{
    public class PDFOpenService
    {
        public void OpenPdf(string pdfPath)
        {
            if (string.IsNullOrWhiteSpace(pdfPath))
            {
                throw new ArgumentException("The PDF path cannot be empty.", nameof(pdfPath));
            }

            if (!File.Exists(pdfPath))
            {
                throw new FileNotFoundException("PDF file not found.", pdfPath);
            }

            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = pdfPath,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Could not open PDF: {ex.Message}");
            }
        }
    }
}
