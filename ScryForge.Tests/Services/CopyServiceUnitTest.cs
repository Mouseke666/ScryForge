using Microsoft.Extensions.Logging;
using Moq;
using ScryForge.Models;
using ScryForge.Services;
using Xunit.Abstractions;

namespace ScryForge.Tests.Services;

public class CopyServiceUnitTest
{
    private readonly ITestOutputHelper _output;

    public CopyServiceUnitTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void CopyFilesToRoot_EmptyPath_DoesNotThrow()
    {
        var logger = Mock.Of<ILogger<CopyService>>();
        var service = new CopyService(logger);
        Exception? exception = null;
        try
        {
            service.CopyFilesToRoot("");
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

    [Fact]
    public void CopyFilesToRoot_NullPath_DoesNotThrow()
    {
        var logger = Mock.Of<ILogger<CopyService>>();
        var service = new CopyService(logger);
        Exception? exception = null;
        try
        {
            service.CopyFilesToRoot(null!);
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

    [Fact]
    public void CopyFilesToRoot_WhitespacePath_DoesNotThrow()
    {
        var logger = Mock.Of<ILogger<CopyService>>();
        var service = new CopyService(logger);
        Exception? exception = null;
        try
        {
            service.CopyFilesToRoot("   ");
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

    [Fact]
    public void DuplicateCards_EmptyList_DoesNothing()
    {
        var logger = Mock.Of<ILogger<CopyService>>();
        var service = new CopyService(logger);
        Exception? exception = null;
        try
        {
            service.DuplicateCards(new List<CardInfo>());
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

    [Fact]
    public void CopyFolderFiles_EmptySource_DoesNotThrow()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"ScryForgeTest_{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        var logger = Mock.Of<ILogger<CopyService>>();
        var service = new CopyService(logger);
        Exception? exception = null;
        try
        {
            service.CopyFolderFiles(@"C:\NonExistent", tempDir);
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

    [Fact]
    public void CopyFile_ExistingFile_CopiesIt()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"ScryForgeTest_{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);
        var sourceFile = Path.Combine(tempDir, "source.txt");
        File.WriteAllText(sourceFile, "content");

        var logger = Mock.Of<ILogger<CopyService>>();
        var service = new CopyService(logger);
        Exception? exception = null;
        try
        {
            service.CopyFile(sourceFile, Path.Combine(tempDir, "dest.txt"));
        }
        catch (Exception ex)
        {
            exception = ex;
        }

        if (exception != null)
        {
            Assert.Fail($"Expected no exception but got: {exception.GetType().Name}");
        }

        Assert.True(File.Exists(Path.Combine(tempDir, "dest.txt")));
    }
}
