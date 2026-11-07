using Microsoft.Extensions.Logging;
using Moq;
using ScryForge.Services;
using Xunit.Abstractions;

namespace ScryForge.Tests.Services;

public class OpenFolderServiceUnitTest
{
    private readonly ITestOutputHelper _output;

    public OpenFolderServiceUnitTest(ITestOutputHelper output)
    {
        _output = output;
    }

    [Fact]
    public void OpenFolder_NonExistentPath_DoesNotThrow()
    {
        var logger = Mock.Of<ILogger<OpenFolderService>>();
        var service = new OpenFolderService(logger);
        try
        {
            service.OpenFolder(@"C:\NonExistent");
        }
        catch (Exception ex)
        {
            Assert.Fail($"Expected no exception but got: {ex.GetType().Name}");
        }
    }

    [Fact]
    public void OpenFolder_NullPath_UsesBasePath()
    {
        var logger = Mock.Of<ILogger<OpenFolderService>>();
        var service = new OpenFolderService(logger);
        try
        {
            service.OpenFolder(null!);
        }
        catch (Exception ex)
        {
            Assert.Fail($"Expected no exception but got: {ex.GetType().Name}");
        }
    }

    [Fact]
    public void OpenFolder_EmptyPath_UsesBasePath()
    {
        var logger = Mock.Of<ILogger<OpenFolderService>>();
        var service = new OpenFolderService(logger);
        try
        {
            service.OpenFolder("");
        }
        catch (Exception ex)
        {
            Assert.Fail($"Expected no exception but got: {ex.GetType().Name}");
        }
    }

    [Fact]
    public void OpenFolder_ValidExistingPath_DoesNotThrow()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"ScryForgeTest_{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        try
        {
            var logger = Mock.Of<ILogger<OpenFolderService>>();
            var service = new OpenFolderService(logger);
            try
            {
                service.OpenFolder(tempDir);
            }
            catch (Exception ex)
            {
                Assert.Fail($"Expected no exception but got: {ex.GetType().Name}");
            }
        }
        finally
        {
            Directory.Delete(tempDir);
        }
    }
}
