using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Resources;
using Refitter.Tests.TestUtilities;

namespace Refitter.Tests.OpenApi;


[TestClass]
public class DocumentLoaderTests
{
    // A collectionFormat that is not a string cannot be read, and is not rewritten (#1424)
    private const string UnreadableSpec = """
        {
          "swagger": "2.0",
          "info": { "title": "T", "version": "1.0" },
          "paths": {
            "/items": {
              "get": {
                "parameters": [
                  { "name": "ids", "in": "query", "type": "array", "items": { "type": "string" }, "collectionFormat": { "x": 1 } }
                ],
                "responses": { "200": { "description": "OK" } }
              }
            }
          }
        }
        """;

    private static Task<NSwag.OpenApiDocument> Load(string path, CancellationToken cancellationToken = default) =>
        DocumentLoader.LoadAsync(path, ReferencePolicy.Unrestricted, cancellationToken);

    [TestMethod]
    [DataRow("")]
    [DataRow("  ")]
    [DataRow(null)]
    public async Task Throws_On_Missing_Path(string? path)
    {
        var act = () => Load(path!);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [TestMethod]
    public async Task Throws_For_Nonexistent_File()
    {
        var act = () => Load(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "nonexistent.json"));

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Failed to load OpenAPI document*");
    }

    [TestMethod]
    public async Task Throws_For_Invalid_Spec()
    {
        var swaggerFile = await TestFile.CreateSwaggerFile("not valid openapi content", "invalid.json");

        var act = () => Load(swaggerFile);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Failed to load OpenAPI document*");
    }

    [TestMethod]
    public async Task Failure_Message_Includes_The_Parser_Error()
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerJsonFile(UnreadableSpec);

        var act = () => Load(swaggerFile);

        var exception = (await act.Should().ThrowAsync<InvalidOperationException>()).Which;
        exception.Message.Should().Contain("collectionFormat");
    }

    [TestMethod]
    public async Task Propagates_Cancellation()
    {
        var swaggerFile = await TestFile.CreateSwaggerFile(
            EmbeddedResources.GetSwaggerPetstore(SampleOpenSpecifications.SwaggerPetstoreJsonV3),
            "cancel-file.json");
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var act = () => Load(swaggerFile, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [TestMethod]
    public async Task Surfaces_Policy_Rejection_As_ReferenceResolutionException()
    {
        // Port 1 refuses connections, so a fetch that was not blocked would surface as an HTTP failure
        var folder = TestDirectory.CreateFolder();
        var path = Path.Combine(folder, "spec.json");
        await File.WriteAllTextAsync(
            path,
            "{\"openapi\":\"3.0.0\",\"info\":{\"title\":\"t\",\"version\":\"1\"},\"paths\":{}," +
            "\"components\":{\"schemas\":{\"Pet\":{\"$ref\":\"http://127.0.0.1:1/s.json\"}}}}");

        var act = () => DocumentLoader.LoadAsync(path, new ReferencePolicy(allowRemoteReferences: false));

        await act.Should().ThrowAsync<ReferenceResolutionException>().WithMessage("*127.0.0.1:1*");
    }

    [TestMethod]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "petstore.json")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "petstore.yaml")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "petstore.json")]
    [DataRow(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "petstore.yaml")]
    public async Task Loads_Valid_OpenApi_Spec_From_File(
        SampleOpenSpecifications version,
        string filename)
    {
        var swaggerFile = await TestFile.CreateSwaggerFile(
            EmbeddedResources.GetSwaggerPetstore(version),
            filename);
        var result = await Load(swaggerFile);

        result.Should().NotBeNull();
        result!.Info.Title.Should().NotBeNullOrEmpty();
    }

    [TestMethod]
    [DataRow(LargeNumericBoundsSpecs.Yaml, "bounds.yaml")]
    [DataRow(LargeNumericBoundsSpecs.Json, "bounds.json")]
    public async Task Clamps_Numeric_Bounds_Outside_Decimal_Range(string contents, string filename)
    {
        var swaggerFile = await TestFile.CreateSwaggerFile(contents, filename);
        var result = await Load(swaggerFile);

        result.Should().NotBeNull();
        LargeNumericBoundsSpecs.GetMaximum(result!).Should().Be(decimal.MaxValue);
        LargeNumericBoundsSpecs.GetMinimum(result!).Should().Be(decimal.MinValue);
    }

    [TestMethod]
    [DataRow("yaml")]
    [DataRow("json")]
    public async Task Clamps_Numeric_Bounds_Outside_Decimal_Range_In_Referenced_Files(string extension)
    {
        var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(folder);

        var mainSpec = extension == "yaml"
            ? """
              openapi: 3.0.1
              info: { title: Bounds, version: v1 }
              paths:
                /m:
                  get:
                    responses:
                      '200':
                        description: ok
                        content:
                          application/json:
                            schema:
                              $ref: './components.yaml#/components/schemas/M'
              """
            : """
              {
                "openapi": "3.0.1",
                "info": { "title": "Bounds", "version": "v1" },
                "paths": { "/m": { "get": { "responses": { "200": { "description": "ok", "content": { "application/json": { "schema": { "$ref": "./components.json#/components/schemas/M" } } } } } } } }
              }
              """;
        var componentsSpec = extension == "yaml"
            ? LargeNumericBoundsSpecs.Yaml
            : LargeNumericBoundsSpecs.Json;

        var mainFile = Path.Combine(folder, $"main.{extension}");
        await File.WriteAllTextAsync(mainFile, mainSpec);
        await File.WriteAllTextAsync(Path.Combine(folder, $"components.{extension}"), componentsSpec);

        var result = await Load(mainFile);

        result.Should().NotBeNull();
        var schema = result!.Paths["/m"]["get"].ActualResponses["200"].Content["application/json"].Schema.ActualSchema;
        schema.Properties["value"].Maximum.Should().Be(decimal.MaxValue);
        schema.Properties["value"].Minimum.Should().Be(decimal.MinValue);

        Directory.Delete(folder, true);
    }
}
