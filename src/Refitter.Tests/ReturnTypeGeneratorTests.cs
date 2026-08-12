using FluentAssertions;
using NSwag;
using Refitter.Core;
using TUnit.Core;

namespace Refitter.Tests;


public class ReturnTypeGeneratorTests
{
    [Test]
    public async Task Generate_Returns_Task_For_Void_Response()
    {
        var spec = """
            {
              "openapi": "3.0.0",
              "info": { "title": "Test", "version": "1.0" },
              "paths": {
                "/test": {
                  "get": {
                    "operationId": "getTest",
                    "responses": {
                      "204": { "description": "No Content" }
                    }
                  }
                }
              }
            }
            """;

        var document = await OpenApiDocument.FromJsonAsync(spec);
        var settings = new RefitGeneratorSettings();
        var generator = new CSharpClientGeneratorFactory(settings, document).Create();
        var sut = new ReturnTypeGenerator(settings, generator);

        var operation = document.Paths["/test"]["get"];
        var result = sut.Generate(operation);

        result.Should().Be("Task");
    }

    [Test]
    public async Task Generate_Returns_Task_Of_Type_For_Success_Response()
    {
        var spec = """
            {
              "openapi": "3.0.0",
              "info": { "title": "Test", "version": "1.0" },
              "paths": {
                "/test": {
                  "get": {
                    "operationId": "getTest",
                    "responses": {
                      "200": {
                        "description": "Success",
                        "content": {
                          "application/json": {
                            "schema": {
                              "type": "string"
                            }
                          }
                        }
                      }
                    }
                  }
                }
              }
            }
            """;

        var document = await OpenApiDocument.FromJsonAsync(spec);
        var settings = new RefitGeneratorSettings();
        var generator = new CSharpClientGeneratorFactory(settings, document).Create();
        var sut = new ReturnTypeGenerator(settings, generator);

        var operation = document.Paths["/test"]["get"];
        var result = sut.Generate(operation);

        result.Should().Be("Task<string>");
    }

    [Test]
    public async Task Generate_Returns_IApiResponse_When_Wrapping_Enabled()
    {
        var spec = """
            {
              "openapi": "3.0.0",
              "info": { "title": "Test", "version": "1.0" },
              "paths": {
                "/test": {
                  "get": {
                    "operationId": "getTest",
                    "responses": {
                      "200": {
                        "description": "Success",
                        "content": {
                          "application/json": {
                            "schema": {
                              "type": "string"
                            }
                          }
                        }
                      }
                    }
                  }
                }
              }
            }
            """;

        var document = await OpenApiDocument.FromJsonAsync(spec);
        var settings = new RefitGeneratorSettings { ReturnIApiResponse = true };
        var generator = new CSharpClientGeneratorFactory(settings, document).Create();
        var sut = new ReturnTypeGenerator(settings, generator);

        var operation = document.Paths["/test"]["get"];
        var result = sut.Generate(operation);

        result.Should().Be("Task<IApiResponse<string>>");
    }

    [Test]
    public async Task Generate_Returns_IObservable_When_Observable_Enabled()
    {
        var spec = """
            {
              "openapi": "3.0.0",
              "info": { "title": "Test", "version": "1.0" },
              "paths": {
                "/test": {
                  "get": {
                    "operationId": "getTest",
                    "responses": {
                      "200": {
                        "description": "Success",
                        "content": {
                          "application/json": {
                            "schema": {
                              "type": "string"
                            }
                          }
                        }
                      }
                    }
                  }
                }
              }
            }
            """;

        var document = await OpenApiDocument.FromJsonAsync(spec);
        var settings = new RefitGeneratorSettings { ReturnIObservable = true };
        var generator = new CSharpClientGeneratorFactory(settings, document).Create();
        var sut = new ReturnTypeGenerator(settings, generator);

        var operation = document.Paths["/test"]["get"];
        var result = sut.Generate(operation);

        result.Should().Be("IObservable<string>");
    }

    [Test]
    public async Task IsFileStreamResponse_Returns_True_For_Binary_Content()
    {
        var spec = """
            {
              "openapi": "3.0.0",
              "info": { "title": "Test", "version": "1.0" },
              "paths": {
                "/test": {
                  "get": {
                    "operationId": "getFile",
                    "responses": {
                      "200": {
                        "description": "File",
                        "content": {
                          "application/octet-stream": {
                            "schema": {
                              "type": "string",
                              "format": "binary"
                            }
                          }
                        }
                      }
                    }
                  }
                }
              }
            }
            """;

        var document = await OpenApiDocument.FromJsonAsync(spec);
        var settings = new RefitGeneratorSettings();
        var generator = new CSharpClientGeneratorFactory(settings, document).Create();
        var sut = new ReturnTypeGenerator(settings, generator);

        var operation = document.Paths["/test"]["get"];
        var result = sut.IsFileStreamResponse(operation);

        result.Should().BeTrue();
    }

    [Test]
    public async Task IsFileStreamResponse_Returns_False_For_Json_Content()
    {
        var spec = """
            {
              "openapi": "3.0.0",
              "info": { "title": "Test", "version": "1.0" },
              "paths": {
                "/test": {
                  "get": {
                    "operationId": "getTest",
                    "responses": {
                      "200": {
                        "description": "Success",
                        "content": {
                          "application/json": {
                            "schema": {
                              "type": "string"
                            }
                          }
                        }
                      }
                    }
                  }
                }
              }
            }
            """;

        var document = await OpenApiDocument.FromJsonAsync(spec);
        var settings = new RefitGeneratorSettings();
        var generator = new CSharpClientGeneratorFactory(settings, document).Create();
        var sut = new ReturnTypeGenerator(settings, generator);

        var operation = document.Paths["/test"]["get"];
        var result = sut.IsFileStreamResponse(operation);

        result.Should().BeFalse();
    }

    [Test]
    public async Task Generate_Returns_FileStream_Response_Type()
    {
        var spec = """
            {
              "openapi": "3.0.0",
              "info": { "title": "Test", "version": "1.0" },
              "paths": {
                "/test": {
                  "get": {
                    "operationId": "getFile",
                    "responses": {
                      "200": {
                        "description": "File",
                        "content": {
                          "application/pdf": {
                            "schema": {
                              "type": "string",
                              "format": "binary"
                            }
                          }
                        }
                      }
                    }
                  }
                }
              }
            }
            """;

        var document = await OpenApiDocument.FromJsonAsync(spec);
        var settings = new RefitGeneratorSettings();
        var generator = new CSharpClientGeneratorFactory(settings, document).Create();
        var sut = new ReturnTypeGenerator(settings, generator);

        var operation = document.Paths["/test"]["get"];
        var result = sut.Generate(operation);

        result.Should().Be("Task<HttpResponseMessage>");
    }

    [Test]
    public async Task IsApiResponseType_Detects_Task_Of_HttpResponseMessage()
    {
        var settings = new RefitGeneratorSettings();
        var generator = new CSharpClientGeneratorFactory(
            settings,
            await OpenApiDocument.FromJsonAsync("""
                { "openapi": "3.0.0", "info": { "title": "Test", "version": "1.0" }, "paths": {} }
                """))
            .Create();
        var sut = new ReturnTypeGenerator(settings, generator);

        sut.IsApiResponseType("Task<HttpResponseMessage>").Should().BeTrue();
        sut.IsApiResponseType("IObservable<HttpResponseMessage>").Should().BeTrue();
        sut.IsApiResponseType("Task<string>").Should().BeFalse();
        sut.IsApiResponseType("Task<IApiResponse>").Should().BeTrue();
        sut.IsApiResponseType("Task<IApiResponse<string>>").Should().BeTrue();
    }

    [Test]
    public async Task Generate_With_ResponseTypeOverride_Uses_Custom_Type()
    {
        var spec = """
            {
              "openapi": "3.0.0",
              "info": { "title": "Test", "version": "1.0" },
              "paths": {
                "/test": {
                  "get": {
                    "operationId": "customOp",
                    "responses": {
                      "200": {
                        "description": "Success",
                        "content": {
                          "application/json": {
                            "schema": { "type": "string" }
                          }
                        }
                      }
                    }
                  }
                }
              }
            }
            """;

        var document = await OpenApiDocument.FromJsonAsync(spec);
        var settings = new RefitGeneratorSettings();
        settings.ResponseTypeOverride["customOp"] = "MyCustomType";
        var generator = new CSharpClientGeneratorFactory(settings, document).Create();
        var sut = new ReturnTypeGenerator(settings, generator);

        var operation = document.Paths["/test"]["get"];
        var result = sut.Generate(operation);

        result.Should().Be("Task<MyCustomType>");
    }

    [Test]
    [Arguments("application/x-ndjson")]
    [Arguments("application/jsonl")]
    [Arguments("application/x-jsonlines")]
    [Arguments("text/event-stream")]
    public async Task Generate_Returns_IAsyncEnumerable_For_Streaming_Array_Response(
        string contentType)
    {
        var spec = $$"""
            {
              "openapi": "3.0.0",
              "info": { "title": "Test", "version": "1.0" },
              "paths": {
                "/test": {
                  "get": {
                    "operationId": "getTest",
                    "responses": {
                      "200": {
                        "description": "Success",
                        "content": {
                          "{{contentType}}": {
                            "schema": {
                              "type": "array",
                              "items": { "type": "string" }
                            }
                          }
                        }
                      }
                    }
                  }
                }
              }
            }
            """;

        var document = await OpenApiDocument.FromJsonAsync(spec);
        var settings = new RefitGeneratorSettings();
        var generator = new CSharpClientGeneratorFactory(settings, document).Create();
        var sut = new ReturnTypeGenerator(settings, generator);

        var operation = document.Paths["/test"]["get"];
        var result = sut.Generate(operation);

        result.Should().Be("IAsyncEnumerable<string>");
    }

    [Test]
    public async Task Generate_Returns_IAsyncEnumerable_For_Non_Array_Schema_Streaming_Response()
    {
        var spec = """
            {
              "openapi": "3.0.0",
              "info": { "title": "Test", "version": "1.0" },
              "paths": {
                "/test": {
                  "get": {
                    "operationId": "getTest",
                    "responses": {
                      "200": {
                        "description": "Success",
                        "content": {
                          "application/x-ndjson": {
                            "schema": {
                              "type": "object",
                              "properties": {
                                "id": { "type": "string" }
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
            """;

        var document = await OpenApiDocument.FromJsonAsync(spec);
        var settings = new RefitGeneratorSettings();
        var generator = new CSharpClientGeneratorFactory(settings, document).Create();
        var sut = new ReturnTypeGenerator(settings, generator);

        var operation = document.Paths["/test"]["get"];
        var result = sut.Generate(operation);

        result.Should().Be("IAsyncEnumerable<Anonymous>");
    }

    [Test]
    public async Task Generate_Returns_IAsyncEnumerable_For_Streaming_Response_When_ApiResponse_Enabled()
    {
        var spec = """
            {
              "openapi": "3.0.0",
              "info": { "title": "Test", "version": "1.0" },
              "paths": {
                "/test": {
                  "get": {
                    "operationId": "getTest",
                    "responses": {
                      "200": {
                        "description": "Success",
                        "content": {
                          "application/x-ndjson": {
                            "schema": {
                              "type": "array",
                              "items": { "type": "string" }
                            }
                          }
                        }
                      }
                    }
                  }
                }
              }
            }
            """;

        var document = await OpenApiDocument.FromJsonAsync(spec);
        var settings = new RefitGeneratorSettings
        {
            ReturnIApiResponse = true
        };
        var generator = new CSharpClientGeneratorFactory(settings, document).Create();
        var sut = new ReturnTypeGenerator(settings, generator);

        var operation = document.Paths["/test"]["get"];
        var result = sut.Generate(operation);

        result.Should().Be("IAsyncEnumerable<string>");
    }

    [Test]
    public async Task Generate_With_ResponseTypeOverride_Still_Overrides_Streaming()
    {
        var spec = """
            {
              "openapi": "3.0.0",
              "info": { "title": "Test", "version": "1.0" },
              "paths": {
                "/test": {
                  "get": {
                    "operationId": "customOp",
                    "responses": {
                      "200": {
                        "description": "Success",
                        "content": {
                          "application/x-ndjson": {
                            "schema": {
                              "type": "array",
                              "items": { "type": "string" }
                            }
                          }
                        }
                      }
                    }
                  }
                }
              }
            }
            """;

        var document = await OpenApiDocument.FromJsonAsync(spec);
        var settings = new RefitGeneratorSettings();
        settings.ResponseTypeOverride["customOp"] = "MyCustomType";
        var generator = new CSharpClientGeneratorFactory(settings, document).Create();
        var sut = new ReturnTypeGenerator(settings, generator);

        var operation = document.Paths["/test"]["get"];
        var result = sut.Generate(operation);

        result.Should().Be("Task<MyCustomType>");
    }

    [Test]
    public async Task Generate_Returns_IAsyncEnumerable_For_Streaming_Response_When_Observable_Enabled()
    {
        var spec = """
            {
              "openapi": "3.0.0",
              "info": { "title": "Test", "version": "1.0" },
              "paths": {
                "/test": {
                  "get": {
                    "operationId": "getTest",
                    "responses": {
                      "200": {
                        "description": "Success",
                        "content": {
                          "application/x-ndjson": {
                            "schema": {
                              "type": "array",
                              "items": { "type": "string" }
                            }
                          }
                        }
                      }
                    }
                  }
                }
              }
            }
            """;

        var document = await OpenApiDocument.FromJsonAsync(spec);
        var settings = new RefitGeneratorSettings
        {
            ReturnIObservable = true
        };
        var generator = new CSharpClientGeneratorFactory(settings, document).Create();
        var sut = new ReturnTypeGenerator(settings, generator);

        var operation = document.Paths["/test"]["get"];
        var result = sut.Generate(operation);

        result.Should().Be("IAsyncEnumerable<string>");
    }

    [Test]
    public async Task Generate_Returns_IAsyncEnumerable_For_Streaming_Response_With_Parameterized_Content_Type()
    {
        var spec = """
            {
              "openapi": "3.0.0",
              "info": { "title": "Test", "version": "1.0" },
              "paths": {
                "/test": {
                  "get": {
                    "operationId": "getTest",
                    "responses": {
                      "200": {
                        "description": "Success",
                        "content": {
                          "text/event-stream; charset=utf-8": {
                            "schema": {
                              "type": "array",
                              "items": { "type": "string" }
                            }
                          }
                        }
                      }
                    }
                  }
                }
              }
            }
            """;

        var document = await OpenApiDocument.FromJsonAsync(spec);
        var settings = new RefitGeneratorSettings();
        var generator = new CSharpClientGeneratorFactory(settings, document).Create();
        var sut = new ReturnTypeGenerator(settings, generator);

        var operation = document.Paths["/test"]["get"];
        var result = sut.Generate(operation);

        result.Should().Be("IAsyncEnumerable<string>");
    }

    [Test]
    public async Task Generate_Returns_IAsyncEnumerable_For_Default_Streaming_Response_With_SchemaLess_200()
    {
        var spec = """
            {
              "openapi": "3.0.0",
              "info": { "title": "Test", "version": "1.0" },
              "paths": {
                "/test": {
                  "get": {
                    "operationId": "getTest",
                    "responses": {
                      "200": {
                        "description": "Success",
                        "content": {
                          "application/json": {}
                        }
                      },
                      "default": {
                        "description": "Stream",
                        "content": {
                          "text/event-stream": {
                            "schema": {
                              "type": "array",
                              "items": { "type": "string" }
                            }
                          }
                        }
                      }
                    }
                  }
                }
              }
            }
            """;

        var document = await OpenApiDocument.FromJsonAsync(spec);
        var settings = new RefitGeneratorSettings();
        var generator = new CSharpClientGeneratorFactory(settings, document).Create();
        var sut = new ReturnTypeGenerator(settings, generator);

        var operation = document.Paths["/test"]["get"];
        var result = sut.Generate(operation);

        result.Should().Be("IAsyncEnumerable<string>");
    }

    [Test]
    public async Task Generate_Returns_IAsyncEnumerable_Of_Object_For_Streaming_Response_With_No_Schema()
    {
        var spec = """
            {
              "openapi": "3.0.0",
              "info": { "title": "Test", "version": "1.0" },
              "paths": {
                "/test": {
                  "get": {
                    "operationId": "getTest",
                    "responses": {
                      "200": {
                        "description": "Success",
                        "content": {
                          "text/event-stream": {}
                        }
                      }
                    }
                  }
                }
              }
            }
            """;

        var document = await OpenApiDocument.FromJsonAsync(spec);
        var settings = new RefitGeneratorSettings();
        var generator = new CSharpClientGeneratorFactory(settings, document).Create();
        var sut = new ReturnTypeGenerator(settings, generator);

        var operation = document.Paths["/test"]["get"];
        var result = sut.Generate(operation);

        result.Should().Be("IAsyncEnumerable<object>");
    }

    [Test]
    public async Task Generate_Returns_IAsyncEnumerable_Of_Object_For_Streaming_Response_With_Null_Media_Type()
    {
        var spec = """
            {
              "openapi": "3.0.0",
              "info": { "title": "Test", "version": "1.0" },
              "paths": {
                "/test": {
                  "get": {
                    "operationId": "getTest",
                    "responses": {
                      "200": {
                        "description": "Success",
                        "content": {
                          "text/event-stream": {
                            "schema": {
                              "type": "array",
                              "items": { "type": "string" }
                            }
                          }
                        }
                      }
                    }
                  }
                }
              }
            }
            """;

        var document = await OpenApiDocument.FromJsonAsync(spec);
        var settings = new RefitGeneratorSettings();
        var generator = new CSharpClientGeneratorFactory(settings, document).Create();
        document.Paths["/test"]["get"].Responses["200"].ActualResponse.Content["text/event-stream"] = null;
        var sut = new ReturnTypeGenerator(settings, generator);

        var operation = document.Paths["/test"]["get"];
        var result = sut.Generate(operation);

        result.Should().Be("IAsyncEnumerable<object>");
    }

    [Test]
    public async Task Generate_Does_Not_Return_IAsyncEnumerable_For_Non_Streaming_Swagger2_Produces()
    {
        var spec = """
            swagger: '2.0'
            info:
              title: Test
              version: 1.0.0
            paths:
              '/test':
                get:
                  operationId: getTest
                  produces:
                    - application/json
                  responses:
                    '200':
                      description: Success
                      schema:
                        type: array
                        items:
                          type: string
            """;

        var document = await OpenApiYamlDocument.FromYamlAsync(spec);
        var settings = new RefitGeneratorSettings();
        var generator = new CSharpClientGeneratorFactory(settings, document).Create();
        var sut = new ReturnTypeGenerator(settings, generator);

        var operation = document.Paths["/test"]["get"];
        var result = sut.Generate(operation);

        result.Should().Be("Task<ICollection<string>>");
    }

    [Test]
    public async Task Generate_With_ResponseTypeOverride_Void_Returns_Task()
    {
        var spec = """
            {
              "openapi": "3.0.0",
              "info": { "title": "Test", "version": "1.0" },
              "paths": {
                "/test": {
                  "get": {
                    "operationId": "customOp",
                    "responses": {
                      "200": {
                        "description": "Success",
                        "content": {
                          "application/json": {
                            "schema": { "type": "string" }
                          }
                        }
                      }
                    }
                  }
                }
              }
            }
            """;

        var document = await OpenApiDocument.FromJsonAsync(spec);
        var settings = new RefitGeneratorSettings();
        settings.ResponseTypeOverride["customOp"] = "void";
        var generator = new CSharpClientGeneratorFactory(settings, document).Create();
        var sut = new ReturnTypeGenerator(settings, generator);

        var operation = document.Paths["/test"]["get"];
        var result = sut.Generate(operation);

        result.Should().Be("Task");
    }
}
