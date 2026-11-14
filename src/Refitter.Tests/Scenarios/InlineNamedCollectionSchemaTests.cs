using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;

namespace Refitter.Tests.Scenarios;

[TestClass]
public class InlineNamedCollectionSchemaTests
{
    private const string OpenApiSpec = @"
openapi: '3.0.0'
info:
  title: 'OAuth API'
  version: '1.0'
paths:
  /apps:
    get:
      operationId: 'GetApplication'
      responses:
        '200':
          description: 'Success'
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/Application'
  /scopes:
    get:
      operationId: 'GetScopes'
      responses:
        '200':
          description: 'Success'
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/OAuthScopes'
components:
  schemas:
    OAuthScope:
      type: string
    OAuthScopes:
      type: array
      items:
        $ref: '#/components/schemas/OAuthScope'
    OAuthScopeMap:
      type: object
      additionalProperties:
        $ref: '#/components/schemas/OAuthScopes'
    Application:
      type: object
      properties:
        scopes:
          $ref: '#/components/schemas/OAuthScopes'
        scopesByName:
          $ref: '#/components/schemas/OAuthScopeMap'
";

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task Inlined_Named_Array_Is_Not_Generated(bool generateNativeRecords)
    {
        string generatedCode = await GenerateCode(generateNativeRecords, inlineNamedArrays: true, inlineNamedDictionaries: false);
        generatedCode.Should().NotContain("OAuthScopes");
        generatedCode.Should().Contain("ICollection<string> Scopes");
        generatedCode.Should().Contain("partial class OAuthScopeMap");
    }

    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task Inlined_Named_Dictionary_Is_Not_Generated(bool generateNativeRecords)
    {
        string generatedCode = await GenerateCode(generateNativeRecords, inlineNamedArrays: false, inlineNamedDictionaries: true);
        generatedCode.Should().NotContain("OAuthScopeMap");
        generatedCode.Should().Contain("partial class OAuthScopes");
    }

    [TestMethod]
    public async Task Named_Collections_Are_Generated_When_Not_Inlined()
    {
        string generatedCode = await GenerateCode(false, inlineNamedArrays: false, inlineNamedDictionaries: false);
        generatedCode.Should().Contain("partial class OAuthScopes");
        generatedCode.Should().Contain("partial class OAuthScopeMap");
    }

    [TestCategory("Integration")]
    [TestMethod]
    [DataRow(false)]
    [DataRow(true)]
    public async Task Can_Build_Generated_Code_With_Inlined_Collections(bool generateNativeRecords)
    {
        string generatedCode = await GenerateCode(generateNativeRecords, inlineNamedArrays: true, inlineNamedDictionaries: true);
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    private static async Task<string> GenerateCode(
        bool generateNativeRecords,
        bool inlineNamedArrays,
        bool inlineNamedDictionaries)
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        try
        {
            var settings = new RefitGeneratorSettings
            {
                OpenApiPath = swaggerFile,
                GenerateJsonSerializerContext = true,
                CodeGeneratorSettings = new CodeGeneratorSettings
                {
                    GenerateNativeRecords = generateNativeRecords,
                    InlineNamedArrays = inlineNamedArrays,
                    InlineNamedDictionaries = inlineNamedDictionaries,
                },
            };
            var generator = await RefitGenerator.CreateAsync(settings);
            return generator.Generate();
        }
        finally
        {
            if (File.Exists(swaggerFile)) File.Delete(swaggerFile);
            var directory = Path.GetDirectoryName(swaggerFile);
            if (directory != null && Directory.Exists(directory))
                Directory.Delete(directory, true);
        }
    }
}
