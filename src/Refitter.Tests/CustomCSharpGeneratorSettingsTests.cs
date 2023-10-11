using System.Dynamic;

using Atc.Test;

using FluentAssertions;

using NJsonSchema.CodeGeneration.CSharp;

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
    public async Task Can_Generate_Custom_DateType(
        SampleOpenSpecifications version,
        string filename)
    {
        var settings = new RefitGeneratorSettings();
        settings.CodeGeneratorSettings = new CSharpGeneratorSettings();
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
        settings.CodeGeneratorSettings = new CSharpGeneratorSettings();
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
        settings.CodeGeneratorSettings = new CSharpGeneratorSettings();
        settings.CodeGeneratorSettings!.ArrayType = "System.Collection.Generic.IList";
        var generateCode = await GenerateCode(version, filename, settings);
        generateCode.Should().NotBeNullOrWhiteSpace();
        generateCode.Should().Contain("System.Collection.Generic.IList<");
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