using System.Text;
using FluentAssertions;
using NSwag;
using NSwag.CodeGeneration.CSharp.Models;
using Refitter.Core;
using Xunit;

namespace Refitter.Tests
{
    /// <summary>
    /// Unit tests for the <see cref="XmlDocumentationGenerator"/> class.
    /// </summary>
    public class XmlDocumentationGeneratorTests
    {
        /// <summary>
        /// The generator to use for testing.
        /// </summary>
        private XmlDocumentationGenerator _generator =
            new(new RefitGeneratorSettings { GenerateXmlDocCodeComments = true });

        private static CSharpOperationModel CreateOperationModel(OpenApiOperation operation)
        {
            var factory = new CSharpClientGeneratorFactory(new RefitGeneratorSettings(), new OpenApiDocument());
            var generator = factory.Create();
            return generator.CreateOperationModel(operation);
        }

        [Fact]
        public void Can_Generate_Interface_Doc_Without_Linebreaks()
        {
            var docs = new StringBuilder();
            var interfaceDefinition = new OpenApiOperation { Summary = "Test", };
            this._generator.AppendInterfaceDocumentation(interfaceDefinition, docs);
            docs.ToString().Trim().Should().Be("/// <summary>Test</summary>");
        }

        [Fact]
        public void Can_Generate_Interface_Doc_With_Linebreaks()
        {
            var docs = new StringBuilder();
            var interfaceDefinition = new OpenApiOperation { Summary = "Test\n", };
            this._generator.AppendInterfaceDocumentation(interfaceDefinition, docs);
            docs.ToString().Trim().Should().NotBe("/// <summary>Test</summary>");
            docs.ToString().Trim().Should().Contain("<summary>")
                .And.Contain("Test");
        }

        [Fact]
        public void Can_Generate_Method_Summary()
        {
            var docs = new StringBuilder();
            var method = CreateOperationModel(new OpenApiOperation { Summary = "TestSummary", });
            this._generator.AppendMethodDocumentation(method, false, false, false, false, docs);
            docs.ToString().Trim().Should().StartWith("/// <summary>TestSummary</summary>");
        }

        [Fact]
        public void Can_Generate_Method_Remarks()
        {
            var docs = new StringBuilder();
            var method = CreateOperationModel(new OpenApiOperation { Description = "TestDescription", });
            this._generator.AppendMethodDocumentation(method, false, false, false, false, docs);
            docs.ToString().Should().Contain("/// <remarks>TestDescription</remarks>");
        }

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
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

        [Fact]
        public void Can_Generate_Method_Returns_Without_Result()
        {
            var docs = new StringBuilder();
            var method = CreateOperationModel(new OpenApiOperation());
            this._generator.AppendMethodDocumentation(method, false, false, false, false, docs);
            docs.ToString().Should().Contain("/// <returns>")
                .And.Contain("Task");
        }

        [Fact]
        public void Can_Generate_Method_Throws()
        {
            var docs = new StringBuilder();
            var method = CreateOperationModel(new OpenApiOperation());
            this._generator.AppendMethodDocumentation(method, false, false, false, false, docs);
            docs.ToString().Should().Contain("/// <exception cref=\"ApiException\">");
        }

        [Fact]
        public void Can_Generate_Method_Throws_With_Response_Code()
        {
            this._generator = new XmlDocumentationGenerator(new RefitGeneratorSettings
            {
                GenerateXmlDocCodeComments = true,
                GenerateStatusCodeComments = true,
            });
            var docs = new StringBuilder();
            var method = CreateOperationModel(new OpenApiOperation
            {
                Responses = { ["400"] = new OpenApiResponse { Description = "TestResponse" } },
            });
            this._generator.AppendMethodDocumentation(method, false, false, false, false, docs);
            docs.ToString().Should().Contain("/// <exception cref=\"ApiException\">")
                .And.Contain("<term>400</term>");
        }

        [Fact]
        public void Can_Generate_Method_Throws_Without_Response_Code()
        {
            this._generator = new XmlDocumentationGenerator(new RefitGeneratorSettings
            {
                GenerateXmlDocCodeComments = true,
                GenerateStatusCodeComments = false,
            });
            var docs = new StringBuilder();
            var method = CreateOperationModel(new OpenApiOperation
            {
                Responses = { ["400"] = new OpenApiResponse { Description = "TestResponse" } },
            });
            this._generator.AppendMethodDocumentation(method, false, false, false, false, docs);
            docs.ToString().Should().Contain("/// <exception cref=\"ApiException\">")
                .And.NotContain("<term>400</term>");
        }

        [Fact]
        public void Can_Generate_Method_With_IApiResponse()
        {
            this._generator = new XmlDocumentationGenerator(new RefitGeneratorSettings
            {
                GenerateXmlDocCodeComments = true,
                ReturnIApiResponse = true,
            });
            var docs = new StringBuilder();
            var method = CreateOperationModel(new OpenApiOperation
            {
                Responses = { ["400"] = new OpenApiResponse { Description = "TestResponse" } },
            });
            this._generator.AppendMethodDocumentation(method, true, false, false, false, docs);
            docs.ToString().Should().NotContain("/// <exception cref=\"ApiException\">")
                .And.Contain("/// <returns>")
                .And.Contain("<term>400</term>");
        }
    }
}
