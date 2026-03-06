using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.Build;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests;

public class ParameterExtractorDeepCoverageTests
{
    #region Priority 1: EscapeString Special Characters

    private const string OpenApiSpecWithTabCharacterDefault = @"
{
  ""openapi"": ""3.0.1"",
  ""info"": {
    ""title"": ""Test API"",
    ""version"": ""v1""
  },
  ""paths"": {
    ""/test"": {
      ""get"": {
        ""parameters"": [
          {
            ""name"": ""text"",
            ""in"": ""query"",
            ""schema"": {
              ""type"": ""string"",
              ""default"": ""before\tafter""
            }
          }
        ],
        ""responses"": {
          ""200"": {
            ""description"": ""Success""
          }
        }
      }
    }
  }
}
";

    private const string OpenApiSpecWithCarriageReturnDefault = @"
{
  ""openapi"": ""3.0.1"",
  ""info"": {
    ""title"": ""Test API"",
    ""version"": ""v1""
  },
  ""paths"": {
    ""/test"": {
      ""get"": {
        ""parameters"": [
          {
            ""name"": ""text"",
            ""in"": ""query"",
            ""schema"": {
              ""type"": ""string"",
              ""default"": ""line1\rline2""
            }
          }
        ],
        ""responses"": {
          ""200"": {
            ""description"": ""Success""
          }
        }
      }
    }
  }
}
";

    private const string OpenApiSpecWithFormFeedDefault = @"
{
  ""openapi"": ""3.0.1"",
  ""info"": {
    ""title"": ""Test API"",
    ""version"": ""v1""
  },
  ""paths"": {
    ""/test"": {
      ""get"": {
        ""parameters"": [
          {
            ""name"": ""text"",
            ""in"": ""query"",
            ""schema"": {
              ""type"": ""string"",
              ""default"": ""page1\fpage2""
            }
          }
        ],
        ""responses"": {
          ""200"": {
            ""description"": ""Success""
          }
        }
      }
    }
  }
}
";

    private const string OpenApiSpecWithVerticalTabDefault = @"
{
  ""openapi"": ""3.0.1"",
  ""info"": {
    ""title"": ""Test API"",
    ""version"": ""v1""
  },
  ""paths"": {
    ""/test"": {
      ""get"": {
        ""parameters"": [
          {
            ""name"": ""text"",
            ""in"": ""query"",
            ""schema"": {
              ""type"": ""string"",
              ""default"": ""text1\u000Btext2""
            }
          }
        ],
        ""responses"": {
          ""200"": {
            ""description"": ""Success""
          }
        }
      }
    }
  }
}
";

    private const string OpenApiSpecWithBackspaceDefault = @"
{
  ""openapi"": ""3.0.1"",
  ""info"": {
    ""title"": ""Test API"",
    ""version"": ""v1""
  },
  ""paths"": {
    ""/test"": {
      ""get"": {
        ""parameters"": [
          {
            ""name"": ""text"",
            ""in"": ""query"",
            ""schema"": {
              ""type"": ""string"",
              ""default"": ""text\bhere""
            }
          }
        ],
        ""responses"": {
          ""200"": {
            ""description"": ""Success""
          }
        }
      }
    }
  }
}
";

    private const string OpenApiSpecWithNullCharacterDefault = @"
{
  ""openapi"": ""3.0.1"",
  ""info"": {
    ""title"": ""Test API"",
    ""version"": ""v1""
  },
  ""paths"": {
    ""/test"": {
      ""get"": {
        ""parameters"": [
          {
            ""name"": ""text"",
            ""in"": ""query"",
            ""schema"": {
              ""type"": ""string"",
              ""default"": ""text\u0000null""
            }
          }
        ],
        ""responses"": {
          ""200"": {
            ""description"": ""Success""
          }
        }
      }
    }
  }
}
";

    private const string OpenApiSpecWithAllSpecialCharacters = @"
{
  ""openapi"": ""3.0.1"",
  ""info"": {
    ""title"": ""Test API"",
    ""version"": ""v1""
  },
  ""paths"": {
    ""/test"": {
      ""get"": {
        ""parameters"": [
          {
            ""name"": ""mixed"",
            ""in"": ""query"",
            ""schema"": {
              ""type"": ""string"",
              ""default"": ""tab:\tcr:\rff:\fvt:\u000Bbs:\bnull:\u0000end""
            }
          }
        ],
        ""responses"": {
          ""200"": {
            ""description"": ""Success""
          }
        }
      }
    }
  }
}
";

    [Test]
    public async Task EscapeString_Handles_Tab_Character()
    {
        string generatedCode = await GenerateCode(OpenApiSpecWithTabCharacterDefault);
        generatedCode.Should().Contain(@"\t");
        generatedCode.Should().Contain(@"= ""before\tafter""");
    }

    [Test]
    public async Task EscapeString_Handles_Carriage_Return()
    {
        string generatedCode = await GenerateCode(OpenApiSpecWithCarriageReturnDefault);
        generatedCode.Should().Contain(@"\r");
        generatedCode.Should().Contain(@"= ""line1\rline2""");
    }

    [Test]
    public async Task EscapeString_Handles_Form_Feed()
    {
        string generatedCode = await GenerateCode(OpenApiSpecWithFormFeedDefault);
        generatedCode.Should().Contain(@"\f");
        generatedCode.Should().Contain(@"= ""page1\fpage2""");
    }

    [Test]
    public async Task EscapeString_Handles_Vertical_Tab()
    {
        string generatedCode = await GenerateCode(OpenApiSpecWithVerticalTabDefault);
        generatedCode.Should().Contain(@"\v");
        generatedCode.Should().Contain(@"= ""text1\vtext2""");
    }

    [Test]
    public async Task EscapeString_Handles_Backspace()
    {
        string generatedCode = await GenerateCode(OpenApiSpecWithBackspaceDefault);
        generatedCode.Should().Contain(@"\b");
        generatedCode.Should().Contain(@"= ""text\bhere""");
    }

    [Test]
    public async Task EscapeString_Handles_Null_Character()
    {
        string generatedCode = await GenerateCode(OpenApiSpecWithNullCharacterDefault);
        generatedCode.Should().Contain(@"\0");
        generatedCode.Should().Contain(@"= ""text\0null""");
    }

    [Test]
    public async Task EscapeString_Handles_All_Special_Characters_Together()
    {
        string generatedCode = await GenerateCode(OpenApiSpecWithAllSpecialCharacters);
        generatedCode.Should().Contain(@"\t");
        generatedCode.Should().Contain(@"\r");
        generatedCode.Should().Contain(@"\f");
        generatedCode.Should().Contain(@"\v");
        generatedCode.Should().Contain(@"\b");
        generatedCode.Should().Contain(@"\0");
    }

    [Test]
    public async Task EscapeString_Generated_Code_Builds()
    {
        string generatedCode = await GenerateCode(OpenApiSpecWithAllSpecialCharacters);
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    #endregion

    #region Priority 2: FormatNumericValue Type Suffixes

    private const string OpenApiSpecWithFloatDefault = """
{
  "openapi": "3.0.1",
  "info": {
    "title": "Test API",
    "version": "v1"
  },
  "paths": {
    "/test": {
      "get": {
        "parameters": [
          {
            "name": "floatValue",
            "in": "query",
            "schema": {
              "type": "number",
              "format": "float",
              "default": 3.14
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

    private const string OpenApiSpecWithDecimalDefault = """
{
  "openapi": "3.0.1",
  "info": {
    "title": "Test API",
    "version": "v1"
  },
  "paths": {
    "/test": {
      "get": {
        "parameters": [
          {
            "name": "decimalValue",
            "in": "query",
            "schema": {
              "type": "number",
              "format": "decimal",
              "default": 99.99
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

    private const string OpenApiSpecWithDoubleDefault = """
{
  "openapi": "3.0.1",
  "info": {
    "title": "Test API",
    "version": "v1"
  },
  "paths": {
    "/test": {
      "get": {
        "parameters": [
          {
            "name": "doubleValue",
            "in": "query",
            "schema": {
              "type": "number",
              "format": "double",
              "default": 2.718
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

    private const string OpenApiSpecWithLongDefault = """
{
  "openapi": "3.0.1",
  "info": {
    "title": "Test API",
    "version": "v1"
  },
  "paths": {
    "/test": {
      "get": {
        "parameters": [
          {
            "name": "longValue",
            "in": "query",
            "schema": {
              "type": "integer",
              "format": "int64",
              "default": 9223372036854775807
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

    private const string OpenApiSpecWithUlongDefault = """
{
  "openapi": "3.0.1",
  "info": {
    "title": "Test API",
    "version": "v1"
  },
  "paths": {
    "/test": {
      "get": {
        "parameters": [
          {
            "name": "ulongValue",
            "in": "query",
            "schema": {
              "type": "integer",
              "format": "uint64",
              "default": 18446744073709551615
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

    private const string OpenApiSpecWithUintDefault = """
{
  "openapi": "3.0.1",
  "info": {
    "title": "Test API",
    "version": "v1"
  },
  "paths": {
    "/test": {
      "get": {
        "parameters": [
          {
            "name": "uintValue",
            "in": "query",
            "schema": {
              "type": "integer",
              "format": "uint32",
              "default": 4294967295
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

    private const string OpenApiSpecWithAllNumericTypes = """
{
  "openapi": "3.0.1",
  "info": {
    "title": "Test API",
    "version": "v1"
  },
  "paths": {
    "/test": {
      "get": {
        "parameters": [
          {
            "name": "floatValue",
            "in": "query",
            "schema": {
              "type": "number",
              "format": "float",
              "default": 1.5
            }
          },
          {
            "name": "decimalValue",
            "in": "query",
            "schema": {
              "type": "number",
              "format": "decimal",
              "default": 2.5
            }
          },
          {
            "name": "doubleValue",
            "in": "query",
            "schema": {
              "type": "number",
              "format": "double",
              "default": 3.5
            }
          },
          {
            "name": "longValue",
            "in": "query",
            "schema": {
              "type": "integer",
              "format": "int64",
              "default": 100
            }
          },
          {
            "name": "ulongValue",
            "in": "query",
            "schema": {
              "type": "integer",
              "format": "uint64",
              "default": 200
            }
          },
          {
            "name": "uintValue",
            "in": "query",
            "schema": {
              "type": "integer",
              "format": "uint32",
              "default": 300
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
    public async Task FormatNumericValue_Float_Has_F_Suffix()
    {
        string generatedCode = await GenerateCode(OpenApiSpecWithFloatDefault);
        generatedCode.Should().Contain("3.14f");
        generatedCode.Should().Contain("float floatValue = 3.14f");
    }

    [Test]
    public async Task FormatNumericValue_Decimal_Has_M_Suffix()
    {
        string generatedCode = await GenerateCode(OpenApiSpecWithDecimalDefault);
        generatedCode.Should().Contain("99.99m");
        generatedCode.Should().Contain("decimal decimalValue = 99.99m");
    }

    [Test]
    public async Task FormatNumericValue_Double_Has_Implicit_Suffix()
    {
        string generatedCode = await GenerateCode(OpenApiSpecWithDoubleDefault);
        generatedCode.Should().Contain("double doubleValue = 2.718");
        generatedCode.Should().NotContain("2.718d");
    }

    [Test]
    public async Task FormatNumericValue_Long_Has_L_Suffix()
    {
        string generatedCode = await GenerateCode(OpenApiSpecWithLongDefault);
        generatedCode.Should().Contain("9223372036854775807L");
        generatedCode.Should().Contain("long longValue = 9223372036854775807L");
    }

    [Test]
    public async Task FormatNumericValue_Ulong_Has_UL_Suffix()
    {
        string generatedCode = await GenerateCode(OpenApiSpecWithUlongDefault);
        generatedCode.Should().Contain("18446744073709551615UL");
        generatedCode.Should().Contain("ulong ulongValue = 18446744073709551615UL");
    }

    [Test]
    public async Task FormatNumericValue_Uint_Has_U_Suffix()
    {
        string generatedCode = await GenerateCode(OpenApiSpecWithUintDefault);
        generatedCode.Should().Contain("4294967295U");
        generatedCode.Should().Contain("uint uintValue = 4294967295U");
    }

    [Test]
    public async Task FormatNumericValue_All_Types_Together()
    {
        string generatedCode = await GenerateCode(OpenApiSpecWithAllNumericTypes);
        generatedCode.Should().Contain("1.5f");
        generatedCode.Should().Contain("2.5m");
        generatedCode.Should().Contain("double doubleValue = 3.5");
        generatedCode.Should().Contain("100L");
        generatedCode.Should().Contain("200UL");
        generatedCode.Should().Contain("300U");
    }

    [Test]
    public async Task FormatNumericValue_Generated_Code_Builds()
    {
        string generatedCode = await GenerateCode(OpenApiSpecWithAllNumericTypes);
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    #endregion

    #region Priority 3: GetCSharpType Branches

    private const string OpenApiSpecWithAllParameterTypes = """
{
  "openapi": "3.0.1",
  "info": {
    "title": "Test API",
    "version": "v1"
  },
  "paths": {
    "/test": {
      "get": {
        "parameters": [
          {
            "name": "stringParam",
            "in": "query",
            "schema": {
              "type": "string"
            }
          },
          {
            "name": "intParam",
            "in": "query",
            "schema": {
              "type": "integer"
            }
          },
          {
            "name": "numberParam",
            "in": "query",
            "schema": {
              "type": "number"
            }
          },
          {
            "name": "boolParam",
            "in": "query",
            "schema": {
              "type": "boolean"
            }
          },
          {
            "name": "arrayParam",
            "in": "query",
            "schema": {
              "type": "array",
              "items": {
                "type": "string"
              }
            }
          },
          {
            "name": "objectParam",
            "in": "query",
            "schema": {
              "type": "object"
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

    private const string OpenApiSpecWithNullableParameters = """
{
  "openapi": "3.0.1",
  "info": {
    "title": "Test API",
    "version": "v1"
  },
  "paths": {
    "/test": {
      "get": {
        "parameters": [
          {
            "name": "nullableInt",
            "in": "query",
            "schema": {
              "type": "integer",
              "nullable": true
            }
          },
          {
            "name": "nullableBool",
            "in": "query",
            "schema": {
              "type": "boolean",
              "nullable": true
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

    private const string OpenApiSpecWithArrayOfIntegers = """
{
  "openapi": "3.0.1",
  "info": {
    "title": "Test API",
    "version": "v1"
  },
  "paths": {
    "/test": {
      "get": {
        "parameters": [
          {
            "name": "intArray",
            "in": "query",
            "schema": {
              "type": "array",
              "items": {
                "type": "integer"
              }
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

    private const string OpenApiSpecWithArrayOfBooleans = """
{
  "openapi": "3.0.1",
  "info": {
    "title": "Test API",
    "version": "v1"
  },
  "paths": {
    "/test": {
      "get": {
        "parameters": [
          {
            "name": "boolArray",
            "in": "query",
            "schema": {
              "type": "array",
              "items": {
                "type": "boolean"
              }
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
    public async Task GetCSharpType_String_Type()
    {
        string generatedCode = await GenerateCode(OpenApiSpecWithAllParameterTypes);
        generatedCode.Should().Contain("string stringParam");
    }

    [Test]
    public async Task GetCSharpType_Integer_Type()
    {
        string generatedCode = await GenerateCode(OpenApiSpecWithAllParameterTypes);
        generatedCode.Should().Contain("int intParam");
    }

    [Test]
    public async Task GetCSharpType_Number_Type()
    {
        string generatedCode = await GenerateCode(OpenApiSpecWithAllParameterTypes);
        generatedCode.Should().Contain("double numberParam");
    }

    [Test]
    public async Task GetCSharpType_Boolean_Type()
    {
        string generatedCode = await GenerateCode(OpenApiSpecWithAllParameterTypes);
        generatedCode.Should().Contain("bool boolParam");
    }

    [Test]
    public async Task GetCSharpType_Array_Type()
    {
        string generatedCode = await GenerateCode(OpenApiSpecWithAllParameterTypes);
        generatedCode.Should().Contain("string[] arrayParam");
    }

    [Test]
    public async Task GetCSharpType_Object_Type()
    {
        string generatedCode = await GenerateCode(OpenApiSpecWithAllParameterTypes);
        generatedCode.Should().Contain("object objectParam");
    }

    [Test]
    public async Task GetCSharpType_Nullable_With_OptionalParameters()
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpecWithNullableParameters);
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = swaggerFile,
            OptionalParameters = true
        };
        var generator = await RefitGenerator.CreateAsync(settings);
        string generatedCode = generator.Generate();

        generatedCode.Should().Contain("int? nullableInt");
        generatedCode.Should().Contain("bool? nullableBool");
    }

    [Test]
    public async Task GetCSharpType_Array_Of_Integers()
    {
        string generatedCode = await GenerateCode(OpenApiSpecWithArrayOfIntegers);
        generatedCode.Should().Contain("int[] intArray");
    }

    [Test]
    public async Task GetCSharpType_Array_Of_Booleans()
    {
        string generatedCode = await GenerateCode(OpenApiSpecWithArrayOfBooleans);
        generatedCode.Should().Contain("bool[] boolArray");
    }

    [Test]
    public async Task GetCSharpType_All_Types_Build()
    {
        string generatedCode = await GenerateCode(OpenApiSpecWithAllParameterTypes);
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    #endregion

    #region Priority 4: GetIntegerTypeName Format Checks

    private const string OpenApiSpecWithInt32Format = """
{
  "openapi": "3.0.1",
  "info": {
    "title": "Test API",
    "version": "v1"
  },
  "paths": {
    "/test": {
      "get": {
        "parameters": [
          {
            "name": "int32Value",
            "in": "query",
            "schema": {
              "type": "integer",
              "format": "int32"
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

    private const string OpenApiSpecWithInt64Format = """
{
  "openapi": "3.0.1",
  "info": {
    "title": "Test API",
    "version": "v1"
  },
  "paths": {
    "/test": {
      "get": {
        "parameters": [
          {
            "name": "int64Value",
            "in": "query",
            "schema": {
              "type": "integer",
              "format": "int64"
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

    private const string OpenApiSpecWithIntegerNoFormat = """
{
  "openapi": "3.0.1",
  "info": {
    "title": "Test API",
    "version": "v1"
  },
  "paths": {
    "/test": {
      "get": {
        "parameters": [
          {
            "name": "integerValue",
            "in": "query",
            "schema": {
              "type": "integer"
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
    public async Task GetIntegerTypeName_Int32_Format_Returns_Int()
    {
        string generatedCode = await GenerateCode(OpenApiSpecWithInt32Format);
        generatedCode.Should().Contain("int int32Value");
    }

    [Test]
    public async Task GetIntegerTypeName_Int64_Format_Returns_Long()
    {
        string generatedCode = await GenerateCode(OpenApiSpecWithInt64Format);
        generatedCode.Should().Contain("long int64Value");
    }

    [Test]
    public async Task GetIntegerTypeName_No_Format_Defaults_To_Int()
    {
        string generatedCode = await GenerateCode(OpenApiSpecWithIntegerNoFormat);
        generatedCode.Should().Contain("int integerValue");
    }

    [Test]
    public async Task GetIntegerTypeName_With_Int64_Setting()
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpecWithIntegerNoFormat);
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = swaggerFile,
            CodeGeneratorSettings = new CodeGeneratorSettings
            {
                IntegerType = IntegerType.Int64
            }
        };
        var generator = await RefitGenerator.CreateAsync(settings);
        string generatedCode = generator.Generate();

        generatedCode.Should().Contain("long integerValue");
    }

    [Test]
    public async Task GetIntegerTypeName_Format_Overrides_Setting()
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(OpenApiSpecWithInt32Format);
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = swaggerFile,
            CodeGeneratorSettings = new CodeGeneratorSettings
            {
                IntegerType = IntegerType.Int64
            }
        };
        var generator = await RefitGenerator.CreateAsync(settings);
        string generatedCode = generator.Generate();

        generatedCode.Should().Contain("int int32Value");
    }

    [Test]
    public async Task GetIntegerTypeName_Generated_Code_Builds()
    {
        string generatedCode = await GenerateCode(OpenApiSpecWithInt64Format);
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    #endregion

    #region Priority 5: GetArrayType Null Item

    private const string OpenApiSpecWithArrayNoItems = """
{
  "openapi": "3.0.1",
  "info": {
    "title": "Test API",
    "version": "v1"
  },
  "paths": {
    "/test": {
      "get": {
        "parameters": [
          {
            "name": "arrayWithoutItems",
            "in": "query",
            "schema": {
              "type": "array"
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
    public async Task GetArrayType_Null_Item_Returns_Object_Array()
    {
        string generatedCode = await GenerateCode(OpenApiSpecWithArrayNoItems);
        generatedCode.Should().Contain("object[] arrayWithoutItems");
    }

    [Test]
    public async Task GetArrayType_Null_Item_Generated_Code_Builds()
    {
        string generatedCode = await GenerateCode(OpenApiSpecWithArrayNoItems);
        BuildHelper.BuildCSharp(generatedCode).Should().BeTrue();
    }

    #endregion

    #region Additional Coverage: Double Literal Formatting

    private const string OpenApiSpecWithDoubleIntegerDefault = """
{
  "openapi": "3.0.1",
  "info": {
    "title": "Test API",
    "version": "v1"
  },
  "paths": {
    "/test": {
      "get": {
        "parameters": [
          {
            "name": "doubleValue",
            "in": "query",
            "schema": {
              "type": "number",
              "format": "double",
              "default": 42
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

    private const string OpenApiSpecWithDoubleExponentDefault = """
{
  "openapi": "3.0.1",
  "info": {
    "title": "Test API",
    "version": "v1"
  },
  "paths": {
    "/test": {
      "get": {
        "parameters": [
          {
            "name": "doubleValue",
            "in": "query",
            "schema": {
              "type": "number",
              "format": "double",
              "default": 1.5e10
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
    public async Task FormatDoubleLiteral_Integer_Value_Adds_Point_Zero()
    {
        string generatedCode = await GenerateCode(OpenApiSpecWithDoubleIntegerDefault);
        generatedCode.Should().Contain("42.0");
        generatedCode.Should().Contain("double doubleValue = 42.0");
    }

    [Test]
    public async Task FormatDoubleLiteral_With_Exponent_No_Point_Zero()
    {
        string generatedCode = await GenerateCode(OpenApiSpecWithDoubleExponentDefault);
        generatedCode.Should().Contain("1.5e10");
        generatedCode.Should().NotContain("1.5e10.0");
    }

    #endregion

    private static async Task<string> GenerateCode(string openApiSpec)
    {
        var swaggerFile = await SwaggerFileHelper.CreateSwaggerFile(openApiSpec);
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = swaggerFile,
            OptionalParameters = true
        };
        var generator = await RefitGenerator.CreateAsync(settings);
        return generator.Generate();
    }
}
