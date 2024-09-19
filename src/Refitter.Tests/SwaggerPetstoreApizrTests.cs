using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.Resources;
using Xunit;

namespace Refitter.Tests;

public class SwaggerPetstoreApizrTests
{
    private class ApizrGeneratorSettings : RefitGeneratorSettings
    {
        public ApizrGeneratorSettings()
        {
            DependencyInjectionSettings = new DependencyInjectionSettings
            {
                BaseUrl = "https://petstore3.swagger.io/api/v3",
                TransientErrorHandler = TransientErrorHandler.HttpResilience
            };
            ApizrSettings = new ApizrSettings
            {
                WithRequestOptions = true,
                WithRegistrationHelper = true,
                WithCacheProvider = CacheProviderType.InMemory,
                WithPriority = true,
                WithMediation = true,
                WithOptionalMediation = true,
                WithMappingProvider = MappingProviderType.AutoMapper,
                WithFileTransfer = true
            };
        }
    }

    [Theory]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
    public async Task Can_Generate_Code(SampleOpenSpecifications version, string filename)
    {
        var generateCode = await GenerateCode(version, filename);
        generateCode.Should().NotBeNullOrWhiteSpace();
    }

    [Theory]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
    public async Task Can_Generate_Partial_Interfaces(SampleOpenSpecifications version, string filename)
    {
        var generateCode = await GenerateCode(version, filename);
        generateCode.Should().Contain("partial interface");
    }

    [Theory]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
    public async Task Can_Generate_Code_Without_Contracts(SampleOpenSpecifications version, string filename)
    {
        var generateCode = await GenerateCode(
            version,
            filename,
            new ApizrGeneratorSettings { GenerateXmlDocCodeComments = false, GenerateContracts = false });
        generateCode.Should().NotContain("class Pet");
    }

    [Theory]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
    public async Task Can_Generate_Code_With_XmlDoc_Code_Comments(SampleOpenSpecifications version, string filename)
    {
        var generateCode = await GenerateCode(
            version,
            filename,
            new ApizrGeneratorSettings { GenerateXmlDocCodeComments = true, GenerateContracts = false });
        generateCode.Should().Contain("<summary>");
    }

    [Theory]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
    public async Task Can_Generate_Code_Without_XmlDoc_Code_Comments(SampleOpenSpecifications version, string filename)
    {
        var generateCode = await GenerateCode(
            version,
            filename,
            new ApizrGeneratorSettings { GenerateXmlDocCodeComments = false, GenerateContracts = false });
        generateCode.Should().NotContain("<summary>");
    }

    [Theory]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
    public async Task Can_Generate_Code_With_Default_Namespace(SampleOpenSpecifications version, string filename)
    {
        var generateCode = await GenerateCode(
            version,
            filename,
            new ApizrGeneratorSettings { Namespace = "Some.Other.Namespace" });
        generateCode.Should().Contain("namespace Some.Other.Namespace");
    }

    [Theory]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
    public async Task Can_Generate_Code_With_Fixed_Interface_Name(SampleOpenSpecifications version, string filename)
    {
        var settings = new ApizrGeneratorSettings {Naming = {UseOpenApiTitle = false, InterfaceName = "SomeOtherName"}};
        var generateCode = await GenerateCode(version, filename, settings);
        generateCode.Should().Contain("interface ISomeOtherName");
    }

    [Theory]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
    public async Task Can_Generate_Code_Without_AutoGenerated_Header(SampleOpenSpecifications version, string filename)
    {
        var settings = new ApizrGeneratorSettings {AddAutoGeneratedHeader = false};
        var generateCode = await GenerateCode(version, filename, settings);
        generateCode.Should().NotContain("This code was generated by Refitter");
    }

    [Theory]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
    public async Task Can_Generate_Code_That_Returns_IApiResponse(SampleOpenSpecifications version, string filename)
    {
        var settings = new ApizrGeneratorSettings {ReturnIApiResponse = true};
        var generateCode = await GenerateCode(version, filename, settings);
        generateCode.Should().Contain("Task<IApiResponse<Pet>>");
    }

    [Theory]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
    public async Task Can_Generate_Code_With_Type_Override(SampleOpenSpecifications version, string filename)
    {
        var settings = new ApizrGeneratorSettings
        {
            ReturnIApiResponse = false,
            ResponseTypeOverride =
            {
                ["getPetById"] = "IApiResponse<Pet>", // Wrap existing type
                ["deletePet"] = "Pet", // Add type where there was none
                ["addPet"] = "void", // Remove type
            },
        };
        var generateCode = await GenerateCode(version, filename, settings);
        generateCode.Should().Contain("Task<IApiResponse<Pet>> GetPetById");
        generateCode.Should().Contain("Task<Pet> DeletePet");
        generateCode.Should().Contain("Task AddPet");
    }

    [Theory]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
    public async Task Can_Generate_Code_With_Internal_Contract_Types(SampleOpenSpecifications version, string filename)
    {
        var settings = new ApizrGeneratorSettings {TypeAccessibility = TypeAccessibility.Internal};
        var generateCode = await GenerateCode(version, filename, settings);
        generateCode.Should().Contain("internal partial class");
    }

    [Theory]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
    public async Task Can_Generate_Code_With_Internal_Interface(SampleOpenSpecifications version, string filename)
    {
        var settings = new ApizrGeneratorSettings {TypeAccessibility = TypeAccessibility.Internal};
        var generateCode = await GenerateCode(version, filename, settings);
        generateCode.Should().Contain("internal partial interface");
    }

    [Theory]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
    public async Task Can_Generate_Code_Without_CancellationToken(SampleOpenSpecifications version, string filename)
    {
        var settings = new ApizrGeneratorSettings {UseCancellationTokens = true};
        var generateCode = await GenerateCode(version, filename, settings);
        generateCode.Should().NotContain("CancellationToken cancellationToken = default"); // Handled by Apizr request options
    }

    [Theory]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json", true)]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml", true)]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json", true)]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml", true)]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json", false)]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml", false)]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json", false)]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml", false)]
    public async Task Can_Generate_Code_With_Correct_Usings(
        SampleOpenSpecifications version,
        string filename,
        bool cancellationTokens)
    {
        var settings = new ApizrGeneratorSettings {UseCancellationTokens = cancellationTokens};
        var generateCode = await GenerateCode(version, filename, settings);
        generateCode.Should().NotContain("CancellationToken cancellationToken = default"); // Handled by Apizr request options
    }

    [Theory]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreJsonV3WithDifferentHeaders, "SwaggerPetstoreWithDifferentHeaders.json")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreYamlV3WithDifferentHeaders, "SwaggerPetstoreWithDifferentHeaders.yaml")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreJsonV2WithDifferentHeaders, "SwaggerPetstoreWithDifferentHeaders.json")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreYamlV2WithDifferentHeaders, "SwaggerPetstoreWithDifferentHeaders.yaml")]
    public async Task Can_Generate_Code_With_OperationHeaders_With_Different_Headers(SampleOpenSpecifications version, string filename)
    {
        var settings = new ApizrGeneratorSettings {GenerateOperationHeaders = true};
        var generateCode = await GenerateCode(version, filename, settings);
        generateCode.Should().Contain("[Header(\"api-key\")] string api_key");
    }

    [Theory]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
    public async Task Can_Generate_Code_With_OperationHeaders(SampleOpenSpecifications version, string filename)
    {
        var settings = new ApizrGeneratorSettings {GenerateOperationHeaders = true};
        var generateCode = await GenerateCode(version, filename, settings);
        generateCode.Should().Contain("[Header(\"api_key\")] string api_key");
    }

    [Theory]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
    public async Task Can_Generate_Code_Without_OperationHeaders(SampleOpenSpecifications version, string filename)
    {
        var settings = new ApizrGeneratorSettings {GenerateOperationHeaders = false};
        var generateCode = await GenerateCode(version, filename, settings);
        generateCode.Should().NotContain("[Header(\"api_key\")] string? api_key");
    }

    [Theory]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
    public async Task Can_Generate_Code_With_Accept_Request_Header(SampleOpenSpecifications version, string filename)
    {
        var settings = new ApizrGeneratorSettings();
        var generateCode = await GenerateCode(version, filename, settings);
        if (version is SampleOpenSpecifications.SwaggerPetstoreJsonV3 or SampleOpenSpecifications.SwaggerPetstoreYamlV3)
            generateCode.Should().Contain("[Headers(\"Accept: application/json\")]");
        else if (version is SampleOpenSpecifications.SwaggerPetstoreJsonV2 or SampleOpenSpecifications.SwaggerPetstoreYamlV2)
            generateCode.Should().NotContain("[Headers(\"Accept: application/json\")]");
    }

    [Theory]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    public async Task Can_Generate_Code_With_No_Accept_Request_Header(SampleOpenSpecifications version, string filename)
    {
        var settings = new ApizrGeneratorSettings {AddAcceptHeaders = false};
        var generateCode = await GenerateCode(version, filename, settings);
        generateCode.Should().NotContain("[Headers(\"Accept");
    }

    [Theory]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
    public async Task Can_Generate_Code_With_Multiple_Interfaces(SampleOpenSpecifications version, string filename)
    {
        var settings = new ApizrGeneratorSettings {MultipleInterfaces = MultipleInterfaces.ByEndpoint};
        var generateCode = await GenerateCode(version, filename, settings);
        generateCode.Should().NotBeNullOrWhiteSpace();
    }

    [Theory]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
    public async Task Can_Generate_Code_Dependency_Injection_Setup(SampleOpenSpecifications version, string filename)
    {
        var settings = new ApizrGeneratorSettings
        {
            DependencyInjectionSettings = new DependencyInjectionSettings
            {
                BaseUrl = "https://petstore3.swagger.io/api/v3",
                TransientErrorHandler = TransientErrorHandler.Polly
            }
        };
        var generateCode = await GenerateCode(version, filename, settings);
        generateCode.Should().Contain("AddApizrManagerFor");
    }

    [Theory]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
    public async Task Can_Generate_Code_Dependency_Injection_Setup_With_Polly(SampleOpenSpecifications version, string filename)
    {
        var settings = new ApizrGeneratorSettings
        {
            DependencyInjectionSettings = new DependencyInjectionSettings
            {
                BaseUrl = "https://petstore3.swagger.io/api/v3",
                TransientErrorHandler = TransientErrorHandler.Polly
            }
        };
        var generateCode = await GenerateCode(version, filename, settings);
        generateCode.Should().Contain("using Polly");
        generateCode.Should().Contain("AddPolicyHandler");
        generateCode.Should().Contain("Backoff.DecorrelatedJitterBackoffV2");
    }

    [Theory]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
    public async Task Can_Generate_Code_Dependency_Injection_Setup_Without_Polly(SampleOpenSpecifications version, string filename)
    {
        var settings = new ApizrGeneratorSettings
        {
            DependencyInjectionSettings = new DependencyInjectionSettings
            {
                BaseUrl = "https://petstore3.swagger.io/api/v3",
                TransientErrorHandler = TransientErrorHandler.None
            }
        };
        var generateCode = await GenerateCode(version, filename, settings);
        generateCode.Should().NotContain("using Polly");
        generateCode.Should().NotContain("AddPolicyHandler");
        generateCode.Should().NotContain("Backoff.DecorrelatedJitterBackoffV2");
    }

    [Theory]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json", MultipleInterfaces.Unset)]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml", MultipleInterfaces.Unset)]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json", MultipleInterfaces.Unset)]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml", MultipleInterfaces.Unset)]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json", MultipleInterfaces.ByEndpoint)]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml", MultipleInterfaces.ByEndpoint)]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json", MultipleInterfaces.ByEndpoint)]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml", MultipleInterfaces.ByEndpoint)]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json", MultipleInterfaces.ByTag)]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml", MultipleInterfaces.ByTag)]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json", MultipleInterfaces.ByTag)]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml", MultipleInterfaces.ByTag)]
    public async Task Can_Generate_Code_GeneratedCode_Attribute(
        SampleOpenSpecifications version,
        string filename,
        MultipleInterfaces multipleInterfaces)
    {
        var settings = new ApizrGeneratorSettings {MultipleInterfaces = multipleInterfaces};
        var generateCode = await GenerateCode(version, filename, settings);
        generateCode.Should().Contain("[System.CodeDom.Compiler.GeneratedCode(\"Refitter\"");
    }

    [Theory]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json", MultipleInterfaces.ByEndpoint)]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml", MultipleInterfaces.ByEndpoint)]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json", MultipleInterfaces.ByEndpoint)]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml", MultipleInterfaces.ByEndpoint)]
    public async Task Can_Generate_Code_With_Multiple_Interfaces_And_OperationNameTemplate(
        SampleOpenSpecifications version,
        string filename,
        MultipleInterfaces multipleInterfaces)
    {
        var settings = new ApizrGeneratorSettings
        {
            MultipleInterfaces = multipleInterfaces, OperationNameTemplate = "ExecuteAsync"
        };
        var generateCode = await GenerateCode(version, filename, settings);
        generateCode.Should().Contain("ExecuteAsync(");
    }

    [Theory]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
#if !DEBUG
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
#endif
    public async Task Can_Build_Generated_Code(SampleOpenSpecifications version, string filename)
    {
        var generateCode = await GenerateCode(version, filename);
        BuildHelper
            .BuildCSharp(generateCode)
            .Should()
            .BeTrue();
    }

    [Theory]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
#if !DEBUG
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
#endif
    public async Task Can_Build_Generated_Code_With_IApiResponse(SampleOpenSpecifications version, string filename)
    {
        var settings = new ApizrGeneratorSettings {ReturnIApiResponse = true};
        var generateCode = await GenerateCode(version, filename, settings);
        BuildHelper
            .BuildCSharp(generateCode)
            .Should()
            .BeTrue();
    }

    [Theory]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    public async Task Can_Build_Generated_Code_With_IObservableResponse(SampleOpenSpecifications version, string filename)
    {
        var settings = new ApizrGeneratorSettings {ReturnIObservable = true};
        var generateCode = await GenerateCode(version, filename, settings);

        BuildHelper
            .BuildCSharp(generateCode)
            .Should()
            .BeTrue();
    }

    [Theory]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
#if !DEBUG
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
#endif
    public async Task Can_Build_Generated_Code_With_Multiple_Interfaces(SampleOpenSpecifications version, string filename)
    {
        var settings = new ApizrGeneratorSettings {MultipleInterfaces = MultipleInterfaces.ByEndpoint};
        var generateCode = await GenerateCode(version, filename, settings);
        BuildHelper
            .BuildCSharp(generateCode)
            .Should()
            .BeTrue();
    }

    [Theory]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
#if !DEBUG
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
#endif
    public async Task Can_Build_Generated_Code_With_Multiple_Interfaces_ByTag(SampleOpenSpecifications version, string filename)
    {
        var settings = new ApizrGeneratorSettings {MultipleInterfaces = MultipleInterfaces.ByTag};
        var generateCode = await GenerateCode(version, filename, settings);
        BuildHelper
            .BuildCSharp(generateCode)
            .Should()
            .BeTrue();
    }

#if !DEBUG
    [Theory]
    [InlineData("http://raw.githubusercontent.com/christianhelle/refitter/main/test/OpenAPI/v3.0/petstore.json")]
    [InlineData("http://raw.githubusercontent.com/christianhelle/refitter/main/test/OpenAPI/v3.0/petstore.yaml")]
    [InlineData("https://raw.githubusercontent.com/christianhelle/refitter/main/test/OpenAPI/v3.0/petstore.json")]
    [InlineData("https://raw.githubusercontent.com/christianhelle/refitter/main/test/OpenAPI/v3.0/petstore.yaml")]
    public async Task Can_Build_Generated_Code_From_Url(string url)
    {
        var settings = new ApizrGeneratorSettings { OpenApiPath = url };
        var sut = await RefitGenerator.CreateAsync(settings);
        var generateCode = sut.Generate();
        BuildHelper
            .BuildCSharp(generateCode)
            .Should()
            .BeTrue();
    }
#endif

    [Theory]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
    public async Task Can_Generate_Code_Obsolete_Attribute(SampleOpenSpecifications version, string filename)
    {
        var generateCode = await GenerateCode(version, filename);
        generateCode.Should().Contain("[System.Obsolete]");
    }

    [Theory]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
    public async Task Can_Generate_Code_Without_Obsolete_Operations(SampleOpenSpecifications version, string filename)
    {
        var settings = new ApizrGeneratorSettings {GenerateDeprecatedOperations = false};
        var generateCode = await GenerateCode(version, filename, settings);
        generateCode.Should().NotContain(@"/pet/findByTags");
    }

    [Theory]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
    public async Task Can_Generate_Code_With_Operation_Name_Template(SampleOpenSpecifications version, string filename)
    {
        var settings = new ApizrGeneratorSettings {OperationNameTemplate = "{operationName}Async"};
        var generateCode = await GenerateCode(version, filename, settings);
        generateCode.Should().Contain("FindPetsByStatusAsync");
    }

    [Theory]
    [InlineData(OperationNameGeneratorTypes.Default)]
    [InlineData(OperationNameGeneratorTypes.MultipleClientsFromOperationId)]
    [InlineData(OperationNameGeneratorTypes.MultipleClientsFromPathSegments)]
    [InlineData(OperationNameGeneratorTypes.MultipleClientsFromFirstTagAndOperationName)]
    [InlineData(OperationNameGeneratorTypes.MultipleClientsFromFirstTagAndPathSegments)]
    [InlineData(OperationNameGeneratorTypes.MultipleClientsFromFirstTagAndOperationId)]
    [InlineData(OperationNameGeneratorTypes.SingleClientFromOperationId)]
    [InlineData(OperationNameGeneratorTypes.SingleClientFromPathSegments)]
    public async Task Can_Generate_Code_With_OperationNameGenerator(OperationNameGeneratorTypes type)
    {
        var version = SampleOpenSpecifications.SwaggerPetstoreJsonV3;
        var filename = "SwaggerPetstore.json";
        var settings = new ApizrGeneratorSettings
        {
            OperationNameGenerator = type
        };
        var generateCode = await GenerateCode(version, filename, settings);
        BuildHelper
            .BuildCSharp(generateCode)
            .Should()
            .BeTrue();
    }

    [Theory]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
    public async Task Can_Generate_Code_Without_AdditionalProperties(SampleOpenSpecifications version, string filename)
    {
        var settings = new ApizrGeneratorSettings
        {
            GenerateDefaultAdditionalProperties = false, OperationNameTemplate = "{operationName}Async"
        };
        var generateCode = await GenerateCode(version, filename, settings);
        generateCode.Should().NotContain("Dictionary<string, object> AdditionalProperties");
    }

    [Theory]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
    public async Task Can_Generate_Code_With_NonNullable_Return_Types(SampleOpenSpecifications version, string filename)
    {
        var settings = new ApizrGeneratorSettings
        {
            CodeGeneratorSettings = new CodeGeneratorSettings
            {
                GenerateNullableReferenceTypes = true, GenerateOptionalPropertiesAsNullable = true
            }
        };
        var generateCode = await GenerateCode(version, filename, settings);
        generateCode.Should().Contain("Task<Pet>");
        generateCode.Should().NotContain("Task<Pet?>");
    }

    [Theory]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
    public async Task Can_Generate_Code_With_NonNullable_Return_Types_In_ApiResponse(SampleOpenSpecifications version, string filename)
    {
        var settings = new ApizrGeneratorSettings {ReturnIApiResponse = true, CodeGeneratorSettings = new CodeGeneratorSettings
            {
                GenerateNullableReferenceTypes = true,
                GenerateOptionalPropertiesAsNullable = true
            }
        };
        var generateCode = await GenerateCode(version, filename, settings);
        generateCode.Should().Contain("Task<IApiResponse<Pet>>");
        generateCode.Should().NotContain("Task<IApiResponse<Pet?>>");
    }

    [Theory]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
    public async Task Can_Generate_Code_With_ImmutableRecords(SampleOpenSpecifications version, string filename)
    {
        var settings = new ApizrGeneratorSettings { ImmutableRecords = true };
        var generateCode = await GenerateCode(version, filename, settings);
        generateCode.Should().Contain("record Pet");
    }

    [Theory]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [InlineData(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
    public async Task Can_Generate_Code_With_DynamicQuerystringParameter(SampleOpenSpecifications version, string filename)
    {
        var settings = new ApizrGeneratorSettings { UseDynamicQuerystringParameters = true };
        var generateCode = await GenerateCode(version, filename, settings);

        if(version is SampleOpenSpecifications.SwaggerPetstoreJsonV3 or SampleOpenSpecifications.SwaggerPetstoreYamlV3)
            generateCode.Should().Contain("long petId, [Query] UpdatePetWithFormQueryParams queryParams, [RequestOptions] IApizrRequestOptions options);")
                .And.Contain("public class UpdatePetWithFormQueryParams");

        generateCode.Should().Contain("[Query] LoginUserQueryParams queryParams, [RequestOptions] IApizrRequestOptions options);")
            .And.Contain("public class LoginUserQueryParams");
    }

    private static async Task<string> GenerateCode(
        SampleOpenSpecifications version,
        string filename,
        ApizrGeneratorSettings? settings = null)
    {
        var swaggerFile = await TestFile.CreateSwaggerFile(EmbeddedResources.GetSwaggerPetstore(version), filename);
        if (settings is null)
        {
            settings = new ApizrGeneratorSettings
            {
                OpenApiPath = swaggerFile
            };
        }
        else
        {
            settings.OpenApiPath = swaggerFile;
        }

        var sut = await RefitGenerator.CreateAsync(settings);
        return sut.Generate();
    }
}