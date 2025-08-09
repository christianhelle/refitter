using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Xunit;

namespace Refitter.Tests.Examples
{
    public class IncludeInheritanceHierarchyTests
    {
        private const string OpenApiSpec = @"
openapi: 3.0.1
paths:
  /v1/A:
    post:
      tags:
        - A
      operationId: CreateA
      responses:
        '201':
          description: Created
          content:
            application/json:
              schema:
                $ref: '#/components/schemas/A'
components:
  schemas:
    ABaseType:
      required:
        - $type
      type: object
      properties:
        $type:
          type: string
      discriminator:
        propertyName: $type
        mapping:
          A: '#/components/schemas/A'
          B: '#/components/schemas/B'
      additionalProperties: false
    A:
      type: object
      allOf:
        - $ref: '#/components/schemas/ABaseType'
      properties:
        propertyOfA:
          type: integer
          format: int64
      additionalProperties: false
    B:
      type: object
      allOf:
        - $ref: '#/components/schemas/ABaseType'
      properties:
        propertyOfB:
          type: integer
          format: int64
      additionalProperties: false
";

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Can_Generate_Code(bool includeHierarchy)
        {
            string generateCode = await GenerateCode(includeHierarchy);
            generateCode.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public async Task Removes_Unreferenced_Schema()
        {
            string generateCode = await GenerateCode(includeHierarchy: false);
            generateCode.Should().NotContain("Anonymous");

            generateCode.Should().NotContain("class B");
            generateCode.Should().Contain("class A");
        }

        [Fact]
        public async Task Keeps_All_Union_Cases_Schema()
        {
            string generateCode = await GenerateCode(includeHierarchy: true);
            generateCode.Should().NotContain("Anonymous");

            generateCode.Should().Contain("class B");
            generateCode.Should().Contain("class A");
        }

        [Theory]
        [InlineData(true)]
        [InlineData(false)]
        public async Task Can_Build_Generated_Code(bool includeHierarchy)
        {
            string generateCode = await GenerateCode(includeHierarchy);
            BuildHelper
                .BuildCSharp(generateCode)
                .Should()
                .BeTrue();
        }

        private static async Task<string> GenerateCode(bool includeHierarchy)
        {
            var swaggerFile = await CreateSwaggerFile(OpenApiSpec);
            var settings = new RefitGeneratorSettings
            {
                OpenApiPath = swaggerFile,
                TrimUnusedSchema = true,
                IncludeInheritanceHierarchy = includeHierarchy,
                UsePolymorphicSerialization = true, // use System.Text.Json attributes
            };

            var sut = await RefitGenerator.CreateAsync(settings);
            var generateCode = sut.Generate();
            return generateCode;
        }

        private static async Task<string> CreateSwaggerFile(string contents)
        {
            var filename = $"{Guid.NewGuid()}.yml";
            var folder = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
            Directory.CreateDirectory(folder);
            var swaggerFile = Path.Combine(folder, filename);
            await File.WriteAllTextAsync(swaggerFile, contents);
            return swaggerFile;
        }
    }
}
