using FluentAssertions;
using Refit;
using Refitter.Tests.AdditionalFiles.MultipleSources;
using TUnit.Core;

namespace Refitter.SourceGenerators.Tests;

public class MultipleOpenApiPathsGeneratorTests
{
    [Test]
    public void Should_Generate_Interface_From_Relative_OpenApiPaths()
    {
        typeof(ISwaggerPetstoreMultipleSources).Namespace.Should().Be("Refitter.Tests.AdditionalFiles.MultipleSources");
    }

    [Test]
    public void Generated_Interface_From_Relative_OpenApiPaths_Should_Have_Refit_Attributes()
    {
        var hasRefitAttributes = typeof(ISwaggerPetstoreMultipleSources)
            .GetMethods()
            .SelectMany(m => m.GetCustomAttributes(inherit: false))
            .Any(a => a is HttpMethodAttribute);

        hasRefitAttributes.Should().BeTrue();
    }
}
