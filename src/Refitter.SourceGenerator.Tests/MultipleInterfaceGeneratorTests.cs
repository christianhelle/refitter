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
        VerifyHasRefitAttributes(typeof(IAddPetEndpoint));
        VerifyHasRefitAttributes(typeof(IUpdatePetEndpoint));
        VerifyHasRefitAttributes(typeof(IDeletePetEndpoint));
        VerifyHasRefitAttributes(typeof(ICreateUserEndpoint));
        VerifyHasRefitAttributes(typeof(IUpdateUserEndpoint));
        VerifyHasRefitAttributes(typeof(IDeleteUserEndpoint));
        VerifyHasRefitAttributes(typeof(ILoginUserEndpoint));
        VerifyHasRefitAttributes(typeof(ILogoutUserEndpoint));
        VerifyHasRefitAttributes(typeof(IPlaceOrderEndpoint));
        VerifyHasRefitAttributes(typeof(IGetOrderByIdEndpoint));
        VerifyHasRefitAttributes(typeof(IDeleteOrderEndpoint));
        VerifyHasRefitAttributes(typeof(IGetInventoryEndpoint));
        VerifyHasRefitAttributes(typeof(IGetPetByIdEndpoint));
        VerifyHasRefitAttributes(typeof(IUploadFileEndpoint));
        VerifyHasRefitAttributes(typeof(IFindPetsByStatusEndpoint));
        VerifyHasRefitAttributes(typeof(IFindPetsByTagsEndpoint));
        VerifyHasRefitAttributes(typeof(IGetUserByNameEndpoint));
        VerifyHasRefitAttributes(typeof(IUpdatePetWithFormEndpoint));
        VerifyHasRefitAttributes(typeof(ICreateUsersWithListInputEndpoint));
    }

    private static void VerifyHasRefitAttributes(System.Type type)
    {
        var hasRefitAttributes = type
            .GetMethods()
            .SelectMany(m => m.GetCustomAttributes(inherit: false))
            .Any(a => a is HttpMethodAttribute);
        hasRefitAttributes.Should().BeTrue($"{type.Name} should have at least one Refit HTTP method attribute");
    }
}
