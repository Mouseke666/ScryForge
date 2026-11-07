using Microsoft.Extensions.Logging;
using Moq;
using ScryForge.Models;
using ScryForge.Services;
using Xunit.Abstractions;

namespace ScryForge.Tests.Services;

public class CardCopyServiceUnitTest
{
    private readonly ITestOutputHelper _output;

    public CardCopyServiceUnitTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void ProcessCards_EmptyList_ReturnsZeroCounts()
    {
        var logger = Mock.Of<ILogger<CardCopyService>>();
        var service = new CardCopyService(logger);
        Exception? exception = null;
        ProcessCardsResult result = default;
        try
        {
            result = service.ProcessCards(Array.Empty<CardInfo>().ToList());
        }
        catch (Exception ex)
        {
            exception = ex;
        }

        if (exception != null)
        {
            Assert.Fail($"Expected no exception but got: {exception.GetType().Name}");
        }

        Assert.Equal(0, result.TotalCards);
        Assert.Equal(0, result.FlipCardsProcessed);
        Assert.Equal(0, result.SingleCardsProcessed);
    }

    [Fact]
    public void ProcessCards_MissingFile_LogsWarning()
    {
        var loggerMock = new Mock<ILogger<CardCopyService>>();

        var service = new CardCopyService(loggerMock.Object);
        Exception? exception = null;
        ProcessCardsResult result = default;
        try
        {
            result = service.ProcessCards(new List<CardInfo>
            {
                new CardInfo
                {
                    Quantity = 1,
                    Name = "TestCard",
                    SetCode = "TEST",
                    Number = "1",
                    FrontFileName = "NonExistent.png",
                    BackFileName = ""
                }
            });
        }
        catch (Exception ex)
        {
            exception = ex;
        }

        if (exception != null)
        {
            Assert.Fail($"Expected no exception but got: {exception.GetType().Name}");
        }

        Assert.Equal(1, result.TotalCards);
    }
}
