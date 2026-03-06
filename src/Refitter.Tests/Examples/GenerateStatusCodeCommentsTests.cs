using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Examples;

public class GenerateStatusCodeCommentsTests
{
    private const string OpenApiSpec = @"
openapi: 3.0.0
info:
  title: Test API
  version: 1.0.0
paths:
  /users:
    get:
      operationId: getUsers
      responses:
        '200':
          description: Successful operation
          content:
            application/json:
              schema:
                type: array
                items:
                  $ref: '#/components/schemas/User'
        '400':
          description: Bad request
        '404':
          description: Not found
        '500':
          description: Internal server error
    post:
      operationId: createUser
      requestBody:
        content:
          application/json:
            schema:
              $ref: '#/components/schemas/User'
      responses:
        '201':
          description: Created
        '400':
          description: Invalid input
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

    private const string OpenApiSpecWithUnicodeStatusCodeComments = @"
openapi: 3.0.0
info:
  title: Unicode Test API
  version: 1.0.0
paths:
  /directories:
    get:
      operationId: getDirectories
      responses:
        '200':
          description: Возвращает список справочников.
          content:
            application/json:
              schema:
                type: string
        '400':
          description: Ошибка запроса
";

    [Test]
    public async Task Can_Generate_Code()
    {
        string generatedCode = await GenerateCode(generateStatusCodeComments: true);
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Can_Build_Generated_Code()
    {
        string generatedCode = await GenerateCode(generateStatusCodeComments: true);
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    [Test]
    public async Task Generated_Code_Contains_Status_Code_Comments_When_Enabled()
    {
        string generatedCode = await GenerateCode(generateStatusCodeComments: true);
        generatedCode.Should().Contain("400");
        generatedCode.Should().Contain("404");
        generatedCode.Should().Contain("500");
    }

    [Test]
    public async Task Generated_Code_Does_Not_Contain_Status_Code_Comments_When_Disabled()
    {
        string generatedCode = await GenerateCode(generateStatusCodeComments: false);

        // Verify the code still contains methods but without detailed status code documentation
        generatedCode.Should().Contain("GetUsers");
        generatedCode.Should().Contain("CreateUser");

        // The status codes should appear less frequently when comments are disabled
        var codeWithComments = await GenerateCode(generateStatusCodeComments: true);
        var countWith = CountOccurrences(codeWithComments, "400");
        var countWithout = CountOccurrences(generatedCode, "400");
        countWithout.Should().BeLessThan(countWith);
    }

    [Test]
    public async Task Generated_Code_Contains_Response_Descriptions_When_Comments_Enabled()
    {
        string generatedCode = await GenerateCode(generateStatusCodeComments: true);
        generatedCode.Should().Contain("Successful operation");
        generatedCode.Should().Contain("Bad request");
    }

    [Test]
    public async Task Generated_Code_Preserves_Readable_Unicode_In_Status_Code_Comments()
    {
        string generatedCode = await GenerateCode(OpenApiSpecWithUnicodeStatusCodeComments, generateStatusCodeComments: true);

        generatedCode.Should().Contain("/// <term>400</term>")
            .And.Contain("/// <description>Ошибка запроса</description>")
            .And.NotContain(@"\u041e\u0448");
    }

    [Test]
    public async Task Generated_Code_With_Unicode_Status_Code_Comments_Can_Build()
    {
        string generatedCode = await GenerateCode(OpenApiSpecWithUnicodeStatusCodeComments, generateStatusCodeComments: true);

        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    private static async Task<string> GenerateCode(bool generateStatusCodeComments)
    {
        return await GenerateCode(OpenApiSpec, generateStatusCodeComments);
    }

    private static async Task<string> GenerateCode(string openApiSpec, bool generateStatusCodeComments)
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(openApiSpec);
        try
        {
            var settings = new RefitGeneratorSettings
            {
                OpenApiPath = swaggerFile,
                GenerateStatusCodeComments = generateStatusCodeComments
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

    private static int CountOccurrences(string text, string substring)
    {
        int count = 0;
        int index = 0;
        while ((index = text.IndexOf(substring, index, StringComparison.Ordinal)) != -1)
        {
            count++;
            index += substring.Length;
        }
        return count;
    }
}
