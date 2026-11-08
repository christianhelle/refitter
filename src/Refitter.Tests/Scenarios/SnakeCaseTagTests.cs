using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;

namespace Refitter.Tests.Scenarios;

/// <summary>
/// Snake_case tags must produce PascalCase interface names when grouping by tag.
/// </summary>
[TestClass]
public class SnakeCaseTagTests
{
    private const string OpenApiSpec = """
        openapi: 3.0.1
        info: { title: Tags, version: v1 }
        tags:
          - name: custom_emojis
            description: Custom emoji operations
        paths:
          /emojis:
            get:
              operationId: GetEmojis
              tags: ['custom_emojis']
              responses: { '204': { description: ok } }
          /featured:
            get:
              operationId: GetFeatured
              tags: ['featured_tags']
              responses: { '204': { description: ok } }
          /numeric:
            get:
              operationId: GetNumeric
              tags: ['123_numeric']
              responses: { '204': { description: ok } }
          /keyword:
            get:
              operationId: GetKeyword
              tags: ['class_']
              responses: { '204': { description: ok } }
          /underscore:
            get:
              operationId: GetUnderscore
              tags: ['_']
              responses: { '204': { description: ok } }
          /merged-snake:
            get:
              operationId: GetMergedSnake
              tags: ['user_profile']
              responses: { '204': { description: ok } }
          /merged-pascal:
            get:
              operationId: GetMergedPascal
              tags: ['UserProfile']
              responses: { '204': { description: ok } }
        """;

    [TestMethod]
    [DataRow("ICustomEmojisApi")]
    [DataRow("IFeaturedTagsApi")]
    [DataRow("I_123NumericApi")]
    [DataRow("IClassApi")]
    public async Task Generates_PascalCase_Interface_Names_From_Snake_Case_Tags(string expected)
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().Contain($"interface {expected}");
    }

    [TestMethod]
    public async Task Does_Not_Keep_Snake_Case_In_Interface_Names()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().NotContain("ICustom_emojisApi");
        generatedCode.Should().NotContain("IFeatured_tagsApi");
    }

    [TestMethod]
    public async Task Keeps_Tag_Description_On_Interface_Documentation()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().Contain("Custom emoji operations");
    }

    [TestMethod]
    public async Task Merges_Tags_That_Normalize_To_The_Same_Name()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().Contain("interface IUserProfileApi");
        generatedCode.Should().NotContain("interface IUserProfile2Api");

        var start = generatedCode.IndexOf("interface IUserProfileApi", StringComparison.Ordinal);
        var end = generatedCode.IndexOf("interface ", start + 1, StringComparison.Ordinal);
        var mergedInterface = end < 0 ? generatedCode.Substring(start) : generatedCode.Substring(start, end - start);

        mergedInterface.Should().Contain("GetMergedSnake");
        mergedInterface.Should().Contain("GetMergedPascal");
    }

    [TestMethod]
    [TestCategory("Integration")]
    public async Task Can_Build_Generated_Code()
    {
        var generatedCode = await GenerateCode();
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    private static async Task<string> GenerateCode()
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = swaggerFile,
            MultipleInterfaces = MultipleInterfaces.ByTag
        };

        var sut = await RefitGenerator.CreateAsync(settings);
        return sut.Generate();
    }
}
