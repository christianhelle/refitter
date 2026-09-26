using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Resources;

namespace Refitter.Tests.OpenApi;


public class FileDocumentStrategyTests
{
    [Test]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreJsonV3, "petstore.json")]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreYamlV3, "petstore.yaml")]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreJsonV2, "petstore.json")]
    [Arguments(SampleOpenSpecifications.SwaggerPetstoreYamlV2, "petstore.yaml")]
    public async Task Loads_Valid_OpenApi_Spec_From_File(
        SampleOpenSpecifications version,
        string filename)
    {
        var swaggerFile = await TestFile.CreateSwaggerFile(
            EmbeddedResources.GetSwaggerPetstore(version),
            filename);
        var strategy = new FileDocumentStrategy();
        var result = await strategy.TryLoadAsync(swaggerFile);

        result.Should().NotBeNull();
        result!.Info.Title.Should().NotBeNullOrEmpty();
    }

    [Test]
    [Arguments("petstore.json")]
    [Arguments("petstore.yaml")]
    [Arguments("petstore.yml")]
    public async Task Returns_Null_For_Http_Uri(string filename)
    {
        var strategy = new FileDocumentStrategy();
        var result = await strategy.TryLoadAsync($"https://example.com/{filename}");

        result.Should().BeNull();
    }

    [Test]
    public async Task Returns_Null_For_Nonexistent_File()
    {
        var strategy = new FileDocumentStrategy();
        var result = await strategy.TryLoadAsync(
            Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString(), "nonexistent.json"));

        result.Should().BeNull();
    }

    [Test]
    public async Task Returns_Null_For_Invalid_Spec()
    {
        var swaggerFile = await TestFile.CreateSwaggerFile(
            "not valid openapi content",
            "invalid.json");
        var strategy = new FileDocumentStrategy();
        var result = await strategy.TryLoadAsync(swaggerFile);

        result.Should().BeNull();
    }

    [Test]
    [Arguments(LargeNumericBoundsSpecs.Yaml, "bounds.yaml")]
    [Arguments(LargeNumericBoundsSpecs.Json, "bounds.json")]
    public async Task Clamps_Numeric_Bounds_Outside_Decimal_Range(string contents, string filename)
    {
        var swaggerFile = await TestFile.CreateSwaggerFile(contents, filename);
        var strategy = new FileDocumentStrategy();
        var result = await strategy.TryLoadAsync(swaggerFile);

        result.Should().NotBeNull();
        LargeNumericBoundsSpecs.GetMaximum(result!).Should().Be(decimal.MaxValue);
        LargeNumericBoundsSpecs.GetMinimum(result!).Should().Be(decimal.MinValue);
    }

    [Test]
    [Arguments("yaml")]
    [Arguments("json")]
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

        var strategy = new FileDocumentStrategy();
        var result = await strategy.TryLoadAsync(mainFile);

        result.Should().NotBeNull();
        var schema = result!.Paths["/m"]["get"].ActualResponses["200"].Content["application/json"].Schema.ActualSchema;
        schema.Properties["value"].Maximum.Should().Be(decimal.MaxValue);
        schema.Properties["value"].Minimum.Should().Be(decimal.MinValue);

        Directory.Delete(folder, true);
    }
}
