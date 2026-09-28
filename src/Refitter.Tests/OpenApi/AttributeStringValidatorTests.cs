using AwesomeAssertions;
using Refitter.Core.Validation;
using Refitter.Tests.TestUtilities;

namespace Refitter.Tests.OpenApi;

public class AttributeStringValidatorTests
{
    [Test]
    public void ContainsUnsafeCharacters_Should_Return_False_For_Null_And_Safe_Values()
    {
        AttributeStringValidator.ContainsUnsafeCharacters(null).Should().BeFalse();
        AttributeStringValidator.ContainsUnsafeCharacters("X-Api-Key").Should().BeFalse();
    }

    [Test]
    public void ContainsUnsafeCharacters_Should_Return_True_For_Unsafe_Values()
    {
        AttributeStringValidator.ContainsUnsafeCharacters("X\"Api").Should().BeTrue();
        AttributeStringValidator.ContainsUnsafeCharacters("X\\Api").Should().BeTrue();
        AttributeStringValidator.ContainsUnsafeCharacters("X\nApi").Should().BeTrue();
    }

    [Test]
    public async Task Validate_Should_Reject_Unsafe_Header_Names_Of_ApiKey_Security_Schemes()
    {
        var result = await ValidateAsync(
            """
            openapi: 3.0.1
            info:
              title: Security schemes
              version: 1.0.0
            paths: {}
            components:
              securitySchemes:
                notAMap: scheme
                unsafeHeader:
                  type: apiKey
                  in: header
                  name: X"Api
                safeHeader:
                  type: apiKey
                  in: header
                  name: X-Api-Key
                unsafeQuery:
                  type: apiKey
                  in: query
                  name: X"Api
            """);

        result.Diagnostics.Errors.Should().ContainSingle();
        result.Diagnostics.Errors[0].Pointer.Should().Be("unsafeHeader");
        result.Diagnostics.Errors[0].Message.Should().Contain("Security scheme 'unsafeHeader'");
    }

    [Test]
    public async Task Validate_Should_Reject_Unsafe_Paths_And_Header_Parameter_Names()
    {
        var result = await ValidateAsync(
            """
            openapi: 3.0.1
            info:
              title: Paths and headers
              version: 1.0.0
            paths:
              /unsafe"path:
                get:
                  responses:
                    '200':
                      description: OK
              /safe:
                get:
                  parameters:
                    - name: X"Bad
                      in: header
                      schema:
                        type: string
                    - name: X-Good
                      in: header
                      schema:
                        type: string
                    - name: X"Ignored
                      in: query
                      schema:
                        type: string
                  responses:
                    '200':
                      description: OK
            """);

        result.Diagnostics.Errors.Select(error => error.Pointer)
            .Should().BeEquivalentTo(["/unsafe\"path", "X\"Bad"]);
    }

    [Test]
    public async Task Validate_Should_Reject_Unsafe_Content_Types_In_OpenApi_3()
    {
        var result = await ValidateAsync(
            """
            openapi: 3.0.1
            info:
              title: Content types
              version: 1.0.0
            paths:
              /api:
                post:
                  requestBody:
                    content:
                      application/json: {}
                      'application/json")] x': {}
                  responses:
                    '200':
                      description: OK
                      content:
                        'text/plain"': {}
                    '204':
                      description: No content
            """);

        result.Diagnostics.Errors.Select(error => error.Message).Should().OnlyContain(message => message.Contains("Content type"));
        result.Diagnostics.Errors.Select(error => error.Pointer)
            .Should().BeEquivalentTo(["application/json\")] x", "text/plain\""]);
    }

    [Test]
    public async Task Validate_Should_Not_Reject_Content_Types_In_Swagger_2()
    {
        var result = await ValidateAsync(
            """
            swagger: '2.0'
            info:
              title: Content types
              version: 1.0.0
            paths:
              /api:
                post:
                  consumes:
                    - 'application/json")] x'
                  produces:
                    - 'text/plain"'
                  parameters:
                    - name: body
                      in: body
                      schema:
                        type: object
                  responses:
                    '200':
                      description: OK
                      schema:
                        type: string
            """);

        result.Diagnostics.Errors.Should().BeEmpty();
    }

    [Test]
    public async Task Validate_Should_Accept_An_Operation_Without_Responses()
    {
        var result = await ValidateAsync(
            """
            openapi: 3.0.1
            info:
              title: No responses
              version: 1.0.0
            paths:
              /safe:
                post:
                  requestBody:
                    content:
                      application/json: {}
            """);

        result.Diagnostics.Errors.Should().BeEmpty();
    }

    private static async Task<OpenApiValidationResult> ValidateAsync(string spec) =>
        await OpenApiValidator.Validate(await SwaggerFileHelper.CreateSwaggerFile(spec));
}
