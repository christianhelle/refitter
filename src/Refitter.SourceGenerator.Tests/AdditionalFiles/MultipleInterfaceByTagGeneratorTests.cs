using FluentAssertions;

using Refit;

using Xunit;

namespace Refitter.Tests.AdditionalFiles;

public class MultipleInterfaceByTagGeneratorTests
{
    [Theory]
    [InlineData(typeof(ByTag.IPetApi))]
    [InlineData(typeof(ByTag.IUserApi))]
    [InlineData(typeof(ByTag.IStoreApi))]
    public void Should_Generate_Interface(Type type) =>
        type
            .Namespace
            .Should()
            .Be("Refitter.Tests.AdditionalFiles.ByTag");

    [Theory]
    [InlineData(typeof(ByTag.IPetApi))]
    [InlineData(typeof(ByTag.IUserApi))]
    [InlineData(typeof(ByTag.IStoreApi))]
    public void Can_Resolve_Refit_Interface(Type type) =>
        RestService.For(type, "https://petstore3.swagger.io/api/v3")
            .Should()
            .NotBeNull();
}