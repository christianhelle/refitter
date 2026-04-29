using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Scenarios;

public class NamingSettingsTests
{
    private const string OpenApiSpec = @"
openapi: 3.0.0
info:
  title: PetStore
  version: 1.0.0
paths:
  /pets:
    get:
      operationId: listPets
      responses:
        '200':
          description: successful operation
          content:
            application/json:
              schema:
                type: array
                items:
                  $ref: '#/components/schemas/Pet'
components:
  schemas:
    Pet:
      type: object
      properties:
        id:
          type: string
        name:
          type: string
";

    [Test]
    public async Task Can_Generate_Code()
    {
        string generatedCode = await GenerateCode(useOpenApiTitle: true);
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Can_Build_Generated_Code()
    {
        string generatedCode = await GenerateCode(useOpenApiTitle: true);
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    [Test]
    public async Task Generated_Code_Uses_OpenApi_Title_As_Interface_Name()
    {
        string generatedCode = await GenerateCode(useOpenApiTitle: true);
        generatedCode.Should().Contain("interface IPetStore");
    }

    [Test]
    public async Task Generated_Code_Uses_Custom_Interface_Name()
    {
        string generatedCode = await GenerateCode(useOpenApiTitle: false, interfaceName: "MyCustomApi");
        generatedCode.Should().Contain("interface IMyCustomApi");
    }

    [Test]
    public async Task Generated_Code_Uses_Default_Interface_Name_When_Not_Configured()
    {
        string generatedCode = await GenerateCode(useOpenApiTitle: false, interfaceName: null);
        generatedCode.Should().Contain("interface IApiClient");
    }

    [Test]
    public async Task Custom_Interface_Name_Takes_Precedence_Over_OpenApi_Title()
    {
        string generatedCode = await GenerateCode(useOpenApiTitle: false, interfaceName: "OverrideApi");
        generatedCode.Should().Contain("interface IOverrideApi");
        generatedCode.Should().NotContain("interface IPetStore");
    }

    private static async Task<string> GenerateCode(bool useOpenApiTitle, string? interfaceName = null)
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        try
        {
            var namingSettings = new NamingSettings
            {
                UseOpenApiTitle = useOpenApiTitle
            };

            if (interfaceName != null)
            {
                namingSettings.InterfaceName = interfaceName;
            }

            var settings = new RefitGeneratorSettings
            {
                OpenApiPath = swaggerFile,
                Naming = namingSettings
            };

            var sut = await RefitGenerator.CreateAsync(settings);
            return sut.Generate();
        }
        finally
        {
            if (File.Exists(swaggerFile))
                File.Delete(swaggerFile);
            var directory = Path.GetDirectoryName(swaggerFile);
            if (directory != null && Directory.Exists(directory))
                Directory.Delete(directory, true);
        }
    }
}
