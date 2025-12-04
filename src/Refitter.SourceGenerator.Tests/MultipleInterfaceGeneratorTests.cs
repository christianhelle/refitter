using FluentAssertions;
using Refit;
using Refitter.Tests.AdditionalFiles.ByEndpoint;
using TUnit.Core;

namespace Refitter.SourceGenerators.Tests;

public class MultipleInterfaceGeneratorTests
{
    [Test]
    [Arguments(typeof(IAddPetEndpoint))]
    [Arguments(typeof(IUpdatePetEndpoint))]
    [Arguments(typeof(IDeletePetEndpoint))]
    [Arguments(typeof(ICreateUserEndpoint))]
    [Arguments(typeof(IUpdateUserEndpoint))]
    [Arguments(typeof(IDeleteUserEndpoint))]
    [Arguments(typeof(ILoginUserEndpoint))]
    [Arguments(typeof(ILogoutUserEndpoint))]
    [Arguments(typeof(IPlaceOrderEndpoint))]
    [Arguments(typeof(IGetOrderByIdEndpoint))]
    [Arguments(typeof(IDeleteOrderEndpoint))]
    [Arguments(typeof(IGetInventoryEndpoint))]
    [Arguments(typeof(IGetPetByIdEndpoint))]
    [Arguments(typeof(IUploadFileEndpoint))]
    [Arguments(typeof(IFindPetsByStatusEndpoint))]
    [Arguments(typeof(IFindPetsByTagsEndpoint))]
    [Arguments(typeof(IGetUserByNameEndpoint))]
    [Arguments(typeof(IUpdatePetWithFormEndpoint))]
    [Arguments(typeof(ICreateUsersWithListInputEndpoint))]
    public void Should_Generate_Interface(Type type) =>
        type
            .Namespace
            .Should()
            .Be("Refitter.Tests.AdditionalFiles.ByEndpoint");

    [Test]
    [Arguments(typeof(IAddPetEndpoint))]
    [Arguments(typeof(IUpdatePetEndpoint))]
    [Arguments(typeof(IDeletePetEndpoint))]
    [Arguments(typeof(ICreateUserEndpoint))]
    [Arguments(typeof(IUpdateUserEndpoint))]
    [Arguments(typeof(IDeleteUserEndpoint))]
    [Arguments(typeof(ILoginUserEndpoint))]
    [Arguments(typeof(ILogoutUserEndpoint))]
    [Arguments(typeof(IPlaceOrderEndpoint))]
    [Arguments(typeof(IGetOrderByIdEndpoint))]
    [Arguments(typeof(IDeleteOrderEndpoint))]
    [Arguments(typeof(IGetInventoryEndpoint))]
    [Arguments(typeof(IGetPetByIdEndpoint))]
    [Arguments(typeof(IUploadFileEndpoint))]
    [Arguments(typeof(IFindPetsByStatusEndpoint))]
    [Arguments(typeof(IFindPetsByTagsEndpoint))]
    [Arguments(typeof(IGetUserByNameEndpoint))]
    [Arguments(typeof(IUpdatePetWithFormEndpoint))]
    [Arguments(typeof(ICreateUsersWithListInputEndpoint))]
    public void Can_Resolve_Refit_Interface(Type type) =>
        RestService.For(type, "https://petstore3.swagger.io/api/v3")
            .Should()
            .NotBeNull();
}
