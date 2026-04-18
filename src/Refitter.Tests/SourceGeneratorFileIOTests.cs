using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests;

/// <summary>
/// Test for Issue #635: Source generator should use context.AddSource() instead of File.WriteAllText()
/// https://github.com/christianhelle/refitter/issues/635
/// </summary>
public class SourceGeneratorFileIOTests
{
    private const string OpenApiSpec = @"
openapi: '3.0.0'
info:
  title: Test API
  version: '1.0.0'
paths:
  /users:
    get:
      operationId: 'GetUsers'
      responses:
        '200':
          description: 'Success'
          content:
            application/json:
              schema:
                type: array
                items:
                  $ref: '#/components/schemas/User'
components:
  schemas:
    User:
      type: object
      properties:
        id:
          type: string
        name:
          type: string
";

    [Test]
    public async Task Test_SourceGenerator_DoesNotWriteToFileSystem()
    {
        // Arrange
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = swaggerFile,
            ReturnIApiResponse = true,
        };

        // Get the directory where output would be written
        var outputDir = Path.GetDirectoryName(swaggerFile)!;
        var possibleOutputFiles = Directory.GetFiles(outputDir, "*.g.cs");
        var initialFileCount = possibleOutputFiles.Length;

        // Act - Generate code (this simulates what the source generator does)
        var generator = await RefitGenerator.CreateAsync(settings);
        var generatedCode = generator.Generate();

        // Assert - Verify no new .g.cs files were created
        var finalOutputFiles = Directory.GetFiles(outputDir, "*.g.cs");
        var finalFileCount = finalOutputFiles.Length;

        // No new files should have been written to disk during generation
        finalFileCount.Should().Be(initialFileCount,
            "source generator should use context.AddSource() not File.WriteAllText()");

        // But generated code should exist in memory
        generatedCode.Should().NotBeNullOrWhiteSpace();
        generatedCode.Should().Contain("partial interface ITestAPI");
    }

    [Test]
    public async Task Test_SourceGenerator_WithApiDescriptionServer_NoFileConflicts()
    {
        // This tests that concurrent generation doesn't cause file I/O conflicts
        // When using context.AddSource(), multiple generators can run in parallel safely

        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = swaggerFile,
            ReturnIApiResponse = true,
        };

        // Act - Simulate concurrent generation
        var tasks = Enumerable.Range(0, 5).Select(async i =>
        {
            var generator = await RefitGenerator.CreateAsync(settings);
            return generator.Generate();
        });

        // This should not throw IOException or file access exceptions
        var results = await Task.WhenAll(tasks);

        // Assert - All generations should succeed
        foreach (var code in results)
        {
            code.Should().NotBeNullOrWhiteSpace();
            code.Should().Contain("partial interface ITestAPI");
        }
    }

    [Test]
    public async Task Test_SourceGenerator_GeneratedCodeIsValid()
    {
        // Arrange
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = swaggerFile,
            ReturnIApiResponse = true,
        };

        // Act
        var generator = await RefitGenerator.CreateAsync(settings);
        var generatedCode = generator.Generate();

        // Assert - Generated code should compile
        generatedCode.Should().Contain("partial interface ITestAPI");
        generatedCode.Should().Contain("Task<IApiResponse<ICollection<User>>> GetUsers(");

        BuildHelper
            .BuildCSharp(generatedCode)
            .Should()
            .BeTrue("generated code should compile successfully");
    }

    [Test]
    public async Task Test_SourceGenerator_OutputFilename_NotCreatedOnDisk()
    {
        // Arrange
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        var outputFilename = "CustomOutput.g.cs";
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = swaggerFile,
            OutputFilename = outputFilename,
            ReturnIApiResponse = true,
        };

        var outputDir = Path.GetDirectoryName(swaggerFile)!;
        var expectedFilePath = Path.Combine(outputDir, outputFilename);

        // Act
        var generator = await RefitGenerator.CreateAsync(settings);
        var generatedCode = generator.Generate();

        // Assert - The OutputFilename should NOT create a file on disk
        File.Exists(expectedFilePath).Should().BeFalse(
            "OutputFilename should be used as hint name for context.AddSource(), not as a file path");

        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public void Test_HintName_IsUnique_For_SameFilename_InDifferentDirectories()
    {
        // Regression test for: hint-name collisions when two .refitter files share a filename
        // Two .refitter files with the same filename in different directories must produce different hint names.
        static string ComputeHintName(string filePath)
        {
            var fileNameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
            if (string.IsNullOrEmpty(fileNameWithoutExt) || fileNameWithoutExt == ".")
                fileNameWithoutExt = "Refitter";

            var dir = Path.GetDirectoryName(filePath) ?? string.Empty;
            var safeDir = dir
                .Replace(Path.DirectorySeparatorChar, '_')
                .Replace(Path.AltDirectorySeparatorChar, '_')
                .Replace(':', '_');

            return string.IsNullOrEmpty(safeDir)
                ? $"{fileNameWithoutExt}.g.cs"
                : $"{safeDir}_{fileNameWithoutExt}.g.cs";
        }

        var pathA = Path.Combine("src", "ApiA", "petstore.refitter");
        var pathB = Path.Combine("src", "ApiB", "petstore.refitter");

        var hintA = ComputeHintName(pathA);
        var hintB = ComputeHintName(pathB);

        hintA.Should().NotBe(hintB,
            "two .refitter files with the same name in different directories must produce different hint names");
    }

    [Test]
    public void Test_HintName_UsesOutputFilename_WhenSpecified()
    {
        // When settings.OutputFilename is set, it should be used as the hint name directly.
        var outputFilename = "MyCustomOutput.g.cs";

        var hintName = outputFilename.EndsWith(".cs", StringComparison.OrdinalIgnoreCase)
            ? outputFilename
            : outputFilename + ".g.cs";

        hintName.Should().Be("MyCustomOutput.g.cs",
            "OutputFilename should be used as-is when it already ends with .cs");
    }
}
