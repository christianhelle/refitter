using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Resources;
using Xunit;

namespace Refitter.Tests;

public class RefitGeneratorTests
{
    [Fact]
    public async Task Generate()
    {
        var contents = EmbeddedResources.SwaggerPetstoreJsonV3;
        var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(folder);
        var swaggerFile = Path.Combine(folder, "Swagger.json");
        await File.WriteAllTextAsync(swaggerFile, contents);

        var generator = new RefitGenerator();
        var result = await generator.Generate(swaggerFile);
        result.Should().NotBeNullOrWhiteSpace();
    }
}