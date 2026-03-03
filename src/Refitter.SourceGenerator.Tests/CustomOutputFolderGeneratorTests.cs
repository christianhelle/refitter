using FluentAssertions;
using Refit;
using Refitter.Tests.CustomGenerated;
using TUnit.Core;

namespace Refitter.SourceGenerators.Tests;

public class CustomOutputFolderGeneratorTests
{
    [Test]
    public void Should_Generate_Interface_Type() =>
        typeof(IApiInCustomGeneratedFolder)
            .Namespace
            .Should()
            .Be("Refitter.Tests.CustomGenerated");

    [Test]
    public void Can_Resolve_Refit_Interface() =>
        RestService.For<IApiInCustomGeneratedFolder>("https://petstore3.swagger.io/api/v3")
            .Should()
            .NotBeNull();
}
