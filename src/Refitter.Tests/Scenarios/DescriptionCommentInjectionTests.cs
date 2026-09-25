using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Scenarios;

/// <summary>
/// Titles, summaries and descriptions containing comment terminators, XML tags and code.
/// </summary>
public class DescriptionCommentInjectionTests
{
    private const string OpenApiSpec = """
        openapi: 3.0.1
        info:
          title: Injection */ class X {} /*
          version: v1
          description: "Ends with */ and <tags> & \\u0000 stuff"
        paths:
          /i:
            get:
              operationId: GetI
              summary: "*/ public class Evil {} /* </summary> <script>"
              description: "Line1\r\nLine2\n/// fake doc\n#region nope"
              parameters:
                - name: q
                  in: query
                  description: "</param> */ \" ' \\ {0} <![CDATA[x]]>"
                  schema: { type: string }
              responses:
                '200':
                  description: "*/ nope <returns>"
                  content: { application/json: { schema: { $ref: '#/components/schemas/I' } } }
        components:
          schemas:
            I:
              type: object
              description: "*/ \" */"
              properties:
                p: { type: string, description: "</summary> */ \"\"\" @\"" , example: "*/" }
        """;

    [Test]
    public async Task Can_Generate_Code()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Keeps_Injected_Code_Inside_Comments()
    {
        var generatedCode = await GenerateCode();
        generatedCode.Split('\n')
            .Where(line => line.Contains("public class Evil"))
            .Should()
            .OnlyContain(line => line.TrimStart().StartsWith("//"));
    }

    [Test]
    [Category("Integration")]
    public async Task Can_Build_Generated_Code()
    {
        var generatedCode = await GenerateCode();
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    private static async Task<string> GenerateCode(Action<RefitGeneratorSettings>? configure = null)
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpec);
        var settings = new RefitGeneratorSettings { OpenApiPath = swaggerFile };
        configure?.Invoke(settings);

        var sut = await RefitGenerator.CreateAsync(settings);
        return sut.Generate();
    }
}
