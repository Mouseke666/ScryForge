using System.Diagnostics;
using Microsoft.Extensions.Logging;
using ScryForge.Services.Intefaces;

namespace ScryForge.Services
{
    public class PDFOpenService(ILogger<PDFOpenService> logger) : IPDFOpenService
    {
        private readonly ILogger<PDFOpenService> _logger = logger;

        public void OpenPdf(string pdfPath)
        {
            if (string.IsNullOrWhiteSpace(pdfPath))
            {
                _logger.LogError("The PDF path cannot be empty.");
                throw new ArgumentException("The PDF path cannot be empty.", nameof(pdfPath));
            }

            if (!File.Exists(pdfPath))
            {
                _logger.LogError("PDF file not found: {PdfPath}", pdfPath);
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
                _logger.LogError(ex, "Could not open PDF: {PdfPath}", pdfPath);
            }
        }
    }
}