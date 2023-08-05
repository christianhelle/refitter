using FluentAssertions;

using Xunit;

namespace Refitter.Tests.AdditionalFiles;

public class MultipleInterfaceGeneratorTests
{
    [Theory]
    [InlineData(typeof(ByEndpoint.IAddPetEndpoint))]
    [InlineData(typeof(ByEndpoint.IUpdatePetEndpoint))]
    [InlineData(typeof(ByEndpoint.IDeletePetEndpoint))]
    [InlineData(typeof(ByEndpoint.ICreateUserEndpoint))]
    [InlineData(typeof(ByEndpoint.IUpdateUserEndpoint))]
    [InlineData(typeof(ByEndpoint.IDeleteUserEndpoint))]
    [InlineData(typeof(ByEndpoint.ILoginUserEndpoint))]
    [InlineData(typeof(ByEndpoint.ILogoutUserEndpoint))]
    [InlineData(typeof(ByEndpoint.IPlaceOrderEndpoint))]
    [InlineData(typeof(ByEndpoint.IGetOrderByIdEndpoint))]
    [InlineData(typeof(ByEndpoint.IDeleteOrderEndpoint))]
    [InlineData(typeof(ByEndpoint.IGetInventoryEndpoint))]
    [InlineData(typeof(ByEndpoint.IGetPetByIdEndpoint))]
    [InlineData(typeof(ByEndpoint.IUploadFileEndpoint))]
    [InlineData(typeof(ByEndpoint.IFindPetsByStatusEndpoint))]
    [InlineData(typeof(ByEndpoint.IFindPetsByTagsEndpoint))]
    [InlineData(typeof(ByEndpoint.IGetUserByNameEndpoint))]
    [InlineData(typeof(ByEndpoint.IUpdatePetWithFormEndpoint))]
    [InlineData(typeof(ByEndpoint.ICreateUsersWithListInputEndpoint))]
    public void Should_Generate_Interface(Type type) =>
        type
            .Namespace
            .Should()
            .Be("Refitter.Tests.AdditionalFiles.ByEndpoint");
}