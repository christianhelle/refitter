using Refitter.Tests.TestUtilities;

namespace Refitter.Tests;

public static class TestFile
{
    public static async Task<string> CreateSwaggerFile(string contents, string filename)
    {
        var folder = TestDirectory.CreateFolder();
        var swaggerFile = Path.Combine(folder, filename);
        await File.WriteAllTextAsync(swaggerFile, contents);
        return swaggerFile;
    }
}
