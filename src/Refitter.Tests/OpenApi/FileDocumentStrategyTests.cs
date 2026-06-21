using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Resources;

namespace Refitter.Tests.OpenApi;


[Category("Unit")]
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
}
