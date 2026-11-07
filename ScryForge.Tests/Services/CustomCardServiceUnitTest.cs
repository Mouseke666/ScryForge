using Microsoft.Extensions.Logging;
using Moq;
using ScryForge.Models;
using ScryForge.Services;
using Xunit.Abstractions;

namespace ScryForge.Tests.Services;

public class CustomCardServiceUnitTest
{
    private readonly ITestOutputHelper _output;

    public CustomCardServiceUnitTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public async Task FetchCustomCardsAsync_EmptyDirectory_ReturnsEmpty()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"ScryForgeTest_{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        var logger = Mock.Of<ILogger<CustomCardService>>();
        var service = new CustomCardService(logger);
        var result = await service.FetchCustomCardsAsync(tempDir);

        Assert.Empty(result);
    }

    [Fact]
    public async Task FetchCustomCardsAsync_WithImages_ReturnsCustomCards()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"ScryForgeTest_{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);
        File.WriteAllText(Path.Combine(tempDir, "Card1.png"), "content");

        var logger = Mock.Of<ILogger<CustomCardService>>();
        var service = new CustomCardService(logger);
        var result = await service.FetchCustomCardsAsync(tempDir);

        Assert.Single(result);
    }

    [Fact]
    public async Task FetchCustomCardsAsync_FlipCard_ReturnsFrontAndBack()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"ScryForgeTest_{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);
        File.WriteAllText(Path.Combine(tempDir, "Flip.png"), "front");
        File.WriteAllText(Path.Combine(tempDir, "__back_Flip.png"), "back");

        var logger = Mock.Of<ILogger<CustomCardService>>();
        var service = new CustomCardService(logger);
        var result = await service.FetchCustomCardsAsync(tempDir);

        Assert.Single(result);
        Assert.NotNull(result[0].BackLocation);
    }

    [Fact]
    public async Task FetchCustomCardsAsync_MultipleImages_ReturnsMultiple()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"ScryForgeTest_{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);
        File.WriteAllText(Path.Combine(tempDir, "Card1.png"), "content");
        File.WriteAllText(Path.Combine(tempDir, "Card2.jpg"), "content");

        var logger = Mock.Of<ILogger<CustomCardService>>();
        var service = new CustomCardService(logger);
        var result = await service.FetchCustomCardsAsync(tempDir);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task FetchCustomCardsAsync_NullOrEmptyPath_ReturnsEmpty()
    {
        var logger = Mock.Of<ILogger<CustomCardService>>();
        var service = new CustomCardService(logger);
        var result = await service.FetchCustomCardsAsync("");

        Assert.Empty(result);
    }
}
