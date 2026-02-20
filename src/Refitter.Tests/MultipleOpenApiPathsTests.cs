using FluentAssertions;
using Refitter.Core;
using TUnit.Core;

namespace Refitter.Tests;

public class MultipleOpenApiPathsTests
{
    private const string PetstoreV1Spec = @"openapi: '3.0.0'
info:
  title: Petstore V1
  version: '1.0'
paths:
  /pets:
    get:
      operationId: listPetsV1
      summary: List all pets
      tags:
        - pets
      responses:
        '200':
          description: A list of pets
          content:
            application/json:
              schema:
                type: array
                items:
                  $ref: '#/components/schemas/PetV1'
components:
  schemas:
    PetV1:
      type: object
      properties:
        id:
          type: integer
        name:
          type: string
";

    private const string PetstoreV2Spec = @"openapi: '3.0.0'
info:
  title: Petstore V2
  version: '2.0'
paths:
  /v2/pets:
    get:
      operationId: listPetsV2
      summary: List all pets (v2)
      tags:
        - pets
      responses:
        '200':
          description: A list of pets
          content:
            application/json:
              schema:
                type: array
                items:
                  $ref: '#/components/schemas/PetV2'
components:
  schemas:
    PetV2:
      type: object
      properties:
        id:
          type: integer
        name:
          type: string
        breed:
          type: string
";

    [Test]
    public async Task Can_Generate_Code_From_Multiple_Paths()
    {
        var (file1, file2) = await CreateTestSpecFiles();
        var settings = new RefitGeneratorSettings
        {
            OpenApiPaths = [file1, file2]
        };

        var sut = await RefitGenerator.CreateAsync(settings);
        var generatedCode = sut.Generate();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Generated_Code_Contains_Operations_From_Both_Specs()
    {
        var (file1, file2) = await CreateTestSpecFiles();
        var settings = new RefitGeneratorSettings
        {
            OpenApiPaths = [file1, file2]
        };

        var sut = await RefitGenerator.CreateAsync(settings);
        var generatedCode = sut.Generate();

        generatedCode.Should().Contain("ListPetsV1");
        generatedCode.Should().Contain("ListPetsV2");
    }

    [Test]
    public async Task Generated_Code_Contains_Schemas_From_Both_Specs()
    {
        var (file1, file2) = await CreateTestSpecFiles();
        var settings = new RefitGeneratorSettings
        {
            OpenApiPaths = [file1, file2]
        };

        var sut = await RefitGenerator.CreateAsync(settings);
        var generatedCode = sut.Generate();

        generatedCode.Should().Contain("PetV1");
        generatedCode.Should().Contain("PetV2");
    }

    [Test]
    public async Task Single_Path_In_OpenApiPaths_Works_Same_As_OpenApiPath()
    {
        var (file1, _) = await CreateTestSpecFiles();
        var settingsSingle = new RefitGeneratorSettings { OpenApiPath = file1 };
        var settingsMulti = new RefitGeneratorSettings { OpenApiPaths = [file1] };

        var sutSingle = await RefitGenerator.CreateAsync(settingsSingle);
        var sutMulti = await RefitGenerator.CreateAsync(settingsMulti);

        var codeSingle = sutSingle.Generate();
        var codeMulti = sutMulti.Generate();

        codeSingle.Should().NotBeNullOrWhiteSpace();
        codeMulti.Should().NotBeNullOrWhiteSpace();
        codeMulti.Should().Contain("ListPetsV1");
        codeSingle.Should().Contain("ListPetsV1");
    }

    [Test]
    public async Task OpenApiDocumentFactory_Merges_Multiple_Documents()
    {
        var (file1, file2) = await CreateTestSpecFiles();

        var merged = await OpenApiDocumentFactory.CreateAsync(new[] { file1, file2 });

        merged.Should().NotBeNull();
        merged.Paths.Should().ContainKey("/pets");
        merged.Paths.Should().ContainKey("/v2/pets");
    }

    [Test]
    public async Task OpenApiDocumentFactory_Merge_Keeps_Base_Document_Info()
    {
        var (file1, file2) = await CreateTestSpecFiles();

        var merged = await OpenApiDocumentFactory.CreateAsync(new[] { file1, file2 });

        merged.Info.Title.Should().Be("Petstore V1");
    }

    [Test]
    public async Task OpenApiDocumentFactory_Merge_Does_Not_Overwrite_Existing_Paths()
    {
        var (file1, _) = await CreateTestSpecFiles();

        var merged = await OpenApiDocumentFactory.CreateAsync(new[] { file1, file1 });

        // /pets appears in both but should only be present once
        merged.Paths.Should().ContainKey("/pets");
        merged.Paths.Count.Should().Be(1);
    }

    [Test]
    public async Task OpenApiDocumentFactory_Merges_Schemas_From_All_Documents()
    {
        var (file1, file2) = await CreateTestSpecFiles();

        var merged = await OpenApiDocumentFactory.CreateAsync(new[] { file1, file2 });

        merged.Components.Schemas.Should().ContainKey("PetV1");
        merged.Components.Schemas.Should().ContainKey("PetV2");
    }

    private static async Task<(string file1, string file2)> CreateTestSpecFiles()
    {
        var file1 = await TestFile.CreateSwaggerFile(PetstoreV1Spec, "petstore-v1.yaml");
        var file2 = await TestFile.CreateSwaggerFile(PetstoreV2Spec, "petstore-v2.yaml");
        return (file1, file2);
    }
}
