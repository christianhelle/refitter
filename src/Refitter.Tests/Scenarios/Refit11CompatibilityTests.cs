using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.Resources;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Scenarios;


public class Refit11CompatibilityTests
{
    private const string Refit11TargetFramework = "net10.0";

    [Test]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    public async Task Can_Build_Generated_Code_With_Refit_11(SampleOpenSpecifications version, string filename)
    {
        var generatedCode = await GenerateCode(version, filename);
        BuildHelper
            .BuildCSharp(Refit11TargetFramework, generatedCode)
            .Should()
            .BeTrue();
    }

    [Test]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    public async Task Can_Build_Generated_Code_With_IApiResponse_Using_Refit_11(SampleOpenSpecifications version, string filename)
    {
        var settings = new RefitGeneratorSettings
        {
            ReturnIApiResponse = true
        };
        var generatedCode = await GenerateCode(version, filename, settings);
        BuildHelper
            .BuildCSharp(Refit11TargetFramework, generatedCode)
            .Should()
            .BeTrue();
    }

    [Test]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    public async Task Can_Build_Generated_Code_With_IObservableResponse_Using_Refit_11(SampleOpenSpecifications version, string filename)
    {
        var settings = new RefitGeneratorSettings
        {
            ReturnIObservable = true
        };
        var generatedCode = await GenerateCode(version, filename, settings);
        BuildHelper
            .BuildCSharp(Refit11TargetFramework, generatedCode)
            .Should()
            .BeTrue();
    }

    [Test]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    public async Task Can_Build_Generated_Code_With_Multiple_Interfaces_Using_Refit_11(SampleOpenSpecifications version, string filename)
    {
        var settings = new RefitGeneratorSettings
        {
            MultipleInterfaces = MultipleInterfaces.ByEndpoint
        };
        var generatedCode = await GenerateCode(version, filename, settings);
        BuildHelper
            .BuildCSharp(Refit11TargetFramework, generatedCode)
            .Should()
            .BeTrue();
    }

    [Test]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    public async Task Can_Build_Generated_Code_With_Multiple_Interfaces_ByTag_Using_Refit_11(SampleOpenSpecifications version, string filename)
    {
        var settings = new RefitGeneratorSettings
        {
            MultipleInterfaces = MultipleInterfaces.ByTag
        };
        var generatedCode = await GenerateCode(version, filename, settings);
        BuildHelper
            .BuildCSharp(Refit11TargetFramework, generatedCode)
            .Should()
            .BeTrue();
    }

    [Test]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    public async Task Can_Build_Generated_Code_With_Dependency_Injection_Using_Refit_11(SampleOpenSpecifications version, string filename)
    {
        var settings = new RefitGeneratorSettings
        {
            DependencyInjectionSettings = new DependencyInjectionSettings
            {
                BaseUrl = "https://petstore3.swagger.io/api/v3"
            }
        };
        var generatedCode = await GenerateCode(version, filename, settings);
        BuildHelper
            .BuildCSharp(Refit11TargetFramework, generatedCode)
            .Should()
            .BeTrue();
    }

    [Test]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    public async Task Can_Build_Generated_Code_With_Polly_Error_Handler_Using_Refit_11(SampleOpenSpecifications version, string filename)
    {
        var settings = new RefitGeneratorSettings
        {
            DependencyInjectionSettings = new DependencyInjectionSettings
            {
                BaseUrl = "https://petstore3.swagger.io/api/v3",
                TransientErrorHandler = TransientErrorHandler.Polly
            }
        };
        var generatedCode = await GenerateCode(version, filename, settings);
        BuildHelper
            .BuildCSharp(Refit11TargetFramework, generatedCode)
            .Should()
            .BeTrue();
    }

    [Test]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    public async Task Can_Build_Generated_Code_With_Microsoft_Http_Resilience_Using_Refit_11(SampleOpenSpecifications version, string filename)
    {
        var settings = new RefitGeneratorSettings
        {
            DependencyInjectionSettings = new DependencyInjectionSettings
            {
                BaseUrl = "https://petstore3.swagger.io/api/v3",
                TransientErrorHandler = TransientErrorHandler.HttpResilience
            }
        };
        var generatedCode = await GenerateCode(version, filename, settings);
        BuildHelper
            .BuildCSharp(Refit11TargetFramework, generatedCode)
            .Should()
            .BeTrue();
    }

    [Test]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    public async Task Can_Build_Generated_Code_With_Cancellation_Tokens_Using_Refit_11(SampleOpenSpecifications version, string filename)
    {
        var settings = new RefitGeneratorSettings
        {
            UseCancellationTokens = true
        };
        var generatedCode = await GenerateCode(version, filename, settings);
        BuildHelper
            .BuildCSharp(Refit11TargetFramework, generatedCode)
            .Should()
            .BeTrue();
    }

    [Test]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    public async Task Can_Build_Generated_Code_With_Operation_Headers_Using_Refit_11(SampleOpenSpecifications version, string filename)
    {
        var settings = new RefitGeneratorSettings
        {
            GenerateOperationHeaders = true
        };
        var generatedCode = await GenerateCode(version, filename, settings);
        BuildHelper
            .BuildCSharp(Refit11TargetFramework, generatedCode)
            .Should()
            .BeTrue();
    }

    [Test]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreJsonV3WithBearerAuthenticationHeaders, "SwaggerPetstoreWithBearerAuthenticationHeaders.json")]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreYamlV3WithBearerAuthenticationHeaders, "SwaggerPetstoreWithBearerAuthenticationHeaders.yaml")]
    public async Task Can_Build_Generated_Code_With_Bearer_Authentication_Method_Style_Using_Refit_11(SampleOpenSpecifications version, string filename)
    {
        var settings = new RefitGeneratorSettings
        {
            AuthenticationHeaderStyle = AuthenticationHeaderStyle.Method
        };
        var generatedCode = await GenerateCode(version, filename, settings);
        BuildHelper
            .BuildCSharp(Refit11TargetFramework, generatedCode)
            .Should()
            .BeTrue();
    }

    [Test]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreJsonV3WithBearerAuthenticationHeaders, "SwaggerPetstoreWithBearerAuthenticationHeaders.json")]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreYamlV3WithBearerAuthenticationHeaders, "SwaggerPetstoreWithBearerAuthenticationHeaders.yaml")]
    public async Task Can_Build_Generated_Code_With_Bearer_Authentication_Parameter_Style_Using_Refit_11(SampleOpenSpecifications version, string filename)
    {
        var settings = new RefitGeneratorSettings
        {
            AuthenticationHeaderStyle = AuthenticationHeaderStyle.Parameter
        };
        var generatedCode = await GenerateCode(version, filename, settings);
        BuildHelper
            .BuildCSharp(Refit11TargetFramework, generatedCode)
            .Should()
            .BeTrue();
    }

    [Test]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    public async Task Can_Build_Generated_Code_With_Immutable_Records_Using_Refit_11(SampleOpenSpecifications version, string filename)
    {
        var settings = new RefitGeneratorSettings
        {
            ImmutableRecords = true
        };
        var generatedCode = await GenerateCode(version, filename, settings);
        BuildHelper
            .BuildCSharp(Refit11TargetFramework, generatedCode)
            .Should()
            .BeTrue();
    }

    [Test]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    public async Task Can_Build_Generated_Code_With_Internal_Type_Accessibility_Using_Refit_11(SampleOpenSpecifications version, string filename)
    {
        var settings = new RefitGeneratorSettings
        {
            TypeAccessibility = TypeAccessibility.Internal
        };
        var generatedCode = await GenerateCode(version, filename, settings);
        BuildHelper
            .BuildCSharp(Refit11TargetFramework, generatedCode)
            .Should()
            .BeTrue();
    }

    [Test]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    public async Task Can_Build_Generated_Code_With_NonNullable_Reference_Types_Using_Refit_11(SampleOpenSpecifications version, string filename)
    {
        var settings = new RefitGeneratorSettings
        {
            CodeGeneratorSettings = new CodeGeneratorSettings
            {
                GenerateNullableReferenceTypes = true,
                GenerateOptionalPropertiesAsNullable = true
            }
        };
        var generatedCode = await GenerateCode(version, filename, settings);
        BuildHelper
            .BuildCSharp(Refit11TargetFramework, generatedCode)
            .Should()
            .BeTrue();
    }

    [Test]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    public async Task Can_Build_Generated_Code_With_JsonSerializerContext_Using_Refit_11(SampleOpenSpecifications version, string filename)
    {
        var settings = new RefitGeneratorSettings
        {
            GenerateJsonSerializerContext = true,
            Naming = new NamingSettings
            {
                UseOpenApiTitle = false,
                InterfaceName = "ISwaggerPetstore"
            }
        };
        var generatedCode = await GenerateCode(version, filename, settings);
        BuildHelper
            .BuildCSharp(Refit11TargetFramework, generatedCode)
            .Should()
            .BeTrue();
    }

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
}
