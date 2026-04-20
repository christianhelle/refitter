using System.Reflection;
using System.Runtime.CompilerServices;
using FluentAssertions;
using NJsonSchema;
using NSwag;
using NSwag.CodeGeneration.CSharp.Models;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Examples;

public class IdentifierCorrectnessTests
{
    #region Issue #1018 - Invalid Parameter Identifiers in Multipart Form Data

    private const string MultipartFormDataWithInvalidIdentifiers = """
        {
          "openapi": "3.0.1",
          "info": {
            "title": "Test API",
            "version": "1.0.0"
          },
          "paths": {
            "/upload": {
              "post": {
                "operationId": "UploadFile",
                "requestBody": {
                  "content": {
                    "multipart/form-data": {
                      "schema": {
                        "type": "object",
                        "properties": {
                          "123File": {
                            "type": "string"
                          },
                          "class": {
                            "type": "string"
                          },
                          "event": {
                            "type": "string"
                          },
                          "!special": {
                            "type": "string"
                          }
                        }
                      }
                    }
                  }
                },
                "responses": {
                  "200": {
                    "description": "Success"
                  }
                }
              }
            }
          }
        }
        """;

    private const string MultipartFormDataWithCollidingSanitizedNames = """
        {
          "openapi": "3.0.1",
          "info": {
            "title": "Test API",
            "version": "1.0.0"
          },
          "paths": {
            "/upload": {
              "post": {
                "operationId": "UploadFile",
                "requestBody": {
                  "content": {
                    "multipart/form-data": {
                      "schema": {
                        "type": "object",
                        "properties": {
                          "Class": {
                            "type": "string"
                          },
                          "class": {
                            "type": "string"
                          },
                          "user-name": {
                            "type": "string"
                          },
                          "user_name": {
                            "type": "string"
                          }
                        }
                      }
                    }
                  }
                },
                "responses": {
                  "200": {
                    "description": "Success"
                  }
                }
              }
            }
          }
        }
        """;

    [Test]
    public async Task Can_Generate_Code_With_Invalid_Multipart_Identifiers()
    {
        var generatedCode = await GenerateCode(MultipartFormDataWithInvalidIdentifiers);
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Multipart_LeadingDigit_Identifiers_Are_Prefixed()
    {
        var generatedCode = await GenerateCode(MultipartFormDataWithInvalidIdentifiers);
        generatedCode.Should().Contain("_123File");
        generatedCode.Should().NotContain("string 123File");
    }

    [Test]
    public async Task Multipart_ReservedKeyword_Identifiers_Are_Escaped()
    {
        var generatedCode = await GenerateCode(MultipartFormDataWithInvalidIdentifiers);
        generatedCode.Should().Contain("@class");
        generatedCode.Should().Contain("@event");
    }

    [Test]
    public async Task Multipart_SpecialChar_Identifiers_Are_Sanitized()
    {
        var generatedCode = await GenerateCode(MultipartFormDataWithInvalidIdentifiers);
        generatedCode.Should().Contain("_special");
        generatedCode.Should().NotContain("string !special");
    }

    [Test]
    public async Task Generated_Code_With_Invalid_Multipart_Identifiers_Compiles()
    {
        var generatedCode = await GenerateCode(MultipartFormDataWithInvalidIdentifiers);
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    [Test]
    public async Task Multipart_CamelCased_Reserved_Keywords_Are_Escaped()
    {
        var generatedCode = await GenerateCode(MultipartFormDataWithCollidingSanitizedNames);
        generatedCode.Should().Contain("[AliasAs(\"Class\")] string @class");
    }

    [Test]
    public async Task Multipart_Deduplicates_Sanitized_Variable_Names()
    {
        var generatedCode = await GenerateCode(MultipartFormDataWithCollidingSanitizedNames);
        generatedCode.Should().Contain("[AliasAs(\"Class\")] string @class");
        generatedCode.Should().NotContain("[AliasAs(\"class\")] string @class");
        generatedCode.Should().Contain("[AliasAs(\"user-name\")] string user_name");
        generatedCode.Should().NotContain("[AliasAs(\"user_name\")] string user_name");
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    #endregion

    #region Issue #1019 - Security Scheme Header Identifier Sanitization

    private const string SecuritySchemeWithInvalidIdentifiers = """
        {
          "openapi": "3.0.1",
          "info": {
            "title": "Test API",
            "version": "1.0.0"
          },
          "paths": {
            "/secure": {
              "get": {
                "operationId": "SecureEndpoint",
                "security": [
                  {
                    "1Token": []
                  },
                  {
                    "class-key": []
                  }
                ],
                "responses": {
                  "200": {
                    "description": "Success"
                  }
                }
              }
            }
          },
          "components": {
            "securitySchemes": {
              "1Token": {
                "type": "apiKey",
                "name": "1Token",
                "in": "header"
              },
              "class-key": {
                "type": "apiKey",
                "name": "class",
                "in": "header"
              }
            }
          }
        }
        """;

    [Test]
    public async Task Can_Generate_Code_With_Invalid_Security_Identifiers()
    {
        var generatedCode = await GenerateCode(
            SecuritySchemeWithInvalidIdentifiers,
            authHeaderStyle: AuthenticationHeaderStyle.Parameter);
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task SecurityScheme_LeadingDigit_Identifiers_Are_Prefixed()
    {
        var generatedCode = await GenerateCode(
            SecuritySchemeWithInvalidIdentifiers,
            authHeaderStyle: AuthenticationHeaderStyle.Parameter);
        generatedCode.Should().Contain("_1Token");
        generatedCode.Should().NotContain("string 1Token");
    }

    [Test]
    public async Task SecurityScheme_ReservedKeyword_Identifiers_Are_Escaped()
    {
        var generatedCode = await GenerateCode(
            SecuritySchemeWithInvalidIdentifiers,
            authHeaderStyle: AuthenticationHeaderStyle.Parameter);
        generatedCode.Should().Contain("@class");
    }

    [Test]
    public async Task Generated_Code_With_Invalid_Security_Identifiers_Compiles()
    {
        var generatedCode = await GenerateCode(
            SecuritySchemeWithInvalidIdentifiers,
            authHeaderStyle: AuthenticationHeaderStyle.Parameter);
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    #endregion

    #region Issue #1020 - Dynamic Querystring Self-Assignment

    private const string QuerystringWithLeadingNonLetter = """
        {
          "openapi": "3.0.1",
          "info": {
            "title": "Test API",
            "version": "1.0.0"
          },
          "paths": {
            "/search": {
              "get": {
                "operationId": "SearchItems",
                "parameters": [
                  {
                    "name": "_foo",
                    "in": "query",
                    "schema": {
                      "type": "string"
                    },
                    "required": true
                  },
                  {
                    "name": "1bar",
                    "in": "query",
                    "schema": {
                      "type": "string"
                    },
                    "required": true
                  }
                ],
                "responses": {
                  "200": {
                    "description": "Success"
                  }
                }
              }
            }
          }
        }
        """;

    [Test]
    public async Task Can_Generate_Code_With_Querystring_Leading_NonLetter()
    {
        var generatedCode = await GenerateCode(
            QuerystringWithLeadingNonLetter,
            useDynamicQuerystringParameters: true);
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Generated_Code_With_Querystring_Leading_NonLetter_Uses_This_Qualifier()
    {
        var generatedCode = await GenerateCode(
            QuerystringWithLeadingNonLetter,
            useDynamicQuerystringParameters: true);

        // Should use "this." prefix to avoid self-assignment when parameter name equals property name
        generatedCode.Should().Contain("this._foo = _foo;");
    }

    [Test]
    public async Task Generated_Code_With_Querystring_Leading_NonLetter_Compiles()
    {
        var generatedCode = await GenerateCode(
            QuerystringWithLeadingNonLetter,
            useDynamicQuerystringParameters: true);
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    #endregion

    #region Issue #1036 - Nullable Parameter Reordering with Generics

    private const string ParametersWithGenerics = """
        {
          "openapi": "3.0.1",
          "info": {
            "title": "Test API",
            "version": "1.0.0"
          },
          "paths": {
            "/filter": {
              "get": {
                "operationId": "FilterItems",
                "parameters": [
                  {
                    "name": "filters",
                    "in": "query",
                    "required": true,
                    "schema": {
                      "type": "object",
                      "additionalProperties": {
                        "type": "string"
                      }
                    }
                  },
                  {
                    "name": "optionalParam",
                    "in": "query",
                    "required": false,
                    "schema": {
                      "type": "string"
                    }
                  }
                ],
                "responses": {
                  "200": {
                    "description": "Success"
                  }
                }
              }
            }
          }
        }
        """;

    [Test]
    public async Task Can_Generate_Code_With_Generic_Parameters()
    {
        var generatedCode = await GenerateCode(ParametersWithGenerics, optionalParameters: true);
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Generic_Dictionary_Parameter_Not_Misclassified_As_Nullable()
    {
        var generatedCode = await GenerateCode(ParametersWithGenerics, optionalParameters: true);

        // Dictionary<string, string?> should NOT be treated as optional based on internal "?"
        // It should come before optional parameters and not have "= default" unless truly optional
        generatedCode.Should().NotContain("IDictionary<string, string?> filters = default");
    }

    [Test]
    public async Task Generated_Code_With_Generic_Parameters_Compiles()
    {
        var generatedCode = await GenerateCode(ParametersWithGenerics, optionalParameters: true);
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    #endregion

    #region Issue #1018 (Swagger 2.0) - Invalid Parameter Identifiers in Multipart Form Data

    private const string MultipartFormDataWithInvalidIdentifiers_Swagger2 = """
        {
          "swagger": "2.0",
          "info": {
            "title": "Test API",
            "version": "1.0.0"
          },
          "host": "localhost",
          "basePath": "/",
          "paths": {
            "/upload": {
              "post": {
                "operationId": "UploadFile",
                "consumes": ["multipart/form-data"],
                "parameters": [
                  {
                    "name": "123File",
                    "in": "formData",
                    "type": "string"
                  },
                  {
                    "name": "class",
                    "in": "formData",
                    "type": "string"
                  },
                  {
                    "name": "event",
                    "in": "formData",
                    "type": "string"
                  },
                  {
                    "name": "!special",
                    "in": "formData",
                    "type": "string"
                  }
                ],
                "responses": {
                  "200": {
                    "description": "Success"
                  }
                }
              }
            }
          }
        }
        """;

    [Test]
    public async Task Can_Generate_Code_With_Invalid_Multipart_Identifiers_Swagger2()
    {
        var generatedCode = await GenerateCode(MultipartFormDataWithInvalidIdentifiers_Swagger2);
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Multipart_LeadingDigit_Identifiers_Are_Prefixed_Swagger2()
    {
        var generatedCode = await GenerateCode(MultipartFormDataWithInvalidIdentifiers_Swagger2);
        generatedCode.Should().Contain("_123File");
        generatedCode.Should().NotContain("string 123File");
    }

    [Test]
    public async Task Multipart_ReservedKeyword_Identifiers_Are_Escaped_Swagger2()
    {
        var generatedCode = await GenerateCode(MultipartFormDataWithInvalidIdentifiers_Swagger2);
        generatedCode.Should().Contain("@class");
        generatedCode.Should().Contain("@event");
    }

    [Test]
    public async Task Multipart_SpecialChar_Identifiers_Are_Sanitized_Swagger2()
    {
        var generatedCode = await GenerateCode(MultipartFormDataWithInvalidIdentifiers_Swagger2);
        generatedCode.Should().Contain("_special");
        generatedCode.Should().NotContain("string !special");
    }

    [Test]
    public async Task Generated_Code_With_Invalid_Multipart_Identifiers_Compiles_Swagger2()
    {
        var generatedCode = await GenerateCode(MultipartFormDataWithInvalidIdentifiers_Swagger2);
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    #endregion

    #region Issue #1019 (Swagger 2.0) - Security Scheme Header Identifier Sanitization

    private const string SecuritySchemeWithInvalidIdentifiers_Swagger2 = """
        {
          "swagger": "2.0",
          "info": {
            "title": "Test API",
            "version": "1.0.0"
          },
          "host": "localhost",
          "basePath": "/",
          "securityDefinitions": {
            "1Token": {
              "type": "apiKey",
              "name": "1Token",
              "in": "header"
            },
            "class-key": {
              "type": "apiKey",
              "name": "class",
              "in": "header"
            }
          },
          "paths": {
            "/secure": {
              "get": {
                "operationId": "SecureEndpoint",
                "security": [
                  {
                    "1Token": []
                  },
                  {
                    "class-key": []
                  }
                ],
                "responses": {
                  "200": {
                    "description": "Success"
                  }
                }
              }
            }
          }
        }
        """;

    [Test]
    public async Task Can_Generate_Code_With_Invalid_Security_Identifiers_Swagger2()
    {
        var generatedCode = await GenerateCode(
            SecuritySchemeWithInvalidIdentifiers_Swagger2,
            authHeaderStyle: AuthenticationHeaderStyle.Parameter);
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task SecurityScheme_LeadingDigit_Identifiers_Are_Prefixed_Swagger2()
    {
        var generatedCode = await GenerateCode(
            SecuritySchemeWithInvalidIdentifiers_Swagger2,
            authHeaderStyle: AuthenticationHeaderStyle.Parameter);
        generatedCode.Should().Contain("_1Token");
        generatedCode.Should().NotContain("string 1Token");
    }

    [Test]
    public async Task SecurityScheme_ReservedKeyword_Identifiers_Are_Escaped_Swagger2()
    {
        var generatedCode = await GenerateCode(
            SecuritySchemeWithInvalidIdentifiers_Swagger2,
            authHeaderStyle: AuthenticationHeaderStyle.Parameter);
        generatedCode.Should().Contain("@class");
    }

    [Test]
    public async Task Generated_Code_With_Invalid_Security_Identifiers_Compiles_Swagger2()
    {
        var generatedCode = await GenerateCode(
            SecuritySchemeWithInvalidIdentifiers_Swagger2,
            authHeaderStyle: AuthenticationHeaderStyle.Parameter);
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    #endregion

    #region Issue #1020 (Swagger 2.0) - Dynamic Querystring Self-Assignment

    private const string QuerystringWithLeadingNonLetter_Swagger2 = """
        {
          "swagger": "2.0",
          "info": {
            "title": "Test API",
            "version": "1.0.0"
          },
          "host": "localhost",
          "basePath": "/",
          "paths": {
            "/search": {
              "get": {
                "operationId": "SearchItems",
                "parameters": [
                  {
                    "name": "_foo",
                    "in": "query",
                    "type": "string",
                    "required": true
                  },
                  {
                    "name": "1bar",
                    "in": "query",
                    "type": "string",
                    "required": true
                  }
                ],
                "responses": {
                  "200": {
                    "description": "Success"
                  }
                }
              }
            }
          }
        }
        """;

    [Test]
    public async Task Can_Generate_Code_With_Querystring_Leading_NonLetter_Swagger2()
    {
        var generatedCode = await GenerateCode(
            QuerystringWithLeadingNonLetter_Swagger2,
            useDynamicQuerystringParameters: true);
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Generated_Code_With_Querystring_Leading_NonLetter_Uses_This_Qualifier_Swagger2()
    {
        var generatedCode = await GenerateCode(
            QuerystringWithLeadingNonLetter_Swagger2,
            useDynamicQuerystringParameters: true);

        // Should use "this." prefix to avoid self-assignment when parameter name equals property name
        generatedCode.Should().Contain("this._foo = _foo;");
    }

    [Test]
    public async Task Generated_Code_With_Querystring_Leading_NonLetter_Compiles_Swagger2()
    {
        var generatedCode = await GenerateCode(
            QuerystringWithLeadingNonLetter_Swagger2,
            useDynamicQuerystringParameters: true);
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    #endregion

    #region Issue #1036 (Swagger 2.0) - Nullable Parameter Reordering with Generics

    private const string ParametersWithGenerics_Swagger2 = """
        {
          "swagger": "2.0",
          "info": {
            "title": "Test API",
            "version": "1.0.0"
          },
          "host": "localhost",
          "basePath": "/",
          "paths": {
            "/filter": {
              "get": {
                "operationId": "FilterItems",
                "parameters": [
                  {
                    "name": "filters",
                    "in": "query",
                    "required": true,
                    "type": "object",
                    "additionalProperties": {
                      "type": "string"
                    }
                  },
                  {
                    "name": "optionalParam",
                    "in": "query",
                    "required": false,
                    "type": "string"
                  }
                ],
                "responses": {
                  "200": {
                    "description": "Success"
                  }
                }
              }
            }
          }
        }
        """;

    [Test]
    public async Task Can_Generate_Code_With_Generic_Parameters_Swagger2()
    {
        var generatedCode = await GenerateCode(ParametersWithGenerics_Swagger2, optionalParameters: true);
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    [Test]
    public async Task Generic_Dictionary_Parameter_Not_Misclassified_As_Nullable_Swagger2()
    {
        var generatedCode = await GenerateCode(ParametersWithGenerics_Swagger2, optionalParameters: true);

        // Dictionary<string, string?> should NOT be treated as optional based on internal "?"
        // It should come before optional parameters and not have "= default" unless truly optional
        generatedCode.Should().NotContain("IDictionary<string, string?> filters = default");
    }

    [Test]
    public async Task Generated_Code_With_Generic_Parameters_Compiles_Swagger2()
    {
        var generatedCode = await GenerateCode(ParametersWithGenerics_Swagger2, optionalParameters: true);
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    #endregion

    #region Optional Parameter Identifier Reordering

    [Test]
    public void GetDefaultValueForParameter_Uses_Sanitized_Variable_Name_Fallback()
    {
        var openApiParameter = new OpenApiParameter
        {
            Name = "class",
            Kind = OpenApiParameterKind.Query,
            IsRequired = false,
            Schema = new JsonSchema
            {
                Type = JsonObjectType.String,
                Default = "abc"
            }
        };

        var parameterModel = (CSharpParameterModel)RuntimeHelpers.GetUninitializedObject(typeof(CSharpParameterModel));
        var baseType = typeof(CSharpParameterModel).BaseType!;

        baseType
            .GetField("<Type>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(parameterModel, "string");
        baseType
            .GetField("<Name>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(parameterModel, "class");
        baseType
            .GetField("<VariableName>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(parameterModel, "class");
        baseType
            .GetField("_parameter", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(parameterModel, openApiParameter);

        var method = typeof(ParameterExtractor)
            .GetMethod("GetDefaultValueForParameter", BindingFlags.NonPublic | BindingFlags.Static);

        var result = method!.Invoke(
            null,
            ["string? @class", new List<CSharpParameterModel> { parameterModel }]);

        result.Should().Be("\"abc\"");
    }

    #endregion

    #region Issue #1037 - Empty Namespace List Crash

    [Test]
    public async Task Can_Generate_Code_With_Empty_Namespace_List()
    {
        var spec = """
            {
              "openapi": "3.0.1",
              "info": {
                "title": "Test API",
                "version": "1.0.0"
              },
              "paths": {
                "/test": {
                  "get": {
                    "operationId": "TestEndpoint",
                    "responses": {
                      "200": {
                        "description": "Success"
                      }
                    }
                  }
                }
              }
            }
            """;

        // Should not throw InvalidOperationException when all namespaces are excluded
        var generatedCode = await GenerateCode(spec, excludeNamespaces: new[] { ".*" });
        generatedCode.Should().NotBeNullOrWhiteSpace();
    }

    #endregion

    #region Issue #1053 - Reserved Keyword Escaping

    [Test]
    public void Sanitize_Escapes_Underscore_Keywords()
    {
        "__arglist".Sanitize().Should().Be("@__arglist");
        "__makeref".Sanitize().Should().Be("@__makeref");
        "__reftype".Sanitize().Should().Be("@__reftype");
        "__refvalue".Sanitize().Should().Be("@__refvalue");
    }

    [Test]
    public void Sanitize_Escapes_Common_Keywords()
    {
        "class".Sanitize().Should().Be("@class");
        "namespace".Sanitize().Should().Be("@namespace");
        "return".Sanitize().Should().Be("@return");
    }

    #endregion

    #region Helper Methods

    private static async Task<string> GenerateCode(
        string openApiSpec,
        AuthenticationHeaderStyle authHeaderStyle = AuthenticationHeaderStyle.None,
        bool optionalParameters = false,
        string[]? excludeNamespaces = null,
        bool useDynamicQuerystringParameters = false)
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(openApiSpec);
        try
        {
            var settings = new RefitGeneratorSettings
            {
                OpenApiPath = swaggerFile,
                Namespace = "TestNamespace",
                AuthenticationHeaderStyle = authHeaderStyle,
                OptionalParameters = optionalParameters,
                ExcludeNamespaces = excludeNamespaces ?? Array.Empty<string>(),
                UseDynamicQuerystringParameters = useDynamicQuerystringParameters
            };
            var generator = await RefitGenerator.CreateAsync(settings);
            return generator.Generate();
        }
        finally
        {
            if (File.Exists(swaggerFile))
                File.Delete(swaggerFile);
            var directory = Path.GetDirectoryName(swaggerFile);
            if (directory != null && Directory.Exists(directory) && !Directory.EnumerateFiles(directory).Any())
                Directory.Delete(directory);
        }
    }

    #endregion
}
