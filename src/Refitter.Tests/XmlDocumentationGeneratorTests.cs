using System.Text;
using FluentAssertions;
using NSwag;
using NSwag.CodeGeneration.CSharp.Models;
using Refitter.Core;

namespace Refitter.Tests;

public class XmlDocumentationGeneratorTests
{
    private readonly XmlDocumentationGenerator _generator = new(new() { GenerateXmlDocCodeComments = true });

    private static CSharpOperationModel CreateOperationModel(OpenApiOperation operation)
    {
        var factory = new CSharpClientGeneratorFactory(new RefitGeneratorSettings(), new OpenApiDocument());
        var generator = factory.Create();
        return generator.CreateOperationModel(operation);
    }

    [Test]
    public void Can_Generate_Interface_Doc_Without_Linebreaks()
    {
        var docs = new StringBuilder();
        var interfaceDefinition = new OpenApiOperation { Summary = "Test", };
        this._generator.AppendInterfaceDocumentationByEndpoint(interfaceDefinition, docs);
        docs.ToString().Trim().Should().Be("/// <summary>Test</summary>");
    }

    [Test]
    public void Can_Generate_Interface_Doc_With_Linebreaks()
    {
        var docs = new StringBuilder();
        var interfaceDefinition = new OpenApiOperation { Summary = "Test\n", };
        this._generator.AppendInterfaceDocumentationByEndpoint(interfaceDefinition, docs);
        docs.ToString().Trim().Should().NotBe("/// <summary>Test</summary>");
        docs.ToString().Trim().Should().Contain("<summary>")
            .And.Contain("Test");
    }

    [Test]
    public void Can_Generate_Interface_Doc_From_Controller_Tag()
    {
        var docs = new StringBuilder();
        var controllerTag = new OpenApiTag { Name = "TestController", Description = "TestControllerDescription" };
        var document = new OpenApiDocument { Tags = [controllerTag] };

        this._generator.AppendInterfaceDocumentationByTag(document, "TestController", docs);

        docs.ToString().Trim().Should().Be("/// <summary>TestControllerDescription</summary>");
    }

    [Test]
    public void Can_Handle_Null_Document_Tags()
    {
        var docs = new StringBuilder();
        var document = new OpenApiDocument { Tags = null };

        this._generator.AppendInterfaceDocumentationByTag(document, "TestController", docs);

        docs.ToString().Trim().Should().BeEmpty();
    }

    [Test]
    public void Can_Escape_Xml_Special_Characters_In_Interface_Doc()
    {
        var docs = new StringBuilder();
        var controllerTag = new OpenApiTag { Name = "TestController", Description = "Test <tag> & content" };
        var document = new OpenApiDocument { Tags = [controllerTag] };

        this._generator.AppendInterfaceDocumentationByTag(document, "TestController", docs);

        docs.ToString().Trim().Should().Be("/// <summary>Test &lt;tag&gt; &amp; content</summary>");
    }

    [Test]
    public void Can_Escape_Xml_Special_Characters_In_Method_Summary()
    {
        var docs = new StringBuilder();
        var method = CreateOperationModel(new OpenApiOperation { Summary = "Test <tag> & content", });
        this._generator.AppendMethodDocumentation(method, false, false, false, false, docs);
        docs.ToString().Trim().Should().StartWith("/// <summary>Test &lt;tag&gt; &amp; content</summary>");
    }

    [Test]
    public void Can_Generate_Method_Summary()
    {
        var docs = new StringBuilder();
        var method = CreateOperationModel(new OpenApiOperation { Summary = "TestSummary", });
        this._generator.AppendMethodDocumentation(method, false, false, false, false, docs);
        docs.ToString().Trim().Should().StartWith("/// <summary>TestSummary</summary>");
    }

    [Test]
    public void Can_Generate_Method_Remarks()
    {
        var docs = new StringBuilder();
        var method = CreateOperationModel(new OpenApiOperation { Description = "TestDescription", });
        this._generator.AppendMethodDocumentation(method, false, false, false, false, docs);
        docs.ToString().Should().Contain("/// <remarks>TestDescription</remarks>");
    }

    [Test]
    public void Can_Generate_Method_Param()
    {
        var docs = new StringBuilder();
        var method = CreateOperationModel(new OpenApiOperation
        {
            Parameters = { new OpenApiParameter { OriginalName = "testParam", Description = "TestParameter" } },
        });
        this._generator.AppendMethodDocumentation(method, false, false, false, false, docs);
        docs.ToString().Should().Contain("/// <param name=\"testParam\">TestParameter</param>");
    }

    [Test]
    public void Can_Generate_ApizrRequestOptions_Param()
    {
        var docs = new StringBuilder();
        var method = CreateOperationModel(new OpenApiOperation
        {
            Parameters = { new OpenApiParameter { OriginalName = "testParam", Description = "TestParameter" } },
        });
        this._generator.AppendMethodDocumentation(method, false, false, true, false, docs);
        docs.ToString().Should().Contain("/// <param name=\"options\">The <see cref=\"IApizrRequestOptions\"/> instance to pass through the request.</param>");
    }

    [Test]
    public void Can_Generate_DynamicQuerystring_Param()
    {
        var docs = new StringBuilder();
        var method = CreateOperationModel(new OpenApiOperation
        {
            Parameters = { new OpenApiParameter { OriginalName = "testParam", Description = "TestParameter" } },
        });
        this._generator.AppendMethodDocumentation(method, false, true, false, false, docs);
        docs.ToString().Should().Contain("/// <param name=\"queryParams\">The dynamic querystring parameter wrapping all others.</param>");
    }

    [Test]
    public void Can_Generate_CancellationToken_Param()
    {
        var docs = new StringBuilder();
        var method = CreateOperationModel(new OpenApiOperation
        {
            Parameters = { new OpenApiParameter { OriginalName = "testParam", Description = "TestParameter" } },
        });
        this._generator.AppendMethodDocumentation(method, false, false, false, true, docs);
        docs.ToString().Should().Contain("/// <param name=\"cancellationToken\">The cancellation token to cancel the request.</param>");
    }

    [Test]
    public void Can_Generate_Method_Returns()
    {
        var docs = new StringBuilder();
        var method = CreateOperationModel(new OpenApiOperation
        {
            Responses =
            {
                ["200"] = new OpenApiResponse
                {
                    Description = "TestResponse",
                    Content = { [""] = new OpenApiMediaType() },
                },
            },
            Produces = ["application/json"],
        });
        this._generator.AppendMethodDocumentation(method, false, false, false, false, docs);
        docs.ToString().Should().Contain("/// <returns>TestResponse</returns>");
    }

    [Test]
    public void Can_Generate_Method_Returns_With_Empty_Result()
    {
        var docs = new StringBuilder();
        var method = CreateOperationModel(new OpenApiOperation
        {
            Responses =
            {
                ["200"] = new OpenApiResponse { Content = { [""] = new OpenApiMediaType() } },
            },
            Produces = ["application/json"],
        });
        this._generator.AppendMethodDocumentation(method, false, false, false, false, docs);
        docs.ToString().Should().Contain("/// <returns>")
            .And.Contain("Task");
    }

    [Test]
    public void Can_Generate_Method_Returns_Without_Result()
    {
        var docs = new StringBuilder();
        var method = CreateOperationModel(new OpenApiOperation());
        this._generator.AppendMethodDocumentation(method, false, false, false, false, docs);
        docs.ToString().Should().Contain("/// <returns>")
            .And.Contain("Task");
    }

    [Test]
    public void Can_Generate_Method_Throws()
    {
        var docs = new StringBuilder();
        var method = CreateOperationModel(new OpenApiOperation());
        this._generator.AppendMethodDocumentation(method, false, false, false, false, docs);
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
        var method = CreateOperationModel(new OpenApiOperation
        {
            Responses = { ["400"] = new OpenApiResponse { Description = "TestResponse" } },
        });
        generator.AppendMethodDocumentation(method, false, false, false, false, docs);
        docs.ToString().Should().Contain("/// <exception cref=\"ApiException\">")
            .And.Contain("<term>400</term>");
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
        var method = CreateOperationModel(new OpenApiOperation
        {
            Responses = { ["400"] = new OpenApiResponse { Description = "TestResponse" } },
        });
        generator.AppendMethodDocumentation(method, false, false, false, false, docs);
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
        var method = CreateOperationModel(new OpenApiOperation
        {
            Responses = { ["400"] = new OpenApiResponse { Description = "TestResponse" } },
        });
        generator.AppendMethodDocumentation(method, true, false, false, false, docs);
        docs.ToString().Should().NotContain("/// <exception cref=\"ApiException\">")
            .And.Contain("/// <returns>")
            .And.Contain("<term>400</term>");
    }
}
