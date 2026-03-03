using FluentAssertions;
using Refit;
using Refitter.Tests.AdditionalFiles.ByTag;
using TUnit.Core;

namespace Refitter.SourceGenerators.Tests;

public class MultipleInterfaceByTagGeneratorTests
{
    private const string ExpectedNamespace = "Refitter.Tests.AdditionalFiles.ByTag";
    private const string BaseUrl = "https://petstore3.swagger.io/api/v3";

    [Test]
    public void Should_Generate_IPetApi_Interface() =>
        typeof(IPetApi).Namespace.Should().Be(ExpectedNamespace);

    [Test]
    public void Should_Generate_IUserApi_Interface() =>
        typeof(IUserApi).Namespace.Should().Be(ExpectedNamespace);

    [Test]
    public void Should_Generate_IStoreApi_Interface() =>
        typeof(IStoreApi).Namespace.Should().Be(ExpectedNamespace);

    [Test]
    public void Can_Resolve_IPetApi_Refit_Interface() =>
        RestService.For(typeof(IPetApi), BaseUrl).Should().NotBeNull();

    [Test]
    public void Can_Resolve_IUserApi_Refit_Interface() =>
        RestService.For(typeof(IUserApi), BaseUrl).Should().NotBeNull();

    [Test]
    public void Can_Resolve_IStoreApi_Refit_Interface() =>
        RestService.For(typeof(IStoreApi), BaseUrl).Should().NotBeNull();
}
