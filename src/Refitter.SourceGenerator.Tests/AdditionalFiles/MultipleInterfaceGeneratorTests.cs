using FluentAssertions;

using Xunit;

namespace Refitter.Tests.AdditionalFiles;

public class MultipleInterfaceGeneratorTests
{
    [Theory]
    [InlineData(typeof(IAddPet))]
    [InlineData(typeof(IUpdatePet))]
    [InlineData(typeof(IDeletePet))]
    [InlineData(typeof(ICreateUser))]
    [InlineData(typeof(IUpdateUser))]
    [InlineData(typeof(IDeleteUser))]
    [InlineData(typeof(ILoginUser))]
    [InlineData(typeof(ILogoutUser))]
    [InlineData(typeof(IPlaceOrder))]
    [InlineData(typeof(IGetOrderById))]
    [InlineData(typeof(IDeleteOrder))]
    [InlineData(typeof(IGetInventory))]
    [InlineData(typeof(IGetPetById))]
    [InlineData(typeof(IUploadFile))]
    [InlineData(typeof(IFindPetsByStatus))]
    [InlineData(typeof(IFindPetsByTags))]
    [InlineData(typeof(IGetUserByName))]
    [InlineData(typeof(IUpdatePetWithForm))]
    [InlineData(typeof(ICreateUsersWithListInput))]
    public void Should_Generate_Interface(Type type) =>
        type
            .Namespace
            .Should()
            .Be("Refitter.Tests.AdditionalFiles");
}