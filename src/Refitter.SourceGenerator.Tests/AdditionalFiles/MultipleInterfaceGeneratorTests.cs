using FluentAssertions;

using Xunit;

namespace Refitter.Tests.AdditionalFiles;

public class MultipleInterfaceGeneratorTests
{
    [Theory]
    [InlineData(typeof(IAddPetEndpoint))]
    [InlineData(typeof(IUpdatePetEndpoint))]
    [InlineData(typeof(IDeletePetEndpoint))]
    [InlineData(typeof(ICreateUserEndpoint))]
    [InlineData(typeof(IUpdateUserEndpoint))]
    [InlineData(typeof(IDeleteUserEndpoint))]
    [InlineData(typeof(ILoginUserEndpoint))]
    [InlineData(typeof(ILogoutUserEndpoint))]
    [InlineData(typeof(IPlaceOrderEndpoint))]
    [InlineData(typeof(IGetOrderByIdEndpoint))]
    [InlineData(typeof(IDeleteOrderEndpoint))]
    [InlineData(typeof(IGetInventoryEndpoint))]
    [InlineData(typeof(IGetPetByIdEndpoint))]
    [InlineData(typeof(IUploadFileEndpoint))]
    [InlineData(typeof(IFindPetsByStatusEndpoint))]
    [InlineData(typeof(IFindPetsByTagsEndpoint))]
    [InlineData(typeof(IGetUserByNameEndpoint))]
    [InlineData(typeof(IUpdatePetWithFormEndpoint))]
    [InlineData(typeof(ICreateUsersWithListInputEndpoint))]
    public void Should_Generate_Interface(Type type) =>
        type
            .Namespace
            .Should()
            .Be("Refitter.Tests.AdditionalFiles");
}