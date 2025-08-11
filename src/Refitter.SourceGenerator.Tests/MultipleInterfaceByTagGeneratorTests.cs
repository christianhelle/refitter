using FluentAssertions;
using Refit;
using Refitter.Tests.AdditionalFiles.ByTag;
using Xunit;

namespace Refitter.SourceGenerators.Tests;

public class MultipleInterfaceByTagGeneratorTests
{
    [Theory]
    [InlineData(typeof(IPetApi))]
    [InlineData(typeof(IUserApi))]
    [InlineData(typeof(IStoreApi))]
    public void Should_Generate_Interface(Type type) =>
        type
            .Namespace
            .Should()
            .Be("Refitter.Tests.AdditionalFiles.ByTag");

    [Theory]
    [InlineData(typeof(IPetApi))]
    [InlineData(typeof(IUserApi))]
    [InlineData(typeof(IStoreApi))]
    public void Can_Resolve_Refit_Interface(Type type) =>
        RestService.For(type, "https://petstore3.swagger.io/api/v3")
            .Should()
            .NotBeNull();
}