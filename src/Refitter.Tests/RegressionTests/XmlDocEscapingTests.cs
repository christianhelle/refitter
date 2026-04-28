using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.RegressionTests;

/// <summary>
/// Regression tests for Issue #1035: XML doc emission does not escape user-supplied parameter / dynamic-querystring descriptions
/// Validates that parameter descriptions containing XML-unsafe characters (&, <, >) are properly escaped in generated XML documentation
/// </summary>
public class XmlDocEscapingTests
{
    private const string SpecWithUnsafeParameterDescription = @"
{
  ""openapi"": ""3.0.1"",
  ""info"": {
    ""title"": ""Test API"",
    ""version"": ""v1""
  },
  ""paths"": {
    ""/api/search"": {
      ""get"": {
        ""operationId"": ""SearchItems"",
        ""summary"": ""Search for items"",
        ""parameters"": [
          {
            ""name"": ""query"",
            ""in"": ""query"",
            ""description"": ""Search filter: use & for AND, < for less than, > for greater than"",
            ""required"": true,
            ""schema"": {
              ""type"": ""string""
            }
          },
          {
            ""name"": ""limit"",
            ""in"": ""query"",
            ""description"": ""Maximum results (e.g., limit<100 & offset>0)"",
            ""schema"": {
              ""type"": ""integer""
            }
          }
        ],
        ""responses"": {
          ""200"": {
            ""description"": ""Success response with items list"",
            ""content"": {
              ""application/json"": {
                ""schema"": {
                  ""type"": ""array"",
                  ""items"": {
                    ""$ref"": ""#/components/schemas/Item""
                  }
                }
              }
            }
          }
        }
      }
    }
  },
  ""components"": {
    ""schemas"": {
      ""Item"": {
        ""type"": ""object"",
        ""properties"": {
          ""id"": {
            ""type"": ""integer""
          },
          ""name"": {
            ""type"": ""string""
          }
        }
      }
    }
  }
}
";

    private const string SpecWithUnsafeResponseDescription = @"
{
  ""openapi"": ""3.0.1"",
  ""info"": {
    ""title"": ""Test API"",
    ""version"": ""v1""
  },
  ""paths"": {
    ""/api/data"": {
      ""get"": {
        ""operationId"": ""GetData"",
        ""responses"": {
          ""200"": {
            ""description"": ""Success: returns data where x<100 & y>0"",
            ""content"": {
              ""application/json"": {
                ""schema"": {
                  ""type"": ""object"",
                  ""properties"": {
                    ""value"": {
                      ""type"": ""string""
                    }
                  }
                }
              }
            }
          },
          ""400"": {
            ""description"": ""Bad request: parameter must be x<10 & validated>always""
          }
        }
      }
    }
  }
}
";

    private const string SpecWithDynamicQuerystringUnsafeDescription = @"
{
  ""openapi"": ""3.0.1"",
  ""info"": {
    ""title"": ""Test API"",
    ""version"": ""v1""
  },
  ""paths"": {
    ""/api/items"": {
      ""get"": {
        ""operationId"": ""GetItems"",
        ""parameters"": [
          {
            ""name"": ""filter"",
            ""in"": ""query"",
            ""description"": ""Filter syntax: name&value or id<100"",
            ""schema"": {
              ""type"": ""string""
            }
          },
          {
            ""name"": ""sort"",
            ""in"": ""query"",
            ""description"": ""Sort by: asc<desc or name>value"",
            ""schema"": {
              ""type"": ""string""
            }
          },
          {
            ""name"": ""page"",
            ""in"": ""query"",
            ""description"": ""Page number: must be >0 & <1000"",
            ""schema"": {
              ""type"": ""integer""
            }
          }
        ],
        ""responses"": {
          ""200"": {
            ""description"": ""Items retrieved"",
            ""content"": {
              ""application/json"": {
                ""schema"": {
                  ""type"": ""array"",
                  ""items"": {
                    ""type"": ""object"",
                    ""properties"": {
                      ""id"": { ""type"": ""integer"" },
                      ""name"": { ""type"": ""string"" }
                    }
                  }
                }
              }
            }
          }
        }
      }
    }
  }
}
";

    private const string SpecWithSwagger2UnsafeParameterDescription = @"
{
  ""swagger"": ""2.0"",
  ""info"": {
    ""title"": ""Test API"",
    ""version"": ""v1""
  },
  ""host"": ""localhost"",
  ""basePath"": ""/"",
  ""paths"": {
    ""/api/search"": {
      ""get"": {
        ""operationId"": ""SearchItems"",
        ""summary"": ""Search for items"",
        ""parameters"": [
          {
            ""name"": ""query"",
            ""in"": ""query"",
            ""description"": ""Search filter: use & for AND, < for less than, > for greater than"",
            ""required"": true,
            ""type"": ""string""
          },
          {
            ""name"": ""limit"",
            ""in"": ""query"",
            ""description"": ""Maximum results (e.g., limit<100 & offset>0)"",
            ""type"": ""integer""
          }
        ],
        ""responses"": {
          ""200"": {
            ""description"": ""Success response with items list"",
            ""schema"": {
              ""type"": ""array"",
              ""items"": {
                ""$ref"": ""#/definitions/Item""
              }
            }
          }
        }
      }
    }
  },
  ""definitions"": {
    ""Item"": {
      ""type"": ""object"",
      ""properties"": {
        ""id"": { ""type"": ""integer"" },
        ""name"": { ""type"": ""string"" }
      }
    }
  }
}
";

    private const string SpecWithSwagger2UnsafeResponseDescription = @"
{
  ""swagger"": ""2.0"",
  ""info"": {
    ""title"": ""Test API"",
    ""version"": ""v1""
  },
  ""host"": ""localhost"",
  ""basePath"": ""/"",
  ""paths"": {
    ""/api/data"": {
      ""get"": {
        ""operationId"": ""GetData"",
        ""responses"": {
          ""200"": {
            ""description"": ""Success: returns data where x<100 & y>0"",
            ""schema"": {
              ""type"": ""object"",
              ""properties"": {
                ""value"": { ""type"": ""string"" }
              }
            }
          },
          ""400"": {
            ""description"": ""Bad request: parameter must be x<10 & validated>always""
          }
        }
      }
    }
  }
}
";

    private const string SpecWithSwagger2DynamicQuerystringUnsafeDescription = @"
{
  ""swagger"": ""2.0"",
  ""info"": {
    ""title"": ""Test API"",
    ""version"": ""v1""
  },
  ""host"": ""localhost"",
  ""basePath"": ""/"",
  ""paths"": {
    ""/api/items"": {
      ""get"": {
        ""operationId"": ""GetItems"",
        ""parameters"": [
          {
            ""name"": ""filter"",
            ""in"": ""query"",
            ""description"": ""Filter syntax: name&value or id<100"",
            ""type"": ""string""
          },
          {
            ""name"": ""sort"",
            ""in"": ""query"",
            ""description"": ""Sort by: asc<desc or name>value"",
            ""type"": ""string""
          },
          {
            ""name"": ""page"",
            ""in"": ""query"",
            ""description"": ""Page number: must be >0 & <1000"",
            ""type"": ""integer""
          }
        ],
        ""responses"": {
          ""200"": {
            ""description"": ""Items retrieved"",
            ""schema"": {
              ""type"": ""array"",
              ""items"": {
                ""type"": ""object"",
                ""properties"": {
                  ""id"": { ""type"": ""integer"" },
                  ""name"": { ""type"": ""string"" }
                }
              }
            }
          }
        }
      }
    }
  }
}
";

    [Test]
    public async Task Should_Escape_Ampersand_In_Parameter_Description()
    {
        string generatedCode = await GenerateCode(SpecWithUnsafeParameterDescription);

        // The & should be escaped to &amp;
        generatedCode.Should().Contain("&amp;");
        generatedCode.Should().NotContain("/// <summary>\n                /// Search filter: use & for");
    }

    [Test]
    public async Task Should_Escape_Less_Than_In_Parameter_Description()
    {
        string generatedCode = await GenerateCode(SpecWithUnsafeParameterDescription);

        // The < should be escaped to &lt;
        generatedCode.Should().Contain("&lt;");
    }

    [Test]
    public async Task Should_Escape_Greater_Than_In_Parameter_Description()
    {
        string generatedCode = await GenerateCode(SpecWithUnsafeParameterDescription);

        // The > should be escaped to &gt;
        generatedCode.Should().Contain("&gt;");
    }

    [Test]
    public async Task Should_Escape_Multiple_Unsafe_Chars_In_Parameter_Description()
    {
        string generatedCode = await GenerateCode(SpecWithUnsafeParameterDescription);

        // Both occurrences of multiple unsafe chars should be escaped
        generatedCode.Should().Contain("&lt;100 &amp; offset&gt;0");
    }

    [Test]
    public async Task Should_Escape_Unsafe_Chars_In_Response_Description()
    {
        string generatedCode = await GenerateCode(SpecWithUnsafeResponseDescription);

        // Response descriptions should also have unsafe chars escaped
        generatedCode.Should().Contain("Success: returns data where x&lt;100 &amp; y&gt;0");
    }

    [Test]
    public async Task Should_Escape_Unsafe_Chars_In_Exception_Description()
    {
        string generatedCode = await GenerateCode(SpecWithUnsafeResponseDescription, generateStatusCodeComments: true);

        // Exception descriptions in status code comments should be escaped
        generatedCode.Should().Contain("&lt;10 &amp;");
    }

    [Test]
    public async Task Should_Escape_Unsafe_Chars_In_Dynamic_Querystring_Parameter_Descriptions()
    {
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = await SwaggerFileHelper.CreateSwaggerFile(SpecWithDynamicQuerystringUnsafeDescription),
            UseDynamicQuerystringParameters = true
        };

        var sut = await RefitGenerator.CreateAsync(settings);
        var generatedCode = sut.Generate();

        // Dynamic querystring property descriptions should be escaped
        generatedCode.Should().Contain("&amp;");
        generatedCode.Should().Contain("&lt;");
        generatedCode.Should().Contain("&gt;");
    }

    [Test]
    public async Task Can_Build_Generated_Code_With_Escaped_Parameter_Descriptions()
    {
        string generatedCode = await GenerateCode(SpecWithUnsafeParameterDescription);

        // Verify the code compiles (the main validation)
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue(
            "Generated code with escaped parameter descriptions should compile");
    }

    [Test]
    public async Task Can_Build_Generated_Code_With_Escaped_Response_Descriptions()
    {
        string generatedCode = await GenerateCode(SpecWithUnsafeResponseDescription, generateStatusCodeComments: true);

        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue(
            "Generated code with escaped response descriptions should compile");
    }

    [Test]
    public async Task Can_Build_Generated_Code_With_Escaped_Dynamic_Querystring_Descriptions()
    {
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = await SwaggerFileHelper.CreateSwaggerFile(SpecWithDynamicQuerystringUnsafeDescription),
            UseDynamicQuerystringParameters = true
        };

        var sut = await RefitGenerator.CreateAsync(settings);
        var generatedCode = sut.Generate();

        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue(
            "Generated code with escaped dynamic querystring descriptions should compile");
    }

    // ── Swagger 2.0 companion tests ─────────────────────────────────────────

    [Test]
    public async Task Should_Escape_Ampersand_In_Parameter_Description_Swagger2()
    {
        string generatedCode = await GenerateCode(SpecWithSwagger2UnsafeParameterDescription);

        generatedCode.Should().Contain("&amp;");
        generatedCode.Should().NotContain("/// <summary>\n                /// Search filter: use & for");
    }

    [Test]
    public async Task Should_Escape_Less_Than_In_Parameter_Description_Swagger2()
    {
        string generatedCode = await GenerateCode(SpecWithSwagger2UnsafeParameterDescription);

        generatedCode.Should().Contain("&lt;");
    }

    [Test]
    public async Task Should_Escape_Greater_Than_In_Parameter_Description_Swagger2()
    {
        string generatedCode = await GenerateCode(SpecWithSwagger2UnsafeParameterDescription);

        generatedCode.Should().Contain("&gt;");
    }

    [Test]
    public async Task Should_Escape_Multiple_Unsafe_Chars_In_Parameter_Description_Swagger2()
    {
        string generatedCode = await GenerateCode(SpecWithSwagger2UnsafeParameterDescription);

        generatedCode.Should().Contain("&lt;100 &amp; offset&gt;0");
    }

    [Test]
    public async Task Should_Escape_Unsafe_Chars_In_Response_Description_Swagger2()
    {
        string generatedCode = await GenerateCode(SpecWithSwagger2UnsafeResponseDescription);

        generatedCode.Should().Contain("Success: returns data where x&lt;100 &amp; y&gt;0");
    }

    [Test]
    public async Task Should_Escape_Unsafe_Chars_In_Exception_Description_Swagger2()
    {
        string generatedCode = await GenerateCode(SpecWithSwagger2UnsafeResponseDescription, generateStatusCodeComments: true);

        generatedCode.Should().Contain("&lt;10 &amp;");
    }

    [Test]
    public async Task Should_Escape_Unsafe_Chars_In_Dynamic_Querystring_Parameter_Descriptions_Swagger2()
    {
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = await SwaggerFileHelper.CreateSwaggerFile(SpecWithSwagger2DynamicQuerystringUnsafeDescription),
            UseDynamicQuerystringParameters = true
        };

        var sut = await RefitGenerator.CreateAsync(settings);
        var generatedCode = sut.Generate();

        generatedCode.Should().Contain("&amp;");
        generatedCode.Should().Contain("&lt;");
        generatedCode.Should().Contain("&gt;");
    }

    [Test]
    public async Task Can_Build_Generated_Code_With_Escaped_Parameter_Descriptions_Swagger2()
    {
        string generatedCode = await GenerateCode(SpecWithSwagger2UnsafeParameterDescription);

        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue(
            "Generated code with escaped parameter descriptions should compile");
    }

    [Test]
    public async Task Can_Build_Generated_Code_With_Escaped_Response_Descriptions_Swagger2()
    {
        string generatedCode = await GenerateCode(SpecWithSwagger2UnsafeResponseDescription, generateStatusCodeComments: true);

        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue(
            "Generated code with escaped response descriptions should compile");
    }

    [Test]
    public async Task Can_Build_Generated_Code_With_Escaped_Dynamic_Querystring_Descriptions_Swagger2()
    {
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = await SwaggerFileHelper.CreateSwaggerFile(SpecWithSwagger2DynamicQuerystringUnsafeDescription),
            UseDynamicQuerystringParameters = true
        };

        var sut = await RefitGenerator.CreateAsync(settings);
        var generatedCode = sut.Generate();

        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue(
            "Generated code with escaped dynamic querystring descriptions should compile");
    }

    private static async Task<string> GenerateCode(string spec, bool generateStatusCodeComments = false)
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(spec);
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = swaggerFile,
            GenerateStatusCodeComments = generateStatusCodeComments
        };

        var sut = await RefitGenerator.CreateAsync(settings);
        var generatedCode = sut.Generate();
        return generatedCode;
    }
}
