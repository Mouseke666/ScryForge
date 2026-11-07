using Microsoft.Extensions.Logging;
using Moq;
using ScryForge.Models;
using ScryForge.Services;
using Xunit.Abstractions;

namespace ScryForge.Tests.Services;

public class CardParserServiceUnitTest
{
    private readonly ITestOutputHelper _output;

    public CardParserServiceUnitTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task ParseCardsAsync_FileNotFound_ReturnsEmpty()
    {
        var logger = Mock.Of<ILogger<CardParserService>>();
        var service = new CardParserService(logger);
        var result = await service.ParseCardsAsync(@"C:\NonExistent\cards.txt");
        Assert.Empty(result);
    }

    [Fact]
    public async Task GetSuggestedPdfNameAsync_FileNotFound_ReturnsDefault()
    {
        var logger = Mock.Of<ILogger<CardParserService>>();
        var service = new CardParserService(logger);
        var result = await service.GetSuggestedPdfNameAsync(@"C:\NonExistent\cards.txt");
        Assert.Equal("cards", result);
    }

    [Fact]
    public async Task GetSuggestedPdfNameAsync_EmptyFile_ReturnsDefault()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"ScryForgeTest_{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);
        var cardsFile = Path.Combine(tempDir, "cards.txt");
        File.WriteAllText(cardsFile, "");

        var logger = Mock.Of<ILogger<CardParserService>>();
        var service = new CardParserService(logger);
        var result = await service.GetSuggestedPdfNameAsync(cardsFile);
        Assert.Equal("cards", result);
    }

    [Fact]
    public async Task GetSuggestedPdfNameAsync_WithContent_ReturnsCardName()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"ScryForgeTest_{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);
        var cardsFile = Path.Combine(tempDir, "cards.txt");
        File.WriteAllText(cardsFile, "3 Lightning Bolt (GRW) 17b\nIsland x2");

        var logger = Mock.Of<ILogger<CardParserService>>();
        var service = new CardParserService(logger);
        var result = await service.GetSuggestedPdfNameAsync(cardsFile);
        Assert.Equal("Lightning Bolt", result);
    }

    [Fact]
    public async Task GetSuggestedPdfNameAsync_InvalidChars_Replaced()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"ScryForgeTest_{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);
        var cardsFile = Path.Combine(tempDir, "cards.txt");
        File.WriteAllText(cardsFile, "Card/Name x2");

        var logger = Mock.Of<ILogger<CardParserService>>();
        var service = new CardParserService(logger);
        var result = await service.GetSuggestedPdfNameAsync(cardsFile);
        Assert.Contains("_", result);
    }

    [Fact]
    public async Task ParseCustomCardsAsync_EmptyList_ReturnsEmpty()
    {
        var logger = Mock.Of<ILogger<CardParserService>>();
        var service = new CardParserService(logger);
        var result = await service.ParseCustomCardsAsync(Array.Empty<ScryForge.Models.CustomCard>());
        Assert.Empty(result);
    }

    [Fact]
    public async Task ParseCustomCardsAsync_NullList_ReturnsEmpty()
    {
        var logger = Mock.Of<ILogger<CardParserService>>();
        var service = new CardParserService(logger);
        var result = await service.ParseCustomCardsAsync(null!);
        Assert.Empty(result);
    }

    [Fact]
    public async Task ParseCustomCardsAsync_ValidCard_CreatesCardInfo()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"ScryForgeTest_{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);
        File.WriteAllText(Path.Combine(tempDir, "MyCard.png"), "content");

        var customCards = new List<ScryForge.Models.CustomCard>
        {
            new ScryForge.Models.CustomCard { FrontLocation = Path.Combine(tempDir, "MyCard.png") }
        };

        var logger = Mock.Of<ILogger<CardParserService>>();
        var service = new CardParserService(logger);
        var result = await service.ParseCustomCardsAsync(customCards);

        Assert.Single(result);
        Assert.Equal("MyCard", result[0].Name);
        Assert.Equal("CUSTOM", result[0].SetCode);
        Assert.Equal("0", result[0].Number);
        Assert.Equal(1, result[0].Quantity);
    }

    [Fact]
    public async Task ParseCustomCardsAsync_FlipCard_CreatesFrontAndBack()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"ScryForgeTest_{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);
        File.WriteAllText(Path.Combine(tempDir, "MyCard.png"), "front");
        File.WriteAllText(Path.Combine(tempDir, "__back_MyCard.png"), "back");

        var customCards = new List<ScryForge.Models.CustomCard>
        {
            new ScryForge.Models.CustomCard
            {
                FrontLocation = Path.Combine(tempDir, "MyCard.png"),
                BackLocation = Path.Combine(tempDir, "__back_MyCard.png")
            }
        };

        var logger = Mock.Of<ILogger<CardParserService>>();
        var service = new CardParserService(logger);
        var result = await service.ParseCustomCardsAsync(customCards);

        Assert.Single(result);
        Assert.NotEmpty(result[0].FrontFileName);
        Assert.NotEmpty(result[0].BackFileName);
    }

    [Fact]
    public async Task ParseCustomCardsAsync_MissingFrontFile_Skipped()
    {
        var customCards = new List<ScryForge.Models.CustomCard>
        {
            new ScryForge.Models.CustomCard { FrontLocation = @"C:\NonExistent\Card.png" }
        };

        var logger = Mock.Of<ILogger<CardParserService>>();
        var service = new CardParserService(logger);
        var result = await service.ParseCustomCardsAsync(customCards);

        Assert.Empty(result);
    }
}
