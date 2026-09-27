using System.Text;
using AwesomeAssertions;
using Refitter.Core;

namespace Refitter.Tests;


public class XmlDocumentationGeneratorTests
{
    private readonly XmlDocumentationGenerator generator = new(new RefitGeneratorSettings { GenerateXmlDocCodeComments = true });

    private static OperationModel CreateOperationModel(ApiOperation operation)
    {
        var factory = new ContractGeneratorFactory(new RefitGeneratorSettings(), new ApiDocument());
        var generator = factory.Create();
        return generator.CreateOperationModel(operation);
    }

    [Test]
    public void Can_Generate_Interface_Doc_Without_Linebreaks()
    {
        var docs = new StringBuilder();
        var interfaceDefinition = new ApiOperation { Summary = "Test", };
        this.generator.AppendInterfaceDocumentationByEndpoint(interfaceDefinition, docs);
        docs.ToString().Trim().Should().Be("/// <summary>Test</summary>");
    }

    [Test]
    public void Can_Generate_Interface_Doc_With_Linebreaks()
    {
        var docs = new StringBuilder();
        var interfaceDefinition = new ApiOperation { Summary = "Test\n", };
        this.generator.AppendInterfaceDocumentationByEndpoint(interfaceDefinition, docs);
        docs.ToString().Trim().Should().NotBe("/// <summary>Test</summary>");
        docs.ToString().Trim().Should().Contain("<summary>")
            .And.Contain("Test");
    }

    [Test]
    public void Can_Generate_Interface_Doc_From_Controller_Tag()
    {
        var docs = new StringBuilder();
        var controllerTag = new ApiTag { Name = "TestController", Description = "TestControllerDescription" };
        var document = new ApiDocument { Tags = [controllerTag] };

        this.generator.AppendInterfaceDocumentationByTag(document, "TestController", docs);

        docs.ToString().Trim().Should().Be("/// <summary>TestControllerDescription</summary>");
    }

    [Test]
    public void Can_Handle_Null_Document_Tags()
    {
        var docs = new StringBuilder();
        var document = new ApiDocument { Tags = null! };

        this.generator.AppendInterfaceDocumentationByTag(document, "TestController", docs);

        docs.ToString().Trim().Should().Be("/// <summary>Operations for TestController.</summary>");
    }

    [Test]
    public void Can_Escape_Xml_Special_Characters_In_Interface_Doc()
    {
        var docs = new StringBuilder();
        var controllerTag = new ApiTag { Name = "TestController", Description = "Test <tag> & content" };
        var document = new ApiDocument { Tags = [controllerTag] };

        this.generator.AppendInterfaceDocumentationByTag(document, "TestController", docs);

        docs.ToString().Trim().Should().Be("/// <summary>Test &lt;tag&gt; &amp; content</summary>");
    }

    [Test]
    public void Can_Escape_Xml_Special_Characters_In_Method_Summary()
    {
        var docs = new StringBuilder();
        var method = CreateOperationModel(new ApiOperation { Summary = "Test <tag> & content", });
        this.generator.AppendMethodDocumentation(method, [], false, docs);
        docs.ToString().Trim().Should().StartWith("/// <summary>Test &lt;tag&gt; &amp; content</summary>");
    }

    [Test]
    public void Can_Generate_Method_Summary()
    {
        var docs = new StringBuilder();
        var method = CreateOperationModel(new ApiOperation { Summary = "TestSummary", });
        this.generator.AppendMethodDocumentation(method, [], false, docs);
        docs.ToString().Trim().Should().StartWith("/// <summary>TestSummary</summary>");
    }

    [Test]
    public void Can_Generate_Method_Remarks()
    {
        var docs = new StringBuilder();
        var method = CreateOperationModel(new ApiOperation { Description = "TestDescription", });
        this.generator.AppendMethodDocumentation(method, [], false, docs);
        docs.ToString().Should().Contain("/// <remarks>TestDescription</remarks>");
    }

    [Test]
    public void Can_Generate_Method_Param()
    {
        var docs = new StringBuilder();
        var method = CreateOperationModel(new ApiOperation
        {
            Parameters = { new ApiParameter { OriginalName = "testParam", Description = "TestParameter" } },
        });
        this.generator.AppendMethodDocumentation(method, ["string testParam"], false, docs);
        docs.ToString().Should().Contain("/// <param name=\"testParam\">TestParameter</param>");
    }

    [Test]
    public void Documents_Keyword_Parameters_Without_Escape_Prefix()
    {
        var docs = new StringBuilder();
        var method = CreateOperationModel(new ApiOperation
        {
            Parameters = { new ApiParameter { Name = "class", Kind = ApiParameterKind.Query, Description = "The class" } },
        });
        this.generator.AppendMethodDocumentation(method, ["[Query] string @class"], false, docs);
        docs.ToString().Should().Contain("/// <param name=\"class\">The class</param>");
        docs.ToString().Should().NotContain("@class");
    }

    [Test]
    public void Documents_Only_Emitted_Parameters()
    {
        var docs = new StringBuilder();
        var method = CreateOperationModel(new ApiOperation
        {
            Parameters =
            {
                new ApiParameter { Name = "kept", Kind = ApiParameterKind.Query, Description = "Kept" },
                new ApiParameter { Name = "session", Kind = ApiParameterKind.Cookie, Description = "Dropped" },
            },
        });
        this.generator.AppendMethodDocumentation(method, ["[Query] string kept", "[AliasAs(\"extra\")] string extra"], false, docs);
        docs.ToString().Should().Contain("/// <param name=\"kept\">Kept</param>");
        docs.ToString().Should().Contain("/// <param name=\"extra\">extra parameter</param>");
        docs.ToString().Should().NotContain("session");
    }

    [Test]
    public void Can_Generate_ApizrRequestOptions_Param()
    {
        var docs = new StringBuilder();
        var method = CreateOperationModel(new ApiOperation
        {
            Parameters = { new ApiParameter { OriginalName = "testParam", Description = "TestParameter" } },
        });
        this.generator.AppendMethodDocumentation(method, ["string testParam", "[RequestOptions] IApizrRequestOptions options"], false, docs);
        docs.ToString().Should().Contain("/// <param name=\"options\">The <see cref=\"IApizrRequestOptions\"/> instance to pass through the request.</param>");
    }

    [Test]
    public void Can_Generate_DynamicQuerystring_Param()
    {
        var docs = new StringBuilder();
        var method = CreateOperationModel(new ApiOperation
        {
            Parameters = { new ApiParameter { OriginalName = "testParam", Description = "TestParameter" } },
        });
        this.generator.AppendMethodDocumentation(method, ["[Query] TestQueryParams queryParams"], false, docs, "TestQueryParams");
        docs.ToString().Should().Contain("/// <param name=\"queryParams\">The dynamic querystring parameter wrapping all others.</param>");
    }

    [Test]
    public void Documents_Dynamic_Querystring_Wrapper_Even_When_A_Folded_Parameter_Is_Named_QueryParams()
    {
        var docs = new StringBuilder();
        var method = CreateOperationModel(new ApiOperation
        {
            Parameters = { new ApiParameter { Name = "queryParams", Kind = ApiParameterKind.Query, Description = "Folded" } },
        });
        this.generator.AppendMethodDocumentation(method, ["[Query] TestQueryParams? queryParams"], false, docs, "TestQueryParams");
        docs.ToString().Should().Contain("/// <param name=\"queryParams\">The dynamic querystring parameter wrapping all others.</param>");
    }

    [Test]
    public void Documents_Parameter_Named_QueryParams_Without_Dynamic_Querystring()
    {
        var docs = new StringBuilder();
        var method = CreateOperationModel(new ApiOperation
        {
            Parameters = { new ApiParameter { Name = "queryParams", Kind = ApiParameterKind.Query, Description = "Real parameter" } },
        });
        this.generator.AppendMethodDocumentation(method, ["[Query] string queryParams"], false, docs);
        docs.ToString().Should().Contain("/// <param name=\"queryParams\">Real parameter</param>");
    }

    [Test]
    public void Documents_Renamed_Parameter_With_Description_Of_Its_Alias()
    {
        var docs = new StringBuilder();
        var method = CreateOperationModel(new ApiOperation
        {
            Parameters = { new ApiParameter { Name = "cancellationToken", Kind = ApiParameterKind.Query, Description = "Query token" } },
        });
        this.generator.AppendMethodDocumentation(
            method,
            ["[AliasAs(\"cancellationToken\")] [Query] string cancellationToken2", "CancellationToken cancellationToken = default"],
            false,
            docs);
        docs.ToString().Should().Contain("/// <param name=\"cancellationToken2\">Query token</param>");
        docs.ToString().Should().Contain("/// <param name=\"cancellationToken\">The cancellation token to cancel the request.</param>");
    }

    [Test]
    public void Can_Generate_CancellationToken_Param()
    {
        var docs = new StringBuilder();
        var method = CreateOperationModel(new ApiOperation
        {
            Parameters = { new ApiParameter { OriginalName = "testParam", Description = "TestParameter" } },
        });
        this.generator.AppendMethodDocumentation(method, ["string testParam", "CancellationToken cancellationToken = default"], false, docs);
        docs.ToString().Should().Contain("/// <param name=\"cancellationToken\">The cancellation token to cancel the request.</param>");
    }

    [Test]
    public void Can_Generate_Method_Returns()
    {
        var docs = new StringBuilder();
        var method = CreateOperationModel(new ApiOperation
        {
            Responses =
            {
                ["200"] = new ApiResponse
                {
                    Description = "TestResponse",
                    Content = { [""] = new ApiMediaType() },
                },
            },
            Produces = ["application/json"],
        });
        this.generator.AppendMethodDocumentation(method, [], false, docs);
        docs.ToString().Should().Contain("/// <returns>TestResponse</returns>");
    }

    [Test]
    public void Can_Generate_Method_Returns_With_Empty_Result()
    {
        var docs = new StringBuilder();
        var method = CreateOperationModel(new ApiOperation
        {
            Responses =
            {
                ["200"] = new ApiResponse { Content = { [""] = new ApiMediaType() } },
            },
            Produces = ["application/json"],
        });
        this.generator.AppendMethodDocumentation(method, [], false, docs);
        docs.ToString().Should().Contain("/// <returns>")
            .And.Contain("Task");
    }

    [Test]
    public void Can_Generate_Method_Returns_Without_Result()
    {
        var docs = new StringBuilder();
        var method = CreateOperationModel(new ApiOperation());
        this.generator.AppendMethodDocumentation(method, [], false, docs);
        docs.ToString().Should().Contain("/// <returns>")
            .And.Contain("Task");
    }

    [Test]
    public void Can_Generate_Method_Throws()
    {
        var docs = new StringBuilder();
        var method = CreateOperationModel(new ApiOperation());
        this.generator.AppendMethodDocumentation(method, [], false, docs);
        docs.ToString().Should().Contain("/// <exception cref=\"ApiException\">");
    }

    [Test]
    public void Can_Generate_Method_Throws_With_Response_Code()
    {
        var generator = new XmlDocumentationGenerator(new RefitGeneratorSettings
        {
            GenerateXmlDocCodeComments = true,
            GenerateStatusCodeComments = true,
        });
        var docs = new StringBuilder();
        var method = CreateOperationModel(new ApiOperation
        {
            Responses = { ["400"] = new ApiResponse { Description = "TestResponse" } },
        });
        generator.AppendMethodDocumentation(method, [], false, docs);
        docs.ToString().Should().Contain("/// <exception cref=\"ApiException\">")
            .And.Contain("<term>400</term>");
    }

    [Test]
    public void Can_Generate_Method_Throws_With_Readable_Unicode_Status_Code_Comments()
    {
        var generator = new XmlDocumentationGenerator(new RefitGeneratorSettings
        {
            GenerateXmlDocCodeComments = true,
            GenerateStatusCodeComments = true,
        });
        var docs = new StringBuilder();
        var method = CreateOperationModel(new ApiOperation
        {
            Responses = { ["400"] = new ApiResponse { Description = "Ошибка запроса" } },
        });

        generator.AppendMethodDocumentation(method, [], false, docs);

        docs.ToString().Should().Contain("/// <exception cref=\"ApiException\">")
            .And.Contain("<term>400</term>")
            .And.Contain("<description>Ошибка запроса</description>")
            .And.NotContain(@"\u041e\u0448");
    }

    [Test]
    public void Can_Generate_Method_Throws_Without_Response_Code()
    {
        var generator = new XmlDocumentationGenerator(new RefitGeneratorSettings
        {
            GenerateXmlDocCodeComments = true,
            GenerateStatusCodeComments = false,
        });
        var docs = new StringBuilder();
        var method = CreateOperationModel(new ApiOperation
        {
            Responses = { ["400"] = new ApiResponse { Description = "TestResponse" } },
        });
        generator.AppendMethodDocumentation(method, [], false, docs);
        docs.ToString().Should().Contain("/// <exception cref=\"ApiException\">")
            .And.NotContain("<term>400</term>");
    }

    [Test]
    public void Can_Generate_Method_With_IApiResponse()
    {
        var generator = new XmlDocumentationGenerator(new RefitGeneratorSettings
        {
            GenerateXmlDocCodeComments = true,
            ReturnIApiResponse = true,
        });
        var docs = new StringBuilder();
        var method = CreateOperationModel(new ApiOperation
        {
            Responses = { ["400"] = new ApiResponse { Description = "TestResponse" } },
        });
        generator.AppendMethodDocumentation(method, [], true, docs);
        docs.ToString().Should().NotContain("/// <exception cref=\"ApiException\">")
            .And.Contain("/// <returns>")
            .And.Contain("<term>400</term>");
    }

    [Test]
    public void Can_Generate_Method_Returns_With_Readable_Unicode_Description()
    {
        var generator = new XmlDocumentationGenerator(new RefitGeneratorSettings
        {
            GenerateXmlDocCodeComments = true,
        });
        var docs = new StringBuilder();

        // Create an operation with a success response that has a description and a schema (so it has a result)
        var operation = new ApiOperation
        {
            Responses =
            {
                ["200"] = new ApiResponse
                {
                    Description = "Ошибка ответа",
                    Schema = new ApiSchema { Type = ApiObjectType.String }
                }
            },
            Produces = ["application/json"]
        };

        var method = CreateOperationModel(operation);

        // Verify that ResultDescription picked up the description (NSwag might escape it, which is what we want to test)
        // If NSwag escapes it, it will look like "\u..."
        // Our generator should decode it back.

        generator.AppendMethodDocumentation(method, [], false, docs);

        docs.ToString().Should().Contain("/// <returns>")
            .And.Contain("Ошибка ответа")
            .And.NotContain(@"\u041e\u0448");
    }

    [Test]
    public void XmlDocumentationGenerator_AppendInterfaceDocumentationByTag_Returns_Early_When_Disabled()
    {
        var generator = new XmlDocumentationGenerator(new RefitGeneratorSettings
        {
            GenerateXmlDocCodeComments = false
        });

        var code = new StringBuilder();
        var tag = new ApiTag { Name = "TestTag", Description = "Test Description" };
        var document = new ApiDocument { Tags = [tag] };

        generator.AppendInterfaceDocumentationByTag(document, "TestTag", code);

        code.ToString().Should().BeEmpty();
    }

    [Test]
    public void XmlDocumentationGenerator_AppendInterfaceDocumentationByEndpoint_Returns_Early_When_Disabled()
    {
        var generator = new XmlDocumentationGenerator(new RefitGeneratorSettings
        {
            GenerateXmlDocCodeComments = false
        });

        var code = new StringBuilder();
        var endpoint = new ApiOperation { Summary = "Test Summary" };

        generator.AppendInterfaceDocumentationByEndpoint(endpoint, code);

        code.ToString().Should().BeEmpty();
    }

    [Test]
    public void XmlDocumentationGenerator_AppendMethodDocumentation_Returns_Early_When_Disabled()
    {
        var generator = new XmlDocumentationGenerator(new RefitGeneratorSettings
        {
            GenerateXmlDocCodeComments = false
        });

        var code = new StringBuilder();
        var factory = new ContractGeneratorFactory(new RefitGeneratorSettings(), new ApiDocument());
        var csharpGenerator = factory.Create();
        var operation = new ApiOperation { Summary = "Test Summary", Description = "Test Description" };
        var method = csharpGenerator.CreateOperationModel(operation);

        generator.AppendMethodDocumentation(method, [], false, code);

        code.ToString().Should().BeEmpty();
    }
}
