using System.Text.RegularExpressions;
using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.Resources;
using Refitter.Tests.TestUtilities;

namespace Refitter.Tests;


[TestClass]
public class SwaggerPetstoreTests
{
    [TestMethod]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
    public async Task Can_Generate_Code(SampleOpenSpecifications version, string filename)
    {
        var generatedCode = await GenerateCode(version, filename);
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [TestMethod]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
    public async Task Can_Generate_Partial_Interfaces(SampleOpenSpecifications version, string filename)
    {
        var generatedCode = await GenerateCode(version, filename);
        generatedCode.Should().Contain("partial interface");
    }

    [TestMethod]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
    public async Task Can_Generate_Code_Without_Contracts(SampleOpenSpecifications version, string filename)
    {
        var generatedCode = await GenerateCode(
            version,
            filename,
            new RefitGeneratorSettings { GenerateXmlDocCodeComments = false, GenerateContracts = false });
        generatedCode.Should().NotContain("class Pet");
    }

    [TestMethod]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
    public async Task Can_Generate_Code_Without_Clients(SampleOpenSpecifications version, string filename)
    {
        var generatedCode = await GenerateCode(
            version,
            filename,
            new RefitGeneratorSettings { GenerateXmlDocCodeComments = false, GenerateClients = false });
        generatedCode.Should().NotContain("ISwaggerPetstore");
    }

    [TestMethod]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
    public async Task Can_Generate_Code_With_XmlDoc_Code_Comments(SampleOpenSpecifications version, string filename)
    {
        var generatedCode = await GenerateCode(
            version,
            filename,
            new RefitGeneratorSettings { GenerateXmlDocCodeComments = true, GenerateContracts = false });
        generatedCode.Should().Contain("<summary>");
    }

    [TestMethod]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
    public async Task Can_Generate_Code_Without_XmlDoc_Code_Comments(SampleOpenSpecifications version, string filename)
    {
        var generatedCode = await GenerateCode(
            version,
            filename,
            new RefitGeneratorSettings { GenerateXmlDocCodeComments = false, GenerateContracts = false });
        generatedCode.Should().NotContain("<summary>");
    }

    [TestMethod]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
    public async Task Can_Generate_Code_With_Default_Namespace(SampleOpenSpecifications version, string filename)
    {
        var generatedCode = await GenerateCode(
            version,
            filename,
            new RefitGeneratorSettings { Namespace = "Some.Other.Namespace" });
        generatedCode.Should().Contain("namespace Some.Other.Namespace");
    }

    [TestMethod]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
    public async Task Can_Generate_Code_With_Fixed_Interface_Name(SampleOpenSpecifications version, string filename)
    {
        var settings = new RefitGeneratorSettings();
        settings.Naming.UseOpenApiTitle = false;
        settings.Naming.InterfaceName = "SomeOtherName";
        var generatedCode = await GenerateCode(version, filename, settings);
        generatedCode.Should().Contain("interface ISomeOtherName");
    }

    [TestMethod]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
    public async Task Can_Generate_Code_Without_AutoGenerated_Header(SampleOpenSpecifications version, string filename)
    {
        var settings = new RefitGeneratorSettings();
        settings.AddAutoGeneratedHeader = false;
        var generatedCode = await GenerateCode(version, filename, settings);
        generatedCode.Should().NotContain("This code was generated by Refitter");
    }

    [TestMethod]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
    public async Task Can_Generate_Code_That_Returns_IApiResponse(SampleOpenSpecifications version, string filename)
    {
        var settings = new RefitGeneratorSettings();
        settings.ReturnIApiResponse = true;
        var generatedCode = await GenerateCode(version, filename, settings);
        generatedCode.Should().Contain("Task<IApiResponse<Pet>>");
    }

    [TestMethod]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
    public async Task Can_Generate_Code_With_Type_Override(SampleOpenSpecifications version, string filename)
    {
        var settings = new RefitGeneratorSettings
        {
            ReturnIApiResponse = false,
            ResponseTypeOverride =
            {
                ["getPetById"] = "IApiResponse<Pet>", // Wrap existing type
                ["deletePet"] = "Pet", // Add type where there was none
                ["addPet"] = "void", // Remove type
            },
        };
        var generatedCode = await GenerateCode(version, filename, settings);
        generatedCode.Should().Contain("Task<IApiResponse<Pet>> GetPetById");
        generatedCode.Should().Contain("Task<Pet> DeletePet");
        generatedCode.Should().Contain("Task AddPet");
    }

    [TestMethod]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
    public async Task Can_Generate_Code_With_Internal_Contract_Types(SampleOpenSpecifications version, string filename)
    {
        var settings = new RefitGeneratorSettings();
        settings.TypeAccessibility = TypeAccessibility.Internal;
        var generatedCode = await GenerateCode(version, filename, settings);
        generatedCode.Should().Contain("internal partial class");
    }

    [TestMethod]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
    public async Task Can_Generate_Code_With_Internal_Interface(SampleOpenSpecifications version, string filename)
    {
        var settings = new RefitGeneratorSettings();
        settings.TypeAccessibility = TypeAccessibility.Internal;
        var generatedCode = await GenerateCode(version, filename, settings);
        generatedCode.Should().Contain("internal partial interface");
    }

    [TestMethod]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
    public async Task Can_Generate_Code_With_CancellationToken(SampleOpenSpecifications version, string filename)
    {
        var settings = new RefitGeneratorSettings();
        settings.UseCancellationTokens = true;
        var generatedCode = await GenerateCode(version, filename, settings);
        generatedCode.Should().Contain("CancellationToken cancellationToken = default");
    }

    [TestMethod]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json", true)]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml", true)]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json", true)]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml", true)]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json", false)]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml", false)]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json", false)]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml", false)]
    public async Task Can_Generate_Code_With_Correct_Usings(
        SampleOpenSpecifications version,
        string filename,
        bool cancellationTokens)
    {
        var settings = new RefitGeneratorSettings();
        settings.UseCancellationTokens = cancellationTokens;
        var generatedCode = await GenerateCode(version, filename, settings);
        generatedCode.Contains("using System.Threading;").Should().Be(cancellationTokens);
    }

    [TestMethod]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV3WithDifferentHeaders, "SwaggerPetstoreWithDifferentHeaders.json")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV3WithDifferentHeaders, "SwaggerPetstoreWithDifferentHeaders.yaml")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV2WithDifferentHeaders, "SwaggerPetstoreWithDifferentHeaders.json")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV2WithDifferentHeaders, "SwaggerPetstoreWithDifferentHeaders.yaml")]
    public async Task Can_Generate_Code_With_OperationHeaders_With_Different_Headers(SampleOpenSpecifications version, string filename)
    {
        var settings = new RefitGeneratorSettings();
        settings.GenerateOperationHeaders = true;
        var generatedCode = await GenerateCode(version, filename, settings);
        generatedCode.Should().Contain("[Header(\"api-key\")] string api_key");
    }

    [TestMethod]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
    public async Task Can_Generate_Code_With_OperationHeaders(SampleOpenSpecifications version, string filename)
    {
        var settings = new RefitGeneratorSettings();
        settings.GenerateOperationHeaders = true;
        var generatedCode = await GenerateCode(version, filename, settings);
        generatedCode.Should().Contain("[Header(\"api_key\")] string api_key");
    }

    [TestMethod]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
    public async Task Can_Generate_Code_With_OperationHeadersAndWithIgnoredHeaders(SampleOpenSpecifications version, string filename)
    {
        var settings = new RefitGeneratorSettings();
        settings.GenerateOperationHeaders = true;
        settings.IgnoredOperationHeaders = ["api_key"];
        var generatedCode = await GenerateCode(version, filename, settings);
        generatedCode.Should().NotContain("[Header(\"api_key\")] string api_key");
        generatedCode.Should().NotContain("[Header(");
    }

    [TestMethod]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
    public async Task Can_Generate_Code_Without_OperationHeaders(SampleOpenSpecifications version, string filename)
    {
        var settings = new RefitGeneratorSettings();
        settings.GenerateOperationHeaders = false;
        var generatedCode = await GenerateCode(version, filename, settings);
        generatedCode.Should().NotContain("[Header(\"api_key\")] string? api_key");
    }

    [TestMethod]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV3WithUnsafeAuthenticationHeaders, "SwaggerPetstoreWithUnsafeAuthenticationHeaders.json")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV3WithUnsafeAuthenticationHeaders, "SwaggerPetstoreWithUnsafeAuthenticationHeaders.yaml")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV2WithUnsafeAuthenticationHeaders, "SwaggerPetstoreWithUnsafeAuthenticationHeaders.json")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV2WithUnsafeAuthenticationHeaders, "SwaggerPetstoreWithUnsafeAuthenticationHeaders.yaml")]
    public async Task Can_Generate_Code_With_Unsafe_AuthenticationHeaders(SampleOpenSpecifications version, string filename)
    {
        var settings = new RefitGeneratorSettings();
        settings.AuthenticationHeaderStyle = AuthenticationHeaderStyle.Parameter;
        var generatedCode = await GenerateCode(version, filename, settings);
        generatedCode.Should().Contain("[Header(\"auth.key-test_2\")] string auth_key_test_2");
    }

    [TestMethod]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV3WithAuthenticationHeaders, "SwaggerPetstoreWithAuthenticationHeaders.json")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV3WithAuthenticationHeaders, "SwaggerPetstoreWithAuthenticationHeaders.yaml")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV2WithAuthenticationHeaders, "SwaggerPetstoreWithAuthenticationHeaders.json")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV2WithAuthenticationHeaders, "SwaggerPetstoreWithAuthenticationHeaders.yaml")]
    public async Task Can_Generate_Code_With_AuthenticationHeaders(SampleOpenSpecifications version, string filename)
    {
        var settings = new RefitGeneratorSettings();
        settings.AuthenticationHeaderStyle = AuthenticationHeaderStyle.Parameter;
        var generatedCode = await GenerateCode(version, filename, settings);
        generatedCode.Should().Contain("[Header(\"auth_key\")] string auth_key");
    }

    [TestMethod]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV3WithAuthenticationHeaders, "SwaggerPetstoreWithAuthenticationHeaders.json")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV3WithAuthenticationHeaders, "SwaggerPetstoreWithAuthenticationHeaders.yaml")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV2WithAuthenticationHeaders, "SwaggerPetstoreWithAuthenticationHeaders.json")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV2WithAuthenticationHeaders, "SwaggerPetstoreWithAuthenticationHeaders.yaml")]
    public async Task Can_Generate_Code_With_AuthenticationHeaders_Without_OperationHeaders(SampleOpenSpecifications version, string filename)
    {
        var settings = new RefitGeneratorSettings();
        settings.GenerateOperationHeaders = false;
        settings.AuthenticationHeaderStyle = AuthenticationHeaderStyle.Parameter;
        var generatedCode = await GenerateCode(version, filename, settings);
        generatedCode.Should().Contain("[Header(\"auth_key\")] string auth_key");
    }

    [TestMethod]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
    public async Task Can_Generate_Code_Without_AuthenticationHeaders(SampleOpenSpecifications version, string filename)
    {
        var settings = new RefitGeneratorSettings();
        settings.AuthenticationHeaderStyle = AuthenticationHeaderStyle.None;
        var generatedCode = await GenerateCode(version, filename, settings);
        generatedCode.Should().NotContain("[Header(\"auth_key\")] string? auth_key");
    }

    [TestMethod]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV3WithBearerAuthenticationHeaders, "SwaggerPetstoreWithBearerAuthenticationHeaders.json")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV3WithBearerAuthenticationHeaders, "SwaggerPetstoreWithBearerAuthenticationHeaders.yaml")]
    public async Task Can_Generate_Code_With_Bearer_AuthenticationHeaders_Method_Style(SampleOpenSpecifications version, string filename)
    {
        var settings = new RefitGeneratorSettings();
        settings.AuthenticationHeaderStyle = AuthenticationHeaderStyle.Method;
        var generatedCode = await GenerateCode(version, filename, settings);
        generatedCode.Should().Contain("[Headers(\"Authorization: Bearer\")]");
    }

    [TestMethod]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV3WithBearerAuthenticationHeaders, "SwaggerPetstoreWithBearerAuthenticationHeaders.json")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV3WithBearerAuthenticationHeaders, "SwaggerPetstoreWithBearerAuthenticationHeaders.yaml")]
    public async Task Can_Generate_Code_With_Bearer_AuthenticationHeaders_Parameter_Style(SampleOpenSpecifications version, string filename)
    {
        var settings = new RefitGeneratorSettings();
        settings.AuthenticationHeaderStyle = AuthenticationHeaderStyle.Parameter;
        var generatedCode = await GenerateCode(version, filename, settings);
        generatedCode.Should().Contain("[Header(\"Authorization: Bearer\")] string bearerToken");
    }

    [TestMethod]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV3WithBearerAuthenticationHeaders, "SwaggerPetstoreWithBearerAuthenticationHeaders.json")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV3WithBearerAuthenticationHeaders, "SwaggerPetstoreWithBearerAuthenticationHeaders.yaml")]
    public async Task Can_Build_Generated_Code_With_Bearer_AuthenticationHeaders_Method_Style(SampleOpenSpecifications version, string filename)
    {
        var settings = new RefitGeneratorSettings();
        settings.AuthenticationHeaderStyle = AuthenticationHeaderStyle.Method;
        var generatedCode = await GenerateCode(version, filename, settings);
        BuildHelper
            .BuildCSharp(generatedCode)
            .Should()
            .BeTrue();
    }

    [TestMethod]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV3WithBearerAuthenticationHeaders, "SwaggerPetstoreWithBearerAuthenticationHeaders.json")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV3WithBearerAuthenticationHeaders, "SwaggerPetstoreWithBearerAuthenticationHeaders.yaml")]
    public async Task Can_Build_Generated_Code_With_Bearer_AuthenticationHeaders_Parameter_Style(SampleOpenSpecifications version, string filename)
    {
        var settings = new RefitGeneratorSettings();
        settings.AuthenticationHeaderStyle = AuthenticationHeaderStyle.Parameter;
        var generatedCode = await GenerateCode(version, filename, settings);
        BuildHelper
            .BuildCSharp(generatedCode)
            .Should()
            .BeTrue();
    }

    [TestMethod]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV3WithBearerAuthenticationHeaders, "SwaggerPetstoreWithBearerAuthenticationHeaders.json")]
    public async Task Method_Style_Does_Not_Generate_Parameter_For_Bearer(SampleOpenSpecifications version, string filename)
    {
        var settings = new RefitGeneratorSettings();
        settings.AuthenticationHeaderStyle = AuthenticationHeaderStyle.Method;
        var generatedCode = await GenerateCode(version, filename, settings);
        generatedCode.Should().NotContain("string bearerToken");
    }

    [TestMethod]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV3WithAuthenticationHeaders, "SwaggerPetstoreWithAuthenticationHeaders.json")]
    public async Task Method_Style_Does_Not_Generate_Header_For_ApiKey(SampleOpenSpecifications version, string filename)
    {
        var settings = new RefitGeneratorSettings();
        settings.AuthenticationHeaderStyle = AuthenticationHeaderStyle.Method;
        var generatedCode = await GenerateCode(version, filename, settings);
        generatedCode.Should().NotContain("[Headers(\"auth_key\")]");
    }

    [TestMethod]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV3WithMixedAuthenticationHeaders, "SwaggerPetstoreWithMixedAuthenticationHeaders.json")]
    public async Task SecurityScheme_Filter_With_Parameter_Style_Only_Generates_Specified_Scheme(SampleOpenSpecifications version, string filename)
    {
        var settings = new RefitGeneratorSettings();
        settings.AuthenticationHeaderStyle = AuthenticationHeaderStyle.Parameter;
        settings.SecurityScheme = "auth_key";
        var generatedCode = await GenerateCode(version, filename, settings);
        generatedCode.Should().Contain("[Header(\"auth_key\")] string auth_key");
        generatedCode.Should().NotContain("string bearerToken");
    }

    [TestMethod]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV3WithMixedAuthenticationHeaders, "SwaggerPetstoreWithMixedAuthenticationHeaders.json")]
    public async Task SecurityScheme_Filter_With_Parameter_Style_Only_Generates_Bearer_When_Specified(SampleOpenSpecifications version, string filename)
    {
        var settings = new RefitGeneratorSettings();
        settings.AuthenticationHeaderStyle = AuthenticationHeaderStyle.Parameter;
        settings.SecurityScheme = "bearerAuth";
        var generatedCode = await GenerateCode(version, filename, settings);
        generatedCode.Should().Contain("[Header(\"Authorization: Bearer\")] string bearerToken");
        // auth_key only appears once (as a regular operation parameter on DeletePet),
        // not on every operation (which would indicate the security filter is not working)
        Regex.Matches(generatedCode, @"\[Header\(""auth_key""\)\]").Count.Should().BeLessThanOrEqualTo(1);
    }

    [TestMethod]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV3WithMixedAuthenticationHeaders, "SwaggerPetstoreWithMixedAuthenticationHeaders.json")]
    public async Task SecurityScheme_Filter_With_Method_Style_Only_Generates_Bearer_When_Specified(SampleOpenSpecifications version, string filename)
    {
        var settings = new RefitGeneratorSettings();
        settings.AuthenticationHeaderStyle = AuthenticationHeaderStyle.Method;
        settings.SecurityScheme = "bearerAuth";
        var generatedCode = await GenerateCode(version, filename, settings);
        generatedCode.Should().Contain("[Headers(\"Authorization: Bearer\")]");
    }

    [TestMethod]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV3WithMixedAuthenticationHeaders, "SwaggerPetstoreWithMixedAuthenticationHeaders.json")]
    public async Task SecurityScheme_Filter_With_Method_Style_Ignores_NonMatching_Scheme(SampleOpenSpecifications version, string filename)
    {
        var settings = new RefitGeneratorSettings();
        settings.AuthenticationHeaderStyle = AuthenticationHeaderStyle.Method;
        settings.SecurityScheme = "auth_key";
        var generatedCode = await GenerateCode(version, filename, settings);
        generatedCode.Should().NotContain("[Headers(\"Authorization: Bearer\")]");
    }

    [TestMethod]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
    public async Task Can_Generate_Code_With_Accept_Request_Header(SampleOpenSpecifications version, string filename)
    {
        var settings = new RefitGeneratorSettings();
        var generatedCode = await GenerateCode(version, filename, settings);
        if (version is SampleOpenSpecifications.SwaggerPetstoreJsonV3 or SampleOpenSpecifications.SwaggerPetstoreYamlV3)
            generatedCode.Should().Contain("[Headers(\"Accept: application/json\"");
        else if (version is SampleOpenSpecifications.SwaggerPetstoreJsonV2 or SampleOpenSpecifications.SwaggerPetstoreYamlV2)
            generatedCode.Should().NotContain("[Headers(\"Accept: application/json\"");
    }

    [TestMethod]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    public async Task Can_Generate_Code_With_No_Accept_Request_Header(SampleOpenSpecifications version, string filename)
    {
        var settings = new RefitGeneratorSettings();
        settings.AddAcceptHeaders = false;
        var generatedCode = await GenerateCode(version, filename, settings);
        generatedCode.Should().NotContain("[Headers(\"Accept");
    }

    [TestMethod]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
    public async Task Can_Generate_Code_With_Multiple_Interfaces(SampleOpenSpecifications version, string filename)
    {
        var settings = new RefitGeneratorSettings();
        settings.MultipleInterfaces = MultipleInterfaces.ByEndpoint;
        var generatedCode = await GenerateCode(version, filename, settings);
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [TestMethod]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
    public async Task Can_Generate_Code_Dependency_Injection_Setup(SampleOpenSpecifications version, string filename)
    {
        var settings = new RefitGeneratorSettings();
        settings.DependencyInjectionSettings = new DependencyInjectionSettings
        {
            BaseUrl = "https://petstore3.swagger.io/api/v3",
            TransientErrorHandler = TransientErrorHandler.Polly
        };
        var generatedCode = await GenerateCode(version, filename, settings);
        generatedCode.Should().Contain("ConfigureRefitClients");
    }

    [TestMethod]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
    public async Task Can_Generate_Code_Dependency_Injection_Setup_With_Polly(SampleOpenSpecifications version, string filename)
    {
        var settings = new RefitGeneratorSettings();
        settings.DependencyInjectionSettings = new DependencyInjectionSettings
        {
            BaseUrl = "https://petstore3.swagger.io/api/v3",
            TransientErrorHandler = TransientErrorHandler.Polly
        };
        var generatedCode = await GenerateCode(version, filename, settings);
        generatedCode.Should().Contain("using Polly");
    }

    [TestMethod]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
    public async Task Can_Generate_Code_Dependency_Injection_Setup_Without_Polly(SampleOpenSpecifications version, string filename)
    {
        var settings = new RefitGeneratorSettings();
        settings.DependencyInjectionSettings = new DependencyInjectionSettings
        {
            BaseUrl = "https://petstore3.swagger.io/api/v3",
            TransientErrorHandler = TransientErrorHandler.None
        };
        var generatedCode = await GenerateCode(version, filename, settings);
        generatedCode.Should().NotContain("using Polly");
    }

    [TestMethod]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
    public async Task Can_Generate_Code_Apizr_Setup(SampleOpenSpecifications version, string filename)
    {
        var settings = new RefitGeneratorSettings
        {
            DependencyInjectionSettings = new DependencyInjectionSettings
            {
                BaseUrl = "https://petstore3.swagger.io/api/v3",
                TransientErrorHandler = TransientErrorHandler.Polly
            },
            ApizrSettings = new ApizrSettings
            {
                WithRequestOptions = true,
                WithRegistrationHelper = true,
                WithCacheProvider = CacheProviderType.InMemory,
                WithPriority = true,
                WithMediation = true,
                WithMappingProvider = MappingProviderType.AutoMapper,
                WithFileTransfer = true
            }
        };
        var generatedCode = await GenerateCode(version, filename, settings);
        generatedCode.Should().Contain("AddApizrManagerFor");
    }

    [TestMethod]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
    public async Task Can_Generate_Code_Apizr_Setup_With_Polly(SampleOpenSpecifications version, string filename)
    {
        var settings = new RefitGeneratorSettings
        {
            DependencyInjectionSettings = new DependencyInjectionSettings
            {
                BaseUrl = "https://petstore3.swagger.io/api/v3",
                TransientErrorHandler = TransientErrorHandler.Polly
            },
            ApizrSettings = new ApizrSettings
            {
                WithRequestOptions = true,
                WithRegistrationHelper = true,
                WithCacheProvider = CacheProviderType.InMemory,
                WithPriority = true,
                WithMediation = true,
                WithMappingProvider = MappingProviderType.AutoMapper,
                WithFileTransfer = true
            }
        };
        var generatedCode = await GenerateCode(version, filename, settings);
        generatedCode.Should().Contain("using Polly");
    }

    [TestMethod]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
    public async Task Can_Generate_Code_Apizr_Setup_Without_Polly(SampleOpenSpecifications version, string filename)
    {
        var settings = new RefitGeneratorSettings
        {
            DependencyInjectionSettings = new DependencyInjectionSettings
            {
                BaseUrl = "https://petstore3.swagger.io/api/v3",
                TransientErrorHandler = TransientErrorHandler.None
            },
            ApizrSettings = new ApizrSettings
            {
                WithRequestOptions = true,
                WithRegistrationHelper = true,
                WithCacheProvider = CacheProviderType.InMemory,
                WithPriority = true,
                WithMediation = true,
                WithMappingProvider = MappingProviderType.AutoMapper,
                WithFileTransfer = true
            }
        };
        var generatedCode = await GenerateCode(version, filename, settings);
        generatedCode.Should().NotContain("using Polly");
    }

    [TestMethod]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json", MultipleInterfaces.Unset)]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml", MultipleInterfaces.Unset)]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json", MultipleInterfaces.Unset)]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml", MultipleInterfaces.Unset)]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json", MultipleInterfaces.ByEndpoint)]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml", MultipleInterfaces.ByEndpoint)]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json", MultipleInterfaces.ByEndpoint)]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml", MultipleInterfaces.ByEndpoint)]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json", MultipleInterfaces.ByTag)]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml", MultipleInterfaces.ByTag)]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json", MultipleInterfaces.ByTag)]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml", MultipleInterfaces.ByTag)]
    public async Task Can_Generate_Code_GeneratedCode_Attribute(
        SampleOpenSpecifications version,
        string filename,
        MultipleInterfaces multipleInterfaces)
    {
        var settings = new RefitGeneratorSettings();
        settings.MultipleInterfaces = multipleInterfaces;
        var generatedCode = await GenerateCode(version, filename, settings);
        generatedCode.Should().Contain("[System.CodeDom.Compiler.GeneratedCode(\"Refitter\"");
    }

    [TestMethod]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json", MultipleInterfaces.ByEndpoint)]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml", MultipleInterfaces.ByEndpoint)]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json", MultipleInterfaces.ByEndpoint)]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml", MultipleInterfaces.ByEndpoint)]
    public async Task Can_Generate_Code_With_Multiple_Interfaces_And_OperationNameTemplate(
        SampleOpenSpecifications version,
        string filename,
        MultipleInterfaces multipleInterfaces)
    {
        var settings = new RefitGeneratorSettings();
        settings.MultipleInterfaces = multipleInterfaces;
        settings.OperationNameTemplate = "ExecuteAsync";
        var generatedCode = await GenerateCode(version, filename, settings);
        generatedCode.Should().Contain("ExecuteAsync(");
    }

    [TestMethod]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
#if !DEBUG
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
#endif
    public async Task Can_Build_Generated_Code(SampleOpenSpecifications version, string filename)
    {
        var generatedCode = await GenerateCode(version, filename);
        BuildHelper
            .BuildCSharp(generatedCode)
            .Should()
            .BeTrue();
    }

    [TestMethod]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
#if !DEBUG
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
#endif
    public async Task Can_Build_Generated_Code_With_IApiResponse(SampleOpenSpecifications version, string filename)
    {
        var settings = new RefitGeneratorSettings();
        settings.ReturnIApiResponse = true;
        var generatedCode = await GenerateCode(version, filename, settings);
        BuildHelper
            .BuildCSharp(generatedCode)
            .Should()
            .BeTrue();
    }

    [TestMethod]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
#if !DEBUG
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
#endif
    public async Task Can_Build_Generated_Code_With_IObservableResponse(SampleOpenSpecifications version, string filename)
    {
        var settings = new RefitGeneratorSettings();
        settings.ReturnIObservable = true;
        var generatedCode = await GenerateCode(version, filename, settings);

        BuildHelper
            .BuildCSharp(generatedCode)
            .Should()
            .BeTrue();
    }

    [TestMethod]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
#if !DEBUG
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
#endif
    public async Task Can_Build_Generated_Code_With_Multiple_Interfaces(SampleOpenSpecifications version, string filename)
    {
        var settings = new RefitGeneratorSettings();
        settings.MultipleInterfaces = MultipleInterfaces.ByEndpoint;
        var generatedCode = await GenerateCode(version, filename, settings);
        BuildHelper
            .BuildCSharp(generatedCode)
            .Should()
            .BeTrue();
    }

    [TestMethod]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
#if !DEBUG
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
#endif
    public async Task Can_Build_Generated_Code_With_Multiple_Interfaces_ByTag(SampleOpenSpecifications version, string filename)
    {
        var settings = new RefitGeneratorSettings();
        settings.MultipleInterfaces = MultipleInterfaces.ByTag;
        var generatedCode = await GenerateCode(version, filename, settings);
        BuildHelper
            .BuildCSharp(generatedCode)
            .Should()
            .BeTrue();
    }

#if !DEBUG
    [TestMethod]
    [DataRow("http://raw.githubusercontent.com/christianhelle/refitter/main/test/OpenAPI/v3.0/petstore.json")]
    [DataRow("http://raw.githubusercontent.com/christianhelle/refitter/main/test/OpenAPI/v3.0/petstore.yaml")]
    [DataRow("https://raw.githubusercontent.com/christianhelle/refitter/main/test/OpenAPI/v3.0/petstore.json")]
    [DataRow("https://raw.githubusercontent.com/christianhelle/refitter/main/test/OpenAPI/v3.0/petstore.yaml")]
    public async Task Can_Build_Generated_Code_From_Url(string url)
    {
        var settings = new RefitGeneratorSettings { OpenApiPath = url };
        var sut = await RefitGenerator.CreateAsync(settings);
        var generatedCode = sut.Generate();
        BuildHelper
            .BuildCSharp(generatedCode)
            .Should()
            .BeTrue();
    }
#endif

    private static async Task<string> GenerateCode(
        SampleOpenSpecifications version,
        string filename,
        RefitGeneratorSettings? settings = null)
    {
        var swaggerFile = await TestFile.CreateSwaggerFile(EmbeddedResources.GetSwaggerPetstore(version), filename);
        if (settings is null)
        {
            settings = new RefitGeneratorSettings { OpenApiPath = swaggerFile };
        }
        else
        {
            settings.OpenApiPath = swaggerFile;
        }

        var sut = await RefitGenerator.CreateAsync(settings);
        return sut.Generate();
    }

    [TestMethod]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
    public async Task Can_Generate_Code_Obsolete_Attribute(SampleOpenSpecifications version, string filename)
    {
        var generatedCode = await GenerateCode(version, filename);
        generatedCode.Should().Contain("[System.Obsolete]");
    }

    [TestMethod]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
    public async Task Can_Generate_Code_Without_Obsolete_Operations(SampleOpenSpecifications version, string filename)
    {
        var settings = new RefitGeneratorSettings();
        settings.GenerateDeprecatedOperations = false;
        var generatedCode = await GenerateCode(version, filename, settings);
        generatedCode.Should().NotContain(@"/pet/findByTags");
    }

    [TestMethod]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
    public async Task Can_Generate_Code_With_Operation_Name_Template(SampleOpenSpecifications version, string filename)
    {
        var settings = new RefitGeneratorSettings();
        settings.OperationNameTemplate = "{operationName}Async";
        var generatedCode = await GenerateCode(version, filename, settings);
        generatedCode.Should().Contain("FindPetsByStatusAsync");
    }

    [TestMethod]
    [DataRow(OperationNameGeneratorTypes.Default)]
    [DataRow(OperationNameGeneratorTypes.MultipleClientsFromOperationId)]
    [DataRow(OperationNameGeneratorTypes.MultipleClientsFromPathSegments)]
    [DataRow(OperationNameGeneratorTypes.MultipleClientsFromFirstTagAndOperationName)]
    [DataRow(OperationNameGeneratorTypes.MultipleClientsFromFirstTagAndPathSegments)]
    [DataRow(OperationNameGeneratorTypes.MultipleClientsFromFirstTagAndOperationId)]
    [DataRow(OperationNameGeneratorTypes.SingleClientFromOperationId)]
    [DataRow(OperationNameGeneratorTypes.SingleClientFromPathSegments)]
    public async Task Can_Generate_Code_With_OperationNameGenerator(OperationNameGeneratorTypes type)
    {
        var version = SampleOpenSpecifications.SwaggerPetstoreJsonV3;
        var filename = "SwaggerPetstore.json";
        var settings = new RefitGeneratorSettings
        {
            OperationNameGenerator = type
        };
        var generatedCode = await GenerateCode(version, filename, settings);
        BuildHelper
            .BuildCSharp(generatedCode)
            .Should()
            .BeTrue();
    }

    [TestMethod]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
    public async Task Can_Generate_Code_Without_AdditionalProperties(SampleOpenSpecifications version, string filename)
    {
        var settings = new RefitGeneratorSettings();
        settings.GenerateDefaultAdditionalProperties = false;
        settings.OperationNameTemplate = "{operationName}Async";
        var generatedCode = await GenerateCode(version, filename, settings);
        generatedCode.Should().NotContain("Dictionary<string, object> AdditionalProperties");
    }

    [TestMethod]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
    public async Task Can_Generate_Code_With_NonNullable_Return_Types(SampleOpenSpecifications version, string filename)
    {
        var settings = new RefitGeneratorSettings();
        settings.CodeGeneratorSettings = new CodeGeneratorSettings
        {
            GenerateNullableReferenceTypes = true,
            GenerateOptionalPropertiesAsNullable = true
        };
        var generatedCode = await GenerateCode(version, filename, settings);
        generatedCode.Should().Contain("Task<Pet>");
        generatedCode.Should().NotContain("Task<Pet?>");
    }

    [TestMethod]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
    public async Task Can_Generate_Code_With_NonNullable_Return_Types_In_ApiResponse(SampleOpenSpecifications version, string filename)
    {
        var settings = new RefitGeneratorSettings();
        settings.ReturnIApiResponse = true;
        settings.CodeGeneratorSettings = new CodeGeneratorSettings
        {
            GenerateNullableReferenceTypes = true,
            GenerateOptionalPropertiesAsNullable = true
        };
        var generatedCode = await GenerateCode(version, filename, settings);
        generatedCode.Should().Contain("Task<IApiResponse<Pet>>");
        generatedCode.Should().NotContain("Task<IApiResponse<Pet?>>");
    }

    [TestMethod]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
    public async Task Can_Generate_Code_With_ImmutableRecords(SampleOpenSpecifications version, string filename)
    {
        var settings = new RefitGeneratorSettings { ImmutableRecords = true };
        var generatedCode = await GenerateCode(version, filename, settings);
        generatedCode.Should().Contain("record Pet");
    }

    [TestMethod]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
    public async Task Can_Generate_Code_With_DynamicQuerystringParameters(SampleOpenSpecifications version, string filename)
    {
        var settings = new RefitGeneratorSettings { UseDynamicQuerystringParameters = true };
        var generatedCode = await GenerateCode(version, filename, settings);
        if (version is SampleOpenSpecifications.SwaggerPetstoreJsonV3 or SampleOpenSpecifications.SwaggerPetstoreYamlV3)
            generatedCode.Should().Contain("long petId, [Query] UpdatePetWithFormQueryParams queryParams);")
                .And.Contain("public class UpdatePetWithFormQueryParams");

        generatedCode.Should().Contain("[Query] LoginUserQueryParams queryParams);")
            .And.Contain("public class LoginUserQueryParams");
    }

    [TestMethod]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
    public async Task Can_Generate_Code_With_IDisposable(SampleOpenSpecifications version, string filename)
    {
        var settings = new RefitGeneratorSettings { GenerateDisposableClients = true };
        var generatedCode = await GenerateCode(version, filename, settings);
        generatedCode.Should().Contain("IDisposable");
    }

    [TestMethod]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
#if !DEBUG
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
#endif
    public async Task Can_Build_Generated_Code_With_IDisposable(SampleOpenSpecifications version, string filename)
    {
        var settings = new RefitGeneratorSettings();
        settings.GenerateDisposableClients = true;
        var generatedCode = await GenerateCode(version, filename, settings);
        BuildHelper
            .BuildCSharp(generatedCode)
            .Should()
            .BeTrue();
    }

    [TestMethod]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
    public async Task Can_Generate_Interface_With_Summary(SampleOpenSpecifications version, string filename)
    {
        var settings = new RefitGeneratorSettings { GenerateXmlDocCodeComments = true };
        var generatedCode = await GenerateCode(version, filename, settings);
        generatedCode.Should().Contain("<summary>Swagger Petstore");
    }

    [TestMethod]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
    public async Task Can_Generate_Interfaces_Filtered_By_Path_Match(SampleOpenSpecifications version, string filename)
    {
        var settings = new RefitGeneratorSettings { IncludePathMatches = ["^/pet.*"] };
        var generatedCode = await GenerateCode(version, filename, settings);
        generatedCode.Should().Contain("\"/pet\"");
        generatedCode.Should().Contain("\"/pet/{petId}\"");
    }

    [TestMethod]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
#if !DEBUG
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
#endif
    public async Task Can_Import_Contracts_Namespace(SampleOpenSpecifications version, string filename)
    {
        var settings = new RefitGeneratorSettings { ContractsNamespace = "Contracts" };
        var generatedCode = await GenerateCode(version, filename, settings);
        generatedCode.Should().Contain("using Contracts;");
        BuildHelper
            .BuildCSharp(generatedCode)
            .Should()
            .BeTrue();
    }
}
