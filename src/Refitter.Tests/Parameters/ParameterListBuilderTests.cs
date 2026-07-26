using FluentAssertions;
using NSwag;
using NSwag.CodeGeneration.CSharp.Models;
using Refitter.Core;
using TUnit.Core;

namespace Refitter.Tests.Parameters;

public class ParameterListBuilderTests
{
    private const string TwoQueryParameterSpec = """
        openapi: 3.0.0
        info:
          title: Test
          version: "1.0"
        paths:
          /test:
            get:
              operationId: getTest
              parameters:
                - name: page
                  in: query
                  schema:
                    type: integer
                - name: limit
                  in: query
                  schema:
                    type: integer
              responses:
                '200':
                  description: Success
        """;

    private const string NoParameterSpec = """
        openapi: 3.0.0
        info:
          title: Test
          version: "1.0"
        paths:
          /test:
            get:
              operationId: getTest
              responses:
                '200':
                  description: Success
        """;

    private const string RequiredAndOptionalQuerySpec = """
        openapi: 3.0.0
        info:
          title: Test
          version: "1.0"
        paths:
          /test:
            get:
              operationId: getTest
              parameters:
                - name: page
                  in: query
                  required: true
                  schema:
                    type: integer
                - name: limit
                  in: query
                  schema:
                    type: integer
              responses:
                '200':
                  description: Success
        """;

    [Test]
    public async Task Build_With_DynamicQuerystring_Returns_Wrapper_Parameter_And_Code()
    {
        var (operationModel, operation, settings) = await SetupAsync(
            TwoQueryParameterSpec,
            new RefitGeneratorSettings { UseDynamicQuerystringParameters = true });

        var result = new ParameterListBuilder(settings)
            .Build(operationModel, operation, "TestQueryParams");

        result.DynamicQuerystringCode.Should().NotBeNullOrWhiteSpace();
        result.Parameters.Should().ContainSingle()
            .Which.Should().Contain("[Query] TestQueryParams").And.Contain("queryParams");
    }

    [Test]
    public async Task Build_Without_DynamicQuerystring_Returns_Null_Code_And_Individual_Parameters()
    {
        var (operationModel, operation, settings) = await SetupAsync(
            TwoQueryParameterSpec,
            new RefitGeneratorSettings());

        var result = new ParameterListBuilder(settings)
            .Build(operationModel, operation, "TestQueryParams");

        result.DynamicQuerystringCode.Should().BeNull();
        result.Parameters.Should().HaveCount(2);
    }

    [Test]
    public async Task Build_Appends_CancellationToken_When_Enabled()
    {
        var (operationModel, operation, settings) = await SetupAsync(
            NoParameterSpec,
            new RefitGeneratorSettings { UseCancellationTokens = true });

        var result = new ParameterListBuilder(settings)
            .Build(operationModel, operation, string.Empty);

        result.Parameters.Should().ContainSingle()
            .Which.Should().Be("CancellationToken cancellationToken = default");
    }

    [Test]
    public async Task Build_Appends_ApizrRequestOptions_When_Enabled()
    {
        var (operationModel, operation, settings) = await SetupAsync(
            NoParameterSpec,
            new RefitGeneratorSettings { ApizrSettings = new ApizrSettings { WithRequestOptions = true } });

        var result = new ParameterListBuilder(settings)
            .Build(operationModel, operation, string.Empty);

        result.Parameters.Should().ContainSingle()
            .Which.Should().Contain("IApizrRequestOptions options");
    }

    [Test]
    public async Task Build_Orders_Optional_Parameters_After_Required()
    {
        var (operationModel, operation, settings) = await SetupAsync(
            RequiredAndOptionalQuerySpec,
            new RefitGeneratorSettings { OptionalParameters = true });

        var result = new ParameterListBuilder(settings)
            .Build(operationModel, operation, "TestQueryParams");

        result.Parameters.Last().Should().Contain("limit").And.Contain("= default");
    }

    private static async Task<(CSharpOperationModel OperationModel, OpenApiOperation Operation, RefitGeneratorSettings Settings)> SetupAsync(
        string spec,
        RefitGeneratorSettings settings)
    {
        var document = await OpenApiYamlDocument.FromYamlAsync(spec);
        var generator = new CSharpClientGeneratorFactory(settings, document).Create();
        var operation = document.Paths["/test"]["get"];
        var operationModel = generator.CreateOperationModel(operation);
        return (operationModel, operation, settings);
    }
}
