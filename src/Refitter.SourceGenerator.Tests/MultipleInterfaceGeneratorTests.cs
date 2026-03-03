using FluentAssertions;
using Refit;
using Refitter.Tests.AdditionalFiles.ByEndpoint;
using TUnit.Core;

namespace Refitter.SourceGenerators.Tests;

public class MultipleInterfaceGeneratorTests
{
    private const string ExpectedNamespace = "Refitter.Tests.AdditionalFiles.ByEndpoint";
    private const string BaseUrl = "https://petstore3.swagger.io/api/v3";

    [Test]
    public void Should_Generate_All_Interfaces()
    {
        typeof(IAddPetEndpoint).Namespace.Should().Be(ExpectedNamespace);
        typeof(IUpdatePetEndpoint).Namespace.Should().Be(ExpectedNamespace);
        typeof(IDeletePetEndpoint).Namespace.Should().Be(ExpectedNamespace);
        typeof(ICreateUserEndpoint).Namespace.Should().Be(ExpectedNamespace);
        typeof(IUpdateUserEndpoint).Namespace.Should().Be(ExpectedNamespace);
        typeof(IDeleteUserEndpoint).Namespace.Should().Be(ExpectedNamespace);
        typeof(ILoginUserEndpoint).Namespace.Should().Be(ExpectedNamespace);
        typeof(ILogoutUserEndpoint).Namespace.Should().Be(ExpectedNamespace);
        typeof(IPlaceOrderEndpoint).Namespace.Should().Be(ExpectedNamespace);
        typeof(IGetOrderByIdEndpoint).Namespace.Should().Be(ExpectedNamespace);
        typeof(IDeleteOrderEndpoint).Namespace.Should().Be(ExpectedNamespace);
        typeof(IGetInventoryEndpoint).Namespace.Should().Be(ExpectedNamespace);
        typeof(IGetPetByIdEndpoint).Namespace.Should().Be(ExpectedNamespace);
        typeof(IUploadFileEndpoint).Namespace.Should().Be(ExpectedNamespace);
        typeof(IFindPetsByStatusEndpoint).Namespace.Should().Be(ExpectedNamespace);
        typeof(IFindPetsByTagsEndpoint).Namespace.Should().Be(ExpectedNamespace);
        typeof(IGetUserByNameEndpoint).Namespace.Should().Be(ExpectedNamespace);
        typeof(IUpdatePetWithFormEndpoint).Namespace.Should().Be(ExpectedNamespace);
        typeof(ICreateUsersWithListInputEndpoint).Namespace.Should().Be(ExpectedNamespace);
    }

    [Test]
    public void Can_Resolve_All_Refit_Interfaces()
    {
        RestService.For(typeof(IAddPetEndpoint), BaseUrl).Should().NotBeNull();
        RestService.For(typeof(IUpdatePetEndpoint), BaseUrl).Should().NotBeNull();
        RestService.For(typeof(IDeletePetEndpoint), BaseUrl).Should().NotBeNull();
        RestService.For(typeof(ICreateUserEndpoint), BaseUrl).Should().NotBeNull();
        RestService.For(typeof(IUpdateUserEndpoint), BaseUrl).Should().NotBeNull();
        RestService.For(typeof(IDeleteUserEndpoint), BaseUrl).Should().NotBeNull();
        RestService.For(typeof(ILoginUserEndpoint), BaseUrl).Should().NotBeNull();
        RestService.For(typeof(ILogoutUserEndpoint), BaseUrl).Should().NotBeNull();
        RestService.For(typeof(IPlaceOrderEndpoint), BaseUrl).Should().NotBeNull();
        RestService.For(typeof(IGetOrderByIdEndpoint), BaseUrl).Should().NotBeNull();
        RestService.For(typeof(IDeleteOrderEndpoint), BaseUrl).Should().NotBeNull();
        RestService.For(typeof(IGetInventoryEndpoint), BaseUrl).Should().NotBeNull();
        RestService.For(typeof(IGetPetByIdEndpoint), BaseUrl).Should().NotBeNull();
        RestService.For(typeof(IUploadFileEndpoint), BaseUrl).Should().NotBeNull();
        RestService.For(typeof(IFindPetsByStatusEndpoint), BaseUrl).Should().NotBeNull();
        RestService.For(typeof(IFindPetsByTagsEndpoint), BaseUrl).Should().NotBeNull();
        RestService.For(typeof(IGetUserByNameEndpoint), BaseUrl).Should().NotBeNull();
        RestService.For(typeof(IUpdatePetWithFormEndpoint), BaseUrl).Should().NotBeNull();
        RestService.For(typeof(ICreateUsersWithListInputEndpoint), BaseUrl).Should().NotBeNull();
    }
}
