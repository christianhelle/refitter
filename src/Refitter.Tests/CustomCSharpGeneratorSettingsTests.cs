using Atc.Test;
using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Resources;
using Xunit;

namespace Refitter.Tests;

public class CustomCSharpGeneratorSettingsTests
{
    [Theory]
    [InlineAutoNSubstituteData(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [InlineAutoNSubstituteData(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [InlineAutoNSubstituteData(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [InlineAutoNSubstituteData(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
    public async Task Can_Generate_Default_DateType(
        SampleOpenSpecifications version,
        string filename)
    {
        var settings = new RefitGeneratorSettings();
        settings.CodeGeneratorSettings = new CodeGeneratorSettings();
        var generateCode = await GenerateCode(version, filename, settings);
        generateCode.Should().NotBeNullOrWhiteSpace();
        generateCode.Should().Contain(settings.CodeGeneratorSettings!.DateType);
    }

    [Theory]
    [InlineAutoNSubstituteData(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [InlineAutoNSubstituteData(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [InlineAutoNSubstituteData(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [InlineAutoNSubstituteData(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
    public async Task Can_Generate_Default_DateTimeType(
        SampleOpenSpecifications version,
        string filename)
    {
        var settings = new RefitGeneratorSettings();
        settings.CodeGeneratorSettings = new CodeGeneratorSettings();
        var generateCode = await GenerateCode(version, filename, settings);
        generateCode.Should().NotBeNullOrWhiteSpace();
        generateCode.Should().Contain(settings.CodeGeneratorSettings!.DateTimeType);
    }

    [Theory]
    [InlineAutoNSubstituteData(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [InlineAutoNSubstituteData(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [InlineAutoNSubstituteData(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [InlineAutoNSubstituteData(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
    public async Task Can_Generate_Default_ArrayType(
        SampleOpenSpecifications version,
        string filename)
    {
        var settings = new RefitGeneratorSettings();
        settings.CodeGeneratorSettings = new CodeGeneratorSettings();
        var generateCode = await GenerateCode(version, filename, settings);
        generateCode.Should().NotBeNullOrWhiteSpace();
        generateCode.Should().Contain("ICollection<");
    }

    [Theory]
    [InlineAutoNSubstituteData(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [InlineAutoNSubstituteData(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [InlineAutoNSubstituteData(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [InlineAutoNSubstituteData(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
    public async Task Can_Generate_Custom_DateType(
        SampleOpenSpecifications version,
        string filename)
    {
        var settings = new RefitGeneratorSettings();
        settings.CodeGeneratorSettings = new CodeGeneratorSettings();
        settings.CodeGeneratorSettings!.DateType = "DateTime";
        var generateCode = await GenerateCode(version, filename, settings);
        generateCode.Should().NotBeNullOrWhiteSpace();
        generateCode.Should().Contain("DateTime");
    }

    [Theory]
    [InlineAutoNSubstituteData(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [InlineAutoNSubstituteData(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [InlineAutoNSubstituteData(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [InlineAutoNSubstituteData(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
    public async Task Can_Generate_Custom_DateTimeType(
        SampleOpenSpecifications version,
        string filename)
    {
        var settings = new RefitGeneratorSettings();
        settings.CodeGeneratorSettings = new CodeGeneratorSettings();
        settings.CodeGeneratorSettings!.DateTimeType = "DateTime";
        var generateCode = await GenerateCode(version, filename, settings);
        generateCode.Should().NotBeNullOrWhiteSpace();
        generateCode.Should().Contain("DateTime");
    }

    [Theory]
    [InlineAutoNSubstituteData(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [InlineAutoNSubstituteData(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [InlineAutoNSubstituteData(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [InlineAutoNSubstituteData(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
    public async Task Can_Generate_Custom_ArrayType(
        SampleOpenSpecifications version,
        string filename)
    {
        var settings = new RefitGeneratorSettings();
        settings.CodeGeneratorSettings = new CodeGeneratorSettings();
        settings.CodeGeneratorSettings!.ArrayType = "System.Collection.Generic.IList";
        var generateCode = await GenerateCode(version, filename, settings);
        generateCode.Should().NotBeNullOrWhiteSpace();
        generateCode.Should().Contain("System.Collection.Generic.IList<");
    }

    [Theory]
    [InlineAutoNSubstituteData(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [InlineAutoNSubstituteData(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [InlineAutoNSubstituteData(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [InlineAutoNSubstituteData(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
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
        var generateCode = await GenerateCode(version, filename, settings);
        generateCode.Should().NotBeNullOrWhiteSpace();
        generateCode.Should().NotContain("class User");
    }

    [Theory]
    [InlineAutoNSubstituteData(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "SwaggerPetstore.json")]
    [InlineAutoNSubstituteData(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "SwaggerPetstore.yaml")]
    [InlineAutoNSubstituteData(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "SwaggerPetstore.json")]
    [InlineAutoNSubstituteData(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "SwaggerPetstore.yaml")]
    public async Task Can_Generate_With_Immutable_Records(SampleOpenSpecifications version, string filename)
    {
        var settings = new RefitGeneratorSettings();
        settings.ReturnIApiResponse = true;
        settings.CodeGeneratorSettings = new CodeGeneratorSettings { GenerateNativeRecords = true, };
        var generateCode = await GenerateCode(version, filename, settings);
        generateCode.Should().Contain("record Pet");
        generateCode.Should().Contain("Pet(");
        generateCode.Should().Contain("[JsonConstructor]");
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