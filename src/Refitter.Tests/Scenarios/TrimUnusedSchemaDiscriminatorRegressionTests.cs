using FluentAssertions;
using NSwag;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Scenarios;

public class TrimUnusedSchemaDiscriminatorRegressionTests
{
    private const string OpenApiSpec = """
        openapi: 3.1.1
        info:
          title: Test
          version: v1
        paths:
          /results:
            get:
              operationId: GetResults
              responses:
                '200':
                  description: Success
                  content:
                    application/json:
                      schema:
                        $ref: '#/components/schemas/Result'
        components:
          schemas:
            Result:
              type: object
              union:
                - $ref: '#/components/schemas/SonarrResult'
                - $ref: '#/components/schemas/RadarrResult'
              discriminator:
                propertyName: service
                mapping:
                  sonarr: '#/components/schemas/SonarrResult'
                  radarr: '#/components/schemas/RadarrResult'
                  unused: '#/components/schemas/UnusedResult'
            SonarrResult:
              type: object
              properties:
                service:
                  type: string
                  enum: [sonarr]
            RadarrResult:
              type: object
              properties:
                service:
                  type: string
                  enum: [radarr]
            UnusedResult:
              type: object
              properties:
                service:
                  type: string
                  enum: [unused]
        """;

    [Test]
    [Arguments("anyOf")]
    [Arguments("oneOf")]
    public async Task Trimming_Keeps_Mappings_For_Reachable_Union_Targets(string unionType)
    {
        RefitGenerator generator = await CreateGenerator(unionType);
        OpenApiDocument document = generator.OpenApiDocument;

        document.Components.Schemas.Keys.Should().BeEquivalentTo(
            ["Result", "SonarrResult", "RadarrResult"]);
        document.Components.Schemas["Result"]
            .DiscriminatorObject!.Mapping.Keys.Should().BeEquivalentTo(["sonarr", "radarr"]);
    }

    [Test]
    [Arguments("anyOf")]
    [Arguments("oneOf")]
    public async Task Including_Hierarchy_Keeps_All_Discriminator_Mappings(string unionType)
    {
        RefitGenerator generator = await CreateGenerator(
            unionType,
            includeInheritanceHierarchy: true);
        OpenApiDocument document = generator.OpenApiDocument;

        document.Components.Schemas.Keys.Should().BeEquivalentTo(
            ["Result", "SonarrResult", "RadarrResult", "UnusedResult"]);
        document.Components.Schemas["Result"]
            .DiscriminatorObject!.Mapping.Keys.Should()
            .BeEquivalentTo(["sonarr", "radarr", "unused"]);
    }

    [Test]
    [Arguments("anyOf")]
    [Arguments("oneOf")]
    public async Task Disabling_Trimming_Keeps_All_Discriminator_Mappings(string unionType)
    {
        RefitGenerator generator = await CreateGenerator(unionType, trimUnusedSchema: false);
        OpenApiDocument document = generator.OpenApiDocument;

        document.Components.Schemas.Keys.Should().BeEquivalentTo(
            ["Result", "SonarrResult", "RadarrResult", "UnusedResult"]);
        document.Components.Schemas["Result"]
            .DiscriminatorObject!.Mapping.Keys.Should()
            .BeEquivalentTo(["sonarr", "radarr", "unused"]);
    }

    [Test]
    [Arguments("anyOf")]
    [Arguments("oneOf")]
    public async Task Trimming_Preserves_Mapping_Keys_In_Generated_Attributes(string unionType)
    {
        RefitGenerator generator = await CreateGenerator(unionType);

        string generatedCode = generator.Generate();

        generatedCode.Should().Contain(
            "[JsonDerivedType(typeof(SonarrResult), typeDiscriminator: \"sonarr\")]");
        generatedCode.Should().Contain(
            "[JsonDerivedType(typeof(RadarrResult), typeDiscriminator: \"radarr\")]");
        generatedCode.Should().NotContain(
            "[JsonDerivedType(typeof(SonarrResult), typeDiscriminator: \"SonarrResult\")]");
        generatedCode.Should().NotContain(
            "[JsonDerivedType(typeof(RadarrResult), typeDiscriminator: \"RadarrResult\")]");
        generatedCode.Should().NotContain("class UnusedResult");
    }

    [Category("Integration")]
    [Test]
    [Arguments("anyOf")]
    [Arguments("oneOf")]
    public async Task Trimmed_Polymorphic_Output_Can_Build(string unionType)
    {
        RefitGenerator generator = await CreateGenerator(unionType);
        string generatedCode = generator.Generate();

        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    private static async Task<RefitGenerator> CreateGenerator(
        string unionType,
        bool trimUnusedSchema = true,
        bool? includeInheritanceHierarchy = null)
    {
        string openApiSpec = OpenApiSpec.Replace("union:", $"{unionType}:");
        string swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(openApiSpec);

        try
        {
            RefitGeneratorSettings settings = includeInheritanceHierarchy.HasValue
                ? new RefitGeneratorSettings
                {
                    OpenApiPath = swaggerFile,
                    TrimUnusedSchema = trimUnusedSchema,
                    IncludeInheritanceHierarchy = includeInheritanceHierarchy.Value,
                    UsePolymorphicSerialization = true
                }
                : new RefitGeneratorSettings
                {
                    OpenApiPath = swaggerFile,
                    TrimUnusedSchema = trimUnusedSchema,
                    UsePolymorphicSerialization = true
                };

            return await RefitGenerator.CreateAsync(settings);
        }
        finally
        {
            DeleteSwaggerFile(swaggerFile);
        }
    }

    private static void DeleteSwaggerFile(string swaggerFile)
    {
        if (File.Exists(swaggerFile))
        {
            File.Delete(swaggerFile);
        }

        string? directory = Path.GetDirectoryName(swaggerFile);
        if (directory != null && Directory.Exists(directory))
        {
            Directory.Delete(directory, true);
        }
    }
}
