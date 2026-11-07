using Microsoft.Extensions.Logging;
using Moq;
using ScryForge.Services;
using Xunit.Abstractions;

namespace ScryForge.Tests.Services;

public class PDFOpenServiceUnitTest
{
    private readonly ITestOutputHelper _output;

    public PDFOpenServiceUnitTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void OpenPdf_EmptyPath_ThrowsArgumentException()
    {
        var logger = Mock.Of<ILogger<PDFOpenService>>();
        var service = new PDFOpenService(logger);
        Assert.Throws<ArgumentException>(() => service.OpenPdf(""));
    }

    [Fact]
    public void OpenPdf_NullPath_ThrowsArgumentException()
    {
        var logger = Mock.Of<ILogger<PDFOpenService>>();
        var service = new PDFOpenService(logger);
        Assert.Throws<ArgumentException>(() => service.OpenPdf(null!));
    }

    [Fact]
    public void OpenPdf_WhitespacePath_ThrowsArgumentException()
    {
        var logger = Mock.Of<ILogger<PDFOpenService>>();
        var service = new PDFOpenService(logger);
        Assert.Throws<ArgumentException>(() => service.OpenPdf("   "));
    }

    [Fact]
    public void OpenPdf_NonExistentFile_ThrowsFileNotFoundException()
    {
        var logger = Mock.Of<ILogger<PDFOpenService>>();
        var service = new PDFOpenService(logger);
        Assert.Throws<FileNotFoundException>(() => service.OpenPdf(@"C:\NonExistent\file.pdf"));
    }

    [Fact]
    public void OpenPdf_ValidFile_DoesNotThrow()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"ScryForgeTest_{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);
        var pdfPath = Path.Combine(tempDir, "test.pdf");
        File.WriteAllText(pdfPath, "%PDF-1.4 fake content");

        try
        {
            var logger = Mock.Of<ILogger<PDFOpenService>>();
            var service = new PDFOpenService(logger);
            Exception? exception = null;
            try
            {
                service.OpenPdf(pdfPath);
            }
            catch (Exception ex)
            {
                exception = ex;
            }

            if (exception != null)
            {
                Assert.Fail($"Expected no exception but got: {exception.GetType().Name}");
            }
        }
        finally
        {
            File.Delete(pdfPath);
            Directory.Delete(tempDir);
        }
    }
}
