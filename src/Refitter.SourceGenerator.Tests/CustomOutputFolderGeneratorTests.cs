using FluentAssertions;
using Refit;
using Refitter.Tests.CustomGenerated;
using Xunit;

namespace Refitter.SourceGenerators.Tests;

public class CustomOutputFolderGeneratorTests
{
    [Fact]
    public void Can_Create_File_In_Custom_Path() =>
        File.Exists("../../../CustomGenerated/CustomGenerated.cs").Should().BeTrue();

    [Fact]
    public void Can_Resolve_Refit_Interface() =>
        RestService.For<IApiInCustomGeneratedFolder>("https://petstore3.swagger.io/api/v3")
            .Should()
            .NotBeNull();
}
