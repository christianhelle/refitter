using FluentAssertions;
using Microsoft.OpenApi.Models;
using Refitter.Validation;
using TUnit.Core;

namespace Refitter.Tests;

public class OpenApiStatsTests
{
    [Test]
    public void Should_Initialize_With_Zero_Counts()
    {
        var stats = new OpenApiStats();

        stats.ParameterCount.Should().Be(0);
        stats.SchemaCount.Should().Be(0);
        stats.HeaderCount.Should().Be(0);
        stats.PathItemCount.Should().Be(0);
        stats.RequestBodyCount.Should().Be(0);
        stats.ResponseCount.Should().Be(0);
        stats.OperationCount.Should().Be(0);
        stats.LinkCount.Should().Be(0);
        stats.CallbackCount.Should().Be(0);
    }

    [Test]
    public void Visit_Parameter_Should_Increment_ParameterCount()
    {
        var stats = new OpenApiStats();
        var parameter = new OpenApiParameter();

        stats.Visit(parameter);

        stats.ParameterCount.Should().Be(1);
    }

    [Test]
    public void Visit_Schema_Should_Increment_SchemaCount()
    {
        var stats = new OpenApiStats();
        var schema = new OpenApiSchema();

        stats.Visit(schema);

        stats.SchemaCount.Should().Be(1);
    }

    [Test]
    public void Visit_Headers_Should_Increment_HeaderCount()
    {
        var stats = new OpenApiStats();
        var headers = new Dictionary<string, OpenApiHeader>();

        stats.Visit(headers);

        stats.HeaderCount.Should().Be(1);
    }

    [Test]
    public void Visit_PathItem_Should_Increment_PathItemCount()
    {
        var stats = new OpenApiStats();
        var pathItem = new OpenApiPathItem();

        stats.Visit(pathItem);

        stats.PathItemCount.Should().Be(1);
    }

    [Test]
    public void Visit_RequestBody_Should_Increment_RequestBodyCount()
    {
        var stats = new OpenApiStats();
        var requestBody = new OpenApiRequestBody();

        stats.Visit(requestBody);

        stats.RequestBodyCount.Should().Be(1);
    }

    [Test]
    public void Visit_Response_Should_Increment_ResponseCount()
    {
        var stats = new OpenApiStats();
        var response = new OpenApiResponses();

        stats.Visit(response);

        stats.ResponseCount.Should().Be(1);
    }

    [Test]
    public void Visit_Operation_Should_Increment_OperationCount()
    {
        var stats = new OpenApiStats();
        var operation = new OpenApiOperation();

        stats.Visit(operation);

        stats.OperationCount.Should().Be(1);
    }

    [Test]
    public void Visit_Link_Should_Increment_LinkCount()
    {
        var stats = new OpenApiStats();
        var link = new OpenApiLink();

        stats.Visit(link);

        stats.LinkCount.Should().Be(1);
    }

    [Test]
    public void Visit_Callback_Should_Increment_CallbackCount()
    {
        var stats = new OpenApiStats();
        var callback = new OpenApiCallback();

        stats.Visit(callback);

        stats.CallbackCount.Should().Be(1);
    }

    [Test]
    public void ToString_Should_Return_Formatted_Statistics()
    {
        var stats = new OpenApiStats();
        stats.Visit(new OpenApiPathItem());
        stats.Visit(new OpenApiOperation());
        stats.Visit(new OpenApiParameter());
        stats.Visit(new OpenApiSchema());

        var output = stats.ToString();

        output.Should().Contain("Path Items: 1");
        output.Should().Contain("Operations: 1");
        output.Should().Contain("Parameters: 1");
        output.Should().Contain("Schemas: 1");
    }

    [Test]
    public void Multiple_Visits_Should_Accumulate_Counts()
    {
        var stats = new OpenApiStats();

        stats.Visit(new OpenApiParameter());
        stats.Visit(new OpenApiParameter());
        stats.Visit(new OpenApiParameter());

        stats.ParameterCount.Should().Be(3);
    }
}
