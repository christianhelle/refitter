using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;

namespace Refitter.Tests.Scenarios;

[TestClass]
public class NativeRecordsWithCollectionSchemaTests
{
    private const string OpenApiSpec = @"
openapi: '3.0.0'
info:
  title: 'OAuth API'
  version: '1.0'
paths:
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
  /scopes/by-name:
    get:
      operationId: 'GetScopesByName'
      responses:
        '200':
          description: 'Success'
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/OAuthScopeMap'
  /scopes/audited:
    get:
      operationId: 'GetAuditedScopes'
      responses:
        '200':
          description: 'Success'
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/AuditedScopeMap'
components:
  schemas:
    OAuthScope:
      type: object
      properties:
        name:
          type: string
    OAuthScopes:
      type: array
      description: 'Array of OAuth scopes'
      items:
        $ref: '#/components/schemas/OAuthScope'
    OAuthScopeMap:
      type: object
      description: 'OAuth scopes by name'
      additionalProperties:
        $ref: '#/components/schemas/OAuthScope'
    DetailedScopeMap:
      allOf:
        - $ref: '#/components/schemas/OAuthScopeMap'
        - type: object
          properties:
            owner:
              type: string
    AuditedScopeMap:
      allOf:
        - $ref: '#/components/schemas/DetailedScopeMap'
        - type: object
          properties:
            auditor:
              type: string
";

    [TestMethod]
    [DataRow(true, false)]
    [DataRow(false, true)]
    public async Task Array_Schema_Is_Generated_As_Class(bool generateNativeRecords, bool immutableRecords)
    {
        string generatedCode = await GenerateCode(generateNativeRecords, immutableRecords);
        generatedCode.Should().Contain(
            "partial class OAuthScopes : System.Collections.ObjectModel.Collection<OAuthScope>");
    }

    [TestMethod]
    [DataRow(true, false)]
    [DataRow(false, true)]
    public async Task Dictionary_Schema_Is_Generated_As_Class(bool generateNativeRecords, bool immutableRecords)
    {
        string generatedCode = await GenerateCode(generateNativeRecords, immutableRecords);
        generatedCode.Should().Contain(
            "partial class OAuthScopeMap : Dictionary<string, OAuthScope>");
    }

    [TestMethod]
    [DataRow(true, false)]
    [DataRow(false, true)]
    public async Task Descendants_Of_Collection_Schemas_Are_Generated_As_Class(
        bool generateNativeRecords,
        bool immutableRecords)
    {
        string generatedCode = await GenerateCode(generateNativeRecords, immutableRecords);
        generatedCode.Should().Contain("partial class DetailedScopeMap : ");
        generatedCode.Should().Contain("partial class AuditedScopeMap : DetailedScopeMap");
    }

    [TestMethod]
    [DataRow(true, false)]
    [DataRow(false, true)]
    public async Task Object_Schema_Is_Still_Generated_As_Record(bool generateNativeRecords, bool immutableRecords)
    {
        string generatedCode = await GenerateCode(generateNativeRecords, immutableRecords);
        generatedCode.Should().Contain("partial record OAuthScope");
    }

    [TestMethod]
    public async Task Custom_Base_Types_Are_Generated_As_Class()
    {
        string generatedCode = await GenerateCodeWithCustomBaseTypes();
        generatedCode.Should().Contain(
            "partial class OAuthScopes : List<OAuthScope>");
        generatedCode.Should().Contain(
            "partial class OAuthScopeMap : System.Collections.Concurrent.ConcurrentDictionary<string, OAuthScope>");
    }

    [TestCategory("Integration")]
    [TestMethod]
    public async Task Can_Build_Generated_Code_With_Custom_Base_Types()
    {
        string generatedCode = await GenerateCodeWithCustomBaseTypes();
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    [TestCategory("Integration")]
    [TestMethod]
    [DataRow(true, false)]
    [DataRow(false, true)]
    public async Task Can_Build_Generated_Code(bool generateNativeRecords, bool immutableRecords)
    {
        string generatedCode = await GenerateCode(generateNativeRecords, immutableRecords);
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    private static Task<string> GenerateCode(bool generateNativeRecords, bool immutableRecords) =>
        GenerateCode(
            immutableRecords,
            new CodeGeneratorSettings
            {
                GenerateNativeRecords = generateNativeRecords,
            });

    private static Task<string> GenerateCodeWithCustomBaseTypes() =>
        GenerateCode(
            false,
            new CodeGeneratorSettings
            {
                GenerateNativeRecords = true,
                ArrayBaseType = "System.Collections.Generic.List",
                DictionaryBaseType = "System.Collections.Concurrent.ConcurrentDictionary",
            });

    private static async Task<string> GenerateCode(bool immutableRecords, CodeGeneratorSettings codeGeneratorSettings)
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        try
        {
            var settings = new RefitGeneratorSettings
            {
                OpenApiPath = swaggerFile,
                ImmutableRecords = immutableRecords,
                CodeGeneratorSettings = codeGeneratorSettings,
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
