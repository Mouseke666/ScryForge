using Microsoft.Extensions.Logging;
using Moq;
using ScryForge.Models;
using ScryForge.Models.Scryfall;
using ScryForge.Services;
using Xunit.Abstractions;

namespace ScryForge.Tests.Services;

public class EmptySlotsServiceUnitTest
{
    private readonly ITestOutputHelper _output;

    public EmptySlotsServiceUnitTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task AnalyAsync_NoCardsAndNoCustomCards_ReturnsNoEmptySlots()
    {
        var pdfMock = new Mock<ScryForge.Services.Interfaces.IPDFService>();
        var logger = Mock.Of<ILogger<EmptySlotsService>>();
        var service = new EmptySlotsService(pdfMock.Object, logger);

        var result = await service.AnalyzeAsync(Array.Empty<ScryfallCard>(), Array.Empty<CustomCard>(), CancellationToken.None);

        Assert.False(result.HasEmptySlots);
        Assert.Equal(0, result.EmptySlotsDefault);
        Assert.Equal(0, result.EmptySlotsFlips);
    }

    [Fact]
    public async Task AnalyAsync_NullCardsAndNullCustomCards_ReturnsNoEmptySlots()
    {
        var pdfMock = new Mock<ScryForge.Services.Interfaces.IPDFService>();
        var logger = Mock.Of<ILogger<EmptySlotsService>>();
        var service = new EmptySlotsService(pdfMock.Object, logger);

        var result = await service.AnalyzeAsync(null!, null!, CancellationToken.None);

        Assert.False(result.HasEmptySlots);
        Assert.Equal(0, result.EmptySlotsDefault);
        Assert.Equal(0, result.EmptySlotsFlips);
    }

    [Fact]
    public async Task AnalyAsync_FullPage_Default_ReturnsNoEmptySlots()
    {
        var pdfMock = new Mock<ScryForge.Services.Interfaces.IPDFService>();
        pdfMock.Setup(p => p.GetMaxCardsPerPage(It.IsAny<string>())).Returns(Task.FromResult(9));

        var cards = new List<ScryfallCard>
        {
            CreateScryfallCard(null, quantity: 9)
        };

        var logger = Mock.Of<ILogger<EmptySlotsService>>();
        var service = new EmptySlotsService(pdfMock.Object, logger);
        var result = await service.AnalyzeAsync(cards, Array.Empty<CustomCard>(), CancellationToken.None);

        Assert.False(result.HasEmptySlots);
        Assert.Equal(0, result.EmptySlotsDefault);
    }

    [Fact]
    public async Task AnalyAsync_PartialPage_Default_ReturnsEmptySlots()
    {
        var pdfMock = new Mock<ScryForge.Services.Interfaces.IPDFService>();
        pdfMock.Setup(p => p.GetMaxCardsPerPage(It.IsAny<string>())).Returns(Task.FromResult(9));

        var cards = new List<ScryfallCard>
        {
            CreateScryfallCard(null, quantity: 5)
        };

        var logger = Mock.Of<ILogger<EmptySlotsService>>();
        var service = new EmptySlotsService(pdfMock.Object, logger);
        var result = await service.AnalyzeAsync(cards, Array.Empty<CustomCard>(), CancellationToken.None);

        Assert.True(result.HasEmptySlots);
        Assert.Equal(4, result.EmptySlotsDefault);
    }

    [Fact]
    public async Task AnalyAsync_CustomCards_CountedAsDefault()
    {
        var pdfMock = new Mock<ScryForge.Services.Interfaces.IPDFService>();
        pdfMock.Setup(p => p.GetMaxCardsPerPage(It.IsAny<string>())).Returns(Task.FromResult(9));

        var customCards = new List<CustomCard>
        {
            new CustomCard(),
            new CustomCard()
        };

        var logger = Mock.Of<ILogger<EmptySlotsService>>();
        var service = new EmptySlotsService(pdfMock.Object, logger);
        var result = await service.AnalyzeAsync(Array.Empty<ScryfallCard>(), customCards, CancellationToken.None);

        Assert.True(result.HasEmptySlots);
        Assert.Equal(7, result.EmptySlotsDefault);
    }

    [Fact]
    public async Task AnalyAsync_FlipCards_UseFlipsMax()
    {
        var pdfMock = new Mock<ScryForge.Services.Interfaces.IPDFService>();
        pdfMock.Setup(p => p.GetMaxCardsPerPage("default.json")).Returns(Task.FromResult(9));
        pdfMock.Setup(p => p.GetMaxCardsPerPage("flips.json")).Returns(Task.FromResult(8));

        var cards = new List<ScryfallCard>
        {
            CreateScryfallCard("flip", quantity: 3)
        };

        var logger = Mock.Of<ILogger<EmptySlotsService>>();
        var service = new EmptySlotsService(pdfMock.Object, logger);
        var result = await service.AnalyzeAsync(cards, Array.Empty<CustomCard>(), CancellationToken.None);

        Assert.True(result.HasEmptySlots);
        Assert.Equal(5, result.EmptySlotsFlips);
    }

    [Fact]
    public async Task AnalyAsync_FlipCards_FullPage_ReturnsNoEmptySlots()
    {
        var pdfMock = new Mock<ScryForge.Services.Interfaces.IPDFService>();
        pdfMock.Setup(p => p.GetMaxCardsPerPage("default.json")).Returns(Task.FromResult(9));
        pdfMock.Setup(p => p.GetMaxCardsPerPage("flips.json")).Returns(Task.FromResult(8));

        var cards = new List<ScryfallCard>
        {
            CreateScryfallCard("flip", quantity: 8)
        };

        var logger = Mock.Of<ILogger<EmptySlotsService>>();
        var service = new EmptySlotsService(pdfMock.Object, logger);
        var result = await service.AnalyzeAsync(cards, Array.Empty<CustomCard>(), CancellationToken.None);

        Assert.False(result.HasEmptySlots);
        Assert.Equal(0, result.EmptySlotsFlips);
    }

    [Fact]
    public async Task AnalyAsync_MixedCardsAndCustom_ReturnsCorrectCounts()
    {
        var pdfMock = new Mock<ScryForge.Services.Interfaces.IPDFService>();
        pdfMock.Setup(p => p.GetMaxCardsPerPage("default.json")).Returns(Task.FromResult(9));
        pdfMock.Setup(p => p.GetMaxCardsPerPage("flips.json")).Returns(Task.FromResult(8));

        var cards = new List<ScryfallCard>
        {
            CreateScryfallCard(null, quantity: 5),
            CreateScryfallCard("flip", quantity: 3)
        };

        var customCards = new List<CustomCard>
        {
            new CustomCard()
        };

        var logger = Mock.Of<ILogger<EmptySlotsService>>();
        var service = new EmptySlotsService(pdfMock.Object, logger);
        var result = await service.AnalyzeAsync(cards, customCards, CancellationToken.None);

        Assert.True(result.HasEmptySlots);
        Assert.Equal(3, result.EmptySlotsDefault);
        Assert.Equal(5, result.EmptySlotsFlips);
    }

    [Fact]
    public async Task AnalyAsync_DefaultMaxIsZero_UsesFallbackOf9()
    {
        var pdfMock = new Mock<ScryForge.Services.Interfaces.IPDFService>();
        pdfMock.Setup(p => p.GetMaxCardsPerPage(It.IsAny<string>())).Returns(Task.FromResult(0));

        var cards = new List<ScryfallCard>
        {
            CreateScryfallCard(null, quantity: 1)
        };

        var logger = Mock.Of<ILogger<EmptySlotsService>>();
        var service = new EmptySlotsService(pdfMock.Object, logger);
        var result = await service.AnalyzeAsync(cards, Array.Empty<CustomCard>(), CancellationToken.None);

        Assert.True(result.HasEmptySlots);
        Assert.Equal(8, result.EmptySlotsDefault);
    }

    [Fact]
    public async Task AnalyAsync_FlipsMaxIsZero_UsesFallbackOf8()
    {
        var pdfMock = new Mock<ScryForge.Services.Interfaces.IPDFService>();
        pdfMock.Setup(p => p.GetMaxCardsPerPage("default.json")).Returns(Task.FromResult(9));
        pdfMock.Setup(p => p.GetMaxCardsPerPage("flips.json")).Returns(Task.FromResult(0));

        var cards = new List<ScryfallCard>
        {
            CreateScryfallCard("flip", quantity: 1)
        };

        var logger = Mock.Of<ILogger<EmptySlotsService>>();
        var service = new EmptySlotsService(pdfMock.Object, logger);
        var result = await service.AnalyzeAsync(cards, Array.Empty<CustomCard>(), CancellationToken.None);

        Assert.True(result.HasEmptySlots);
        Assert.Equal(7, result.EmptySlotsFlips);
    }

    private static ScryfallCard CreateScryfallCard(string? layout, int quantity)
    {
        return new ScryfallCard("Test", "test", "TEST", layout, null, null)
        {
            Quantity = quantity
        };
    }
}
