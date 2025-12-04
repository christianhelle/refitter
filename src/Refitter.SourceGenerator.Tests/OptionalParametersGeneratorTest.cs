using FluentAssertions;
using Refit;
using Refitter.Tests.AdditionalFiles.OptionalParameters;
using TUnit.Core;

namespace Refitter.SourceGenerators.Tests;

public class OperationalParametersGeneratorTest
{
    [Test]
    public void Should_Type_Exist() =>
        typeof(ISwaggerPetstoreWithOptionalParameters)
            .Namespace
            .Should()
            .Be("Refitter.Tests.AdditionalFiles.OptionalParameters");

    [Test]
    public void Can_Resolve_Refit_Interface() =>
        RestService.For<ISwaggerPetstoreWithOptionalParameters>("https://petstore3.swagger.io/api/v3")
            .Should()
            .NotBeNull();
}
