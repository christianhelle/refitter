using FluentAssertions;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;
using Refitter.Core.Validation;
using System.Net.Http;

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
    public void Validate_Should_Return_When_Document_Is_Null()
    {
        OpenApiDiagnostic diagnostic = new();

        AttributeStringValidator.Validate(null, diagnostic);

        diagnostic.Errors.Should().BeEmpty();
    }

    [Test]
    public void Validate_Should_Validate_SecurityScheme_And_Return_When_Paths_Are_Null()
    {
        OpenApiDiagnostic diagnostic = new();
        OpenApiDocument document = new()
        {
            Components = new OpenApiComponents
            {
                SecuritySchemes = new Dictionary<string, IOpenApiSecurityScheme>
                {
                    ["nullScheme"] = null!,
                    ["unsafeHeader"] = new OpenApiSecurityScheme
                    {
                        Type = SecuritySchemeType.ApiKey,
                        In = ParameterLocation.Header,
                        Name = "X\"Api"
                    },
                    ["safeHeader"] = new OpenApiSecurityScheme
                    {
                        Type = SecuritySchemeType.ApiKey,
                        In = ParameterLocation.Header,
                        Name = "X-Api-Key"
                    },
                    ["unsafeQuery"] = new OpenApiSecurityScheme
                    {
                        Type = SecuritySchemeType.ApiKey,
                        In = ParameterLocation.Query,
                        Name = "X\"Api"
                    }
                }
            },
            Paths = null!
        };

        AttributeStringValidator.Validate(document, diagnostic);

        diagnostic.Errors.Should().HaveCount(1);
        diagnostic.Errors[0].Pointer.Should().Be("unsafeHeader");
        diagnostic.Errors[0].Message.Should().Contain("Security scheme 'unsafeHeader'");
    }

    [Test]
    public void Validate_Should_Report_Path_And_Header_Errors_And_Skip_Null_Branches()
    {
        OpenApiDiagnostic diagnostic = new();
        OpenApiDocument document = new()
        {
            Paths = new OpenApiPaths
            {
                ["/nullpath"] = null!,
                ["unsafe\"path"] = new OpenApiPathItem
                {
                    Operations = null!
                },
                ["/safe"] = new OpenApiPathItem
                {
                    Operations = new Dictionary<HttpMethod, OpenApiOperation>
                    {
                        [HttpMethod.Get] = new OpenApiOperation
                        {
                            Parameters = null!
                        },
                        [HttpMethod.Post] = null!,
                        [HttpMethod.Put] = new OpenApiOperation
                        {
                            Parameters = new List<IOpenApiParameter>
                            {
                                new OpenApiParameter
                                {
                                    In = ParameterLocation.Header,
                                    Name = "X\"Bad"
                                },
                                new OpenApiParameter
                                {
                                    In = ParameterLocation.Header,
                                    Name = "X-Good"
                                },
                                new OpenApiParameter
                                {
                                    In = ParameterLocation.Query,
                                    Name = "X\"Ignored"
                                }
                            }
                        }
                    }
                }
            }
        };

        AttributeStringValidator.Validate(document, diagnostic);

        diagnostic.Errors.Should().HaveCount(2);
        diagnostic.Errors.Select(x => x.Pointer).Should().Contain(new[] { "unsafe\"path", "X\"Bad" });
    }

    [Test]
    public void Validate_Should_Report_ContentType_Errors_For_OpenApi3()
    {
        OpenApiDiagnostic diagnostic = new() { SpecificationVersion = OpenApiSpecVersion.OpenApi3_0 };
        OpenApiDocument document = BuildContentTypeDocument();

        AttributeStringValidator.Validate(document, diagnostic);

        diagnostic.Errors.Should().HaveCount(2);
        diagnostic.Errors.Select(x => x.Message).Should().OnlyContain(m => m.Contains("Content type"));
        diagnostic.Errors.Select(x => x.Pointer).Should().Contain(new[] { "application/json\")] x", "text/plain\"" });
    }

    [Test]
    public void Validate_Should_Not_Report_ContentType_Errors_For_OpenApi2()
    {
        OpenApiDiagnostic diagnostic = new() { SpecificationVersion = OpenApiSpecVersion.OpenApi2_0 };
        OpenApiDocument document = BuildContentTypeDocument();

        AttributeStringValidator.Validate(document, diagnostic);

        diagnostic.Errors.Should().BeEmpty();
    }

    private static OpenApiDocument BuildContentTypeDocument() => new()
    {
        Paths = new OpenApiPaths
        {
            ["/api"] = new OpenApiPathItem
            {
                Operations = new Dictionary<HttpMethod, OpenApiOperation>
                {
                    [HttpMethod.Post] = new OpenApiOperation
                    {
                        RequestBody = new OpenApiRequestBody
                        {
                            Content = new Dictionary<string, IOpenApiMediaType>
                            {
                                ["application/json"] = new OpenApiMediaType(),
                                ["application/json\")] x"] = new OpenApiMediaType()
                            }
                        },
                        Responses = new OpenApiResponses
                        {
                            ["200"] = new OpenApiResponse
                            {
                                Content = new Dictionary<string, IOpenApiMediaType>
                                {
                                    ["text/plain\""] = new OpenApiMediaType()
                                }
                            },
                            ["204"] = null!
                        }
                    }
                }
            }
        }
    };
}
