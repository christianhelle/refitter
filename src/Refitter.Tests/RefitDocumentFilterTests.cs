using FluentAssertions;
using NSwag;
using Refitter.Core;
using TUnit.Core;

namespace Refitter.Tests;

public class RefitDocumentFilterTests
{
    private const string OpenApiSpec = @"
openapi: '3.0.0'
info:
  title: Test API
  version: '1.0'
paths:
  /foo/{id}:
    get:
      tags: ['Foo']
      operationId: 'GetFooById'
      responses:
        '200':
          description: success
  /foo:
    get:
      tags: ['Foo']
      operationId: 'GetAllFoos'
      responses:
        '200':
          description: success
  /bar:
    get:
      tags: ['Bar']
      operationId: 'GetAllBars'
      responses:
        '200':
          description: success
  /bar/{id}:
    get:
      tags: ['Bar']
      operationId: 'GetBarById'
      responses:
        '200':
          description: success
  /baz:
    get:
      tags: ['Baz']
      operationId: 'GetAllBazs'
      responses:
        '200':
          description: success
";

    private const string SwaggerSpec = @"
swagger: '2.0'
info:
  title: Test API
  version: '1.0'
host: api.example.com
basePath: /v1
paths:
  /foo/{id}:
    get:
      tags: ['Foo']
      operationId: 'GetFooById'
      responses:
        '200':
          description: success
  /foo:
    get:
      tags: ['Foo']
      operationId: 'GetAllFoos'
      responses:
        '200':
          description: success
  /bar:
    get:
      tags: ['Bar']
      operationId: 'GetAllBars'
      responses:
        '200':
          description: success
  /bar/{id}:
    get:
      tags: ['Bar']
      operationId: 'GetBarById'
      responses:
        '200':
          description: success
  /baz:
    get:
      tags: ['Baz']
      operationId: 'GetAllBazs'
      responses:
        '200':
          description: success
";

    private static async Task<OpenApiDocument> LoadDocument()
    {
        var json = OpenApiYamlDocument.FromYamlAsync(OpenApiSpec)
            .ContinueWith(t => t.Result.ToJson());
        return await OpenApiDocument.FromJsonAsync(await json);
    }

    private static async Task<OpenApiDocument> LoadSwaggerDocument()
    {
        var json = OpenApiYamlDocument.FromYamlAsync(SwaggerSpec)
            .ContinueWith(t => t.Result.ToJson());
        return await OpenApiDocument.FromJsonAsync(await json);
    }

    [Test]
    public async Task FilterByTags_WithEmptyTags_ReturnsSameDocument()
    {
        var document = await LoadDocument();
        var result = RefitDocumentFilter.FilterByTags(document, []);

        result.Paths.Count.Should().Be(document.Paths.Count);
    }

    [Test]
    public async Task FilterByTags_RemovesNonMatchingOperations()
    {
        var document = await LoadDocument();
        var result = RefitDocumentFilter.FilterByTags(document, ["Bar"]);

        result.Paths.Should().ContainKey("/bar");
        result.Paths.Should().ContainKey("/bar/{id}");
        result.Paths.Should().NotContainKey("/foo");
        result.Paths.Should().NotContainKey("/baz");
    }

    [Test]
    public async Task FilterByTags_MultipleTags_KeepsAllMatching()
    {
        var document = await LoadDocument();
        var result = RefitDocumentFilter.FilterByTags(document, ["Foo", "Baz"]);

        result.Paths.Should().ContainKey("/foo");
        result.Paths.Should().ContainKey("/foo/{id}");
        result.Paths.Should().ContainKey("/baz");
        result.Paths.Should().NotContainKey("/bar");
        result.Paths.Should().NotContainKey("/bar/{id}");
    }

    [Test]
    public async Task FilterByTags_DoesNotMutateOriginalDocument()
    {
        var document = await LoadDocument();
        var originalCount = document.Paths.Count;

        RefitDocumentFilter.FilterByTags(document, ["Bar"]);

        document.Paths.Count.Should().Be(originalCount);
    }

    [Test]
    public async Task FilterByPath_WithEmptyPatterns_ReturnsSameDocument()
    {
        var document = await LoadDocument();
        var result = RefitDocumentFilter.FilterByPath(document, []);

        result.Paths.Count.Should().Be(document.Paths.Count);
    }

    [Test]
    public async Task FilterByPath_KeepsMatchingPaths()
    {
        var document = await LoadDocument();
        var result = RefitDocumentFilter.FilterByPath(document, ["^/foo"]);

        result.Paths.Should().ContainKey("/foo");
        result.Paths.Should().ContainKey("/foo/{id}");
        result.Paths.Should().NotContainKey("/bar");
        result.Paths.Should().NotContainKey("/baz");
    }

    [Test]
    public async Task FilterByPath_MultiplePatterns_KeepsAllMatching()
    {
        var document = await LoadDocument();
        var result = RefitDocumentFilter.FilterByPath(document, ["^/bar$", "^/baz"]);

        result.Paths.Should().ContainKey("/bar");
        result.Paths.Should().ContainKey("/baz");
        result.Paths.Should().NotContainKey("/foo");
        result.Paths.Should().NotContainKey("/foo/{id}");
        result.Paths.Should().NotContainKey("/bar/{id}");
    }

    [Test]
    public async Task FilterByPath_DoesNotMutateOriginalDocument()
    {
        var document = await LoadDocument();
        var originalCount = document.Paths.Count;

        RefitDocumentFilter.FilterByPath(document, ["^/bar$"]);

        document.Paths.Count.Should().Be(originalCount);
    }

    [Test]
    public async Task FilterByTags_WithSwagger20_RemovesNonMatchingOperations()
    {
        var document = await LoadSwaggerDocument();
        var result = RefitDocumentFilter.FilterByTags(document, ["Bar"]);

        result.Paths.Should().ContainKey("/bar");
        result.Paths.Should().ContainKey("/bar/{id}");
        result.Paths.Should().NotContainKey("/foo");
        result.Paths.Should().NotContainKey("/baz");
    }

    [Test]
    public async Task FilterByTags_WithSwagger20_MultipleTags_KeepsAllMatching()
    {
        var document = await LoadSwaggerDocument();
        var result = RefitDocumentFilter.FilterByTags(document, ["Foo", "Baz"]);

        result.Paths.Should().ContainKey("/foo");
        result.Paths.Should().ContainKey("/foo/{id}");
        result.Paths.Should().ContainKey("/baz");
        result.Paths.Should().NotContainKey("/bar");
        result.Paths.Should().NotContainKey("/bar/{id}");
    }

    [Test]
    public async Task FilterByPath_WithSwagger20_KeepsMatchingPaths()
    {
        var document = await LoadSwaggerDocument();
        var result = RefitDocumentFilter.FilterByPath(document, ["^/foo"]);

        result.Paths.Should().ContainKey("/foo");
        result.Paths.Should().ContainKey("/foo/{id}");
        result.Paths.Should().NotContainKey("/bar");
        result.Paths.Should().NotContainKey("/baz");
    }
}
