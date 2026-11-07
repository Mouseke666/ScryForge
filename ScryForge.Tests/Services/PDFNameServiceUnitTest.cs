using Microsoft.Extensions.Logging;
using Moq;
using ScryForge.Models;
using ScryForge.Services;
using Xunit.Abstractions;

namespace ScryForge.Tests.Services;

public class PDFNameServiceUnitTest
{
    private readonly ITestOutputHelper _output;

    public PDFNameServiceUnitTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task DeterminePdfNameAsync_ValidFile_ReturnsSuggestedNameWithTimestamp()
    {
        var parserMock = new Mock<ScryForge.Services.Interfaces.ICardParserService>();
        parserMock.Setup(p => p.GetSuggestedPdfNameAsync(It.IsAny<string>())).Returns(Task.FromResult("MyDeck"));

        var logger = Mock.Of<ILogger<PDFNameService>>();
        var service = new PDFNameService(parserMock.Object, logger);
        var result = await service.DeterminePdfNameAsync(@"C:\cards.txt");

        Assert.Equal("MyDeck", result.BaseNameWithoutTimestamp);
        Assert.NotNull(result.Timestamp);
        Assert.NotEmpty(result.Timestamp);
    }

    [Fact]
    public async Task DeterminePdfNameAsync_InvalidCharsInName_Sanitizes()
    {
        var parserMock = new Mock<ScryForge.Services.Interfaces.ICardParserService>();
        parserMock.Setup(p => p.GetSuggestedPdfNameAsync(It.IsAny<string>())).Returns(Task.FromResult("Card/Name:Test"));

        var logger = Mock.Of<ILogger<PDFNameService>>();
        var service = new PDFNameService(parserMock.Object, logger);
        var result = await service.DeterminePdfNameAsync(@"C:\cards.txt");

        Assert.DoesNotContain("/", result.BaseNameWithoutTimestamp);
        Assert.DoesNotContain(":", result.BaseNameWithoutTimestamp);
    }

    [Fact]
    public async Task DeterminePdfNameAsync_EmptySuggestedName_UsesDefault()
    {
        var parserMock = new Mock<ScryForge.Services.Interfaces.ICardParserService>();
        parserMock.Setup(p => p.GetSuggestedPdfNameAsync(It.IsAny<string>())).Returns(Task.FromResult(""));

        var logger = Mock.Of<ILogger<PDFNameService>>();
        var service = new PDFNameService(parserMock.Object, logger);
        var result = await service.DeterminePdfNameAsync(@"C:\cards.txt");

        Assert.Equal("UntitledDeck", result.BaseNameWithoutTimestamp);
    }

    [Fact]
    public async Task DeterminePdfNameAsync_NullSuggestedName_UsesDefault()
    {
        var parserMock = new Mock<ScryForge.Services.Interfaces.ICardParserService>();
        parserMock.Setup(p => p.GetSuggestedPdfNameAsync(It.IsAny<string>())).Returns(Task.FromResult<string>(null));

        var logger = Mock.Of<ILogger<PDFNameService>>();
        var service = new PDFNameService(parserMock.Object, logger);
        var result = await service.DeterminePdfNameAsync(@"C:\cards.txt");

        Assert.Equal("UntitledDeck", result.BaseNameWithoutTimestamp);
    }

    [Fact]
    public async Task DeterminePdfNameAsync_TimestampFormat_IsCorrect()
    {
        var parserMock = new Mock<ScryForge.Services.Interfaces.ICardParserService>();
        parserMock.Setup(p => p.GetSuggestedPdfNameAsync(It.IsAny<string>())).Returns(Task.FromResult("Test"));

        var logger = Mock.Of<ILogger<PDFNameService>>();
        var service = new PDFNameService(parserMock.Object, logger);
        var result = await service.DeterminePdfNameAsync(@"C:\cards.txt");

        Assert.Contains("-", result.Timestamp);
        Assert.Matches(@"^\d{2}-\d{2}-\d{4}_\d{2}-\d{2}-\d{2}$", result.Timestamp);
    }

    [Fact]
    public async Task DeterminePdfNameAsync_BaseNameIncludesTimestamp()
    {
        var parserMock = new Mock<ScryForge.Services.Interfaces.ICardParserService>();
        parserMock.Setup(p => p.GetSuggestedPdfNameAsync(It.IsAny<string>())).Returns(Task.FromResult("MyDeck"));

        var logger = Mock.Of<ILogger<PDFNameService>>();
        var service = new PDFNameService(parserMock.Object, logger);
        var result = await service.DeterminePdfNameAsync(@"C:\cards.txt");

        Assert.StartsWith("MyDeck_", result.BaseName);
    }
}
