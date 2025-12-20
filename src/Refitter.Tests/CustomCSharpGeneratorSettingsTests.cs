using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Resources;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests;

public class CustomCSharpGeneratorSettingsTests
{
    [Test]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
    public async Task Can_Generate_Default_DateType(
        SampleOpenSpecifications version,
        string filename)
    {
        var settings = new RefitGeneratorSettings();
        settings.CodeGeneratorSettings = new CodeGeneratorSettings();
        var generatedCode = await GenerateCode(version, filename, settings);
        generatedCode.Should().NotBeNullOrWhiteSpace();
        generatedCode.Should().Contain(settings.CodeGeneratorSettings!.DateType);
    }

    [Test]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
    public async Task Can_Generate_Default_DateTimeType(
        SampleOpenSpecifications version,
        string filename)
    {
        var settings = new RefitGeneratorSettings();
        settings.CodeGeneratorSettings = new CodeGeneratorSettings();
        var generatedCode = await GenerateCode(version, filename, settings);
        generatedCode.Should().NotBeNullOrWhiteSpace();
        generatedCode.Should().Contain(settings.CodeGeneratorSettings!.DateTimeType);
    }

    [Test]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
    public async Task Can_Generate_Default_ArrayType(
        SampleOpenSpecifications version,
        string filename)
    {
        var settings = new RefitGeneratorSettings();
        settings.CodeGeneratorSettings = new CodeGeneratorSettings();
        var generatedCode = await GenerateCode(version, filename, settings);
        generatedCode.Should().NotBeNullOrWhiteSpace();
        generatedCode.Should().Contain("ICollection<");
    }

    [Test]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
    public async Task Can_Generate_Custom_DateType(
        SampleOpenSpecifications version,
        string filename)
    {
        var settings = new RefitGeneratorSettings();
        settings.CodeGeneratorSettings = new CodeGeneratorSettings();
        settings.CodeGeneratorSettings!.DateType = "DateTime";
        var generatedCode = await GenerateCode(version, filename, settings);
        generatedCode.Should().NotBeNullOrWhiteSpace();
        generatedCode.Should().Contain("DateTime");
    }

    [Test]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
    public async Task Can_Generate_Custom_DateTimeType(
        SampleOpenSpecifications version,
        string filename)
    {
        var settings = new RefitGeneratorSettings();
        settings.CodeGeneratorSettings = new CodeGeneratorSettings();
        settings.CodeGeneratorSettings!.DateTimeType = "DateTime";
        var generatedCode = await GenerateCode(version, filename, settings);
        generatedCode.Should().NotBeNullOrWhiteSpace();
        generatedCode.Should().Contain("DateTime");
    }

    [Test]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
    public async Task Can_Generate_Custom_ArrayType(
        SampleOpenSpecifications version,
        string filename)
    {
        var settings = new RefitGeneratorSettings();
        settings.CodeGeneratorSettings = new CodeGeneratorSettings();
        settings.CodeGeneratorSettings!.ArrayType = "System.Collection.Generic.IList";
        var generatedCode = await GenerateCode(version, filename, settings);
        generatedCode.Should().NotBeNullOrWhiteSpace();
        generatedCode.Should().Contain("System.Collection.Generic.IList<");
    }

    [Test]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
    public async Task Can_Generate_With_ExcludedTypeNames(
        SampleOpenSpecifications version,
        string filename)
    {
        var settings = new RefitGeneratorSettings();
        settings.CodeGeneratorSettings = new CodeGeneratorSettings();
        settings.CodeGeneratorSettings.ExcludedTypeNames = new[]
        {
            "User"
        };
        var generatedCode = await GenerateCode(version, filename, settings);
        generatedCode.Should().NotBeNullOrWhiteSpace();
        generatedCode.Should().NotContain("class User");
    }

    [Test]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
    public async Task Can_Generate_With_Immutable_Records(SampleOpenSpecifications version, string filename)
    {
        var settings = new RefitGeneratorSettings();
        settings.ReturnIApiResponse = true;
        settings.CodeGeneratorSettings = new CodeGeneratorSettings { GenerateNativeRecords = true, };
        var generatedCode = await GenerateCode(version, filename, settings);
        generatedCode.Should().Contain("record Pet");
        generatedCode.Should().Contain("Pet(");
        generatedCode.Should().Contain("[JsonConstructor]");
    }

    [Test]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
    public async Task Can_Generate_With_CustomTemplates(SampleOpenSpecifications version, string filename)
    {
        var settings = new RefitGeneratorSettings();
        settings.ReturnIApiResponse = true;
        settings.CustomTemplateDirectory = "./Templates/";
        var generatedCode = await GenerateCode(version, filename, settings);
        generatedCode.Should().Contain("/* Example Custom Template Texte */");
        generatedCode.Should().Contain("public partial class Pet");
    }


    private static async Task<string> GenerateCode(
        SampleOpenSpecifications version,
        string filename,
        RefitGeneratorSettings settings)
    {
        var swaggerFile = await TestFile.CreateSwaggerFile(EmbeddedResources.GetSwaggerPetstore(version), filename);
        settings.OpenApiPath = swaggerFile;

        var sut = await RefitGenerator.CreateAsync(settings);
        return sut.Generate();
    }
}
