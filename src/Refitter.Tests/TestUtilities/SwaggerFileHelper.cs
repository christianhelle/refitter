using System.IO;
using System.Threading.Tasks;

namespace Refitter.Tests.TestUtilities;

/// <summary>
/// Helper utilities for creating temporary Swagger/OpenAPI files in tests
/// </summary>
public static class SwaggerFileHelper
{
    /// <summary>
    /// Creates a temporary YAML file with the specified OpenAPI content
    /// </summary>
    /// <param name="contents">The OpenAPI specification content</param>
    /// <returns>The path to the created temporary file</returns>
    public static Task<string> CreateSwaggerFile(string contents) =>
        CreateSwaggerFileWithExtension(contents, ".yml");

    /// <summary>
    /// Creates a temporary JSON file with the specified OpenAPI content
    /// </summary>
    /// <param name="contents">The OpenAPI specification content</param>
    /// <returns>The path to the created temporary file</returns>
    public static Task<string> CreateSwaggerJsonFile(string contents) =>
        CreateSwaggerFileWithExtension(contents, ".json");

    private static async Task<string> CreateSwaggerFileWithExtension(string contents, string extension)
    {
        var filename = $"{Guid.NewGuid()}{extension}";
        var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(folder);
        var swaggerFile = Path.Combine(folder, filename);
        await File.WriteAllTextAsync(swaggerFile, contents);
        return swaggerFile;
    }
}
