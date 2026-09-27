using AwesomeAssertions;
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

    private static Task<ApiDocument> LoadDocument() =>
        Task.FromResult(ApiDocumentLoader.Load(OpenApiSpec, null, isYaml: true));

    private static Task<ApiDocument> LoadSwaggerDocument() =>
        Task.FromResult(ApiDocumentLoader.Load(SwaggerSpec, null, isYaml: true));

    [Test]
    public async Task FilterByTags_WithEmptyTags_ReturnsSameDocument()
    {
        var document = await LoadDocument();
        var result = ApiDocumentFilter.FilterByTags(document, []);

        result.Paths.Count.Should().Be(document.Paths.Count);
    }

    [Test]
    public void FilterByTags_Throws_On_Null_Document()
    {
        var act = () => ApiDocumentFilter.FilterByTags(null!, ["Foo"]);

        act.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public async Task FilterByTags_Throws_On_Null_Tags()
    {
        var document = await LoadDocument();

        var act = () => ApiDocumentFilter.FilterByTags(document, null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public void FilterByPath_Throws_On_Null_Document()
    {
        var act = () => ApiDocumentFilter.FilterByPath(null!, ["^/foo"]);

        act.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public async Task FilterByPath_Throws_On_Null_Patterns()
    {
        var document = await LoadDocument();

        var act = () => ApiDocumentFilter.FilterByPath(document, null!);

        act.Should().Throw<ArgumentNullException>();
    }

    [Test]
    public async Task FilterByTags_RemovesNonMatchingOperations()
    {
        var document = await LoadDocument();
        var result = ApiDocumentFilter.FilterByTags(document, ["Bar"]);

        result.Paths.Should().ContainKey("/bar");
        result.Paths.Should().ContainKey("/bar/{id}");
        result.Paths.Should().NotContainKey("/foo");
        result.Paths.Should().NotContainKey("/baz");
    }

    [Test]
    public async Task FilterByTags_MultipleTags_KeepsAllMatching()
    {
        var document = await LoadDocument();
        var result = ApiDocumentFilter.FilterByTags(document, ["Foo", "Baz"]);

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

        ApiDocumentFilter.FilterByTags(document, ["Bar"]);

        document.Paths.Count.Should().Be(originalCount);
    }

    [Test]
    public async Task FilterByPath_WithEmptyPatterns_ReturnsSameDocument()
    {
        var document = await LoadDocument();
        var result = ApiDocumentFilter.FilterByPath(document, []);

        result.Paths.Count.Should().Be(document.Paths.Count);
    }

    [Test]
    public async Task FilterByPath_KeepsMatchingPaths()
    {
        var document = await LoadDocument();
        var result = ApiDocumentFilter.FilterByPath(document, ["^/foo"]);

        result.Paths.Should().ContainKey("/foo");
        result.Paths.Should().ContainKey("/foo/{id}");
        result.Paths.Should().NotContainKey("/bar");
        result.Paths.Should().NotContainKey("/baz");
    }

    [Test]
    public async Task FilterByPath_MultiplePatterns_KeepsAllMatching()
    {
        var document = await LoadDocument();
        var result = ApiDocumentFilter.FilterByPath(document, ["^/bar$", "^/baz"]);

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

        ApiDocumentFilter.FilterByPath(document, ["^/bar$"]);

        document.Paths.Count.Should().Be(originalCount);
    }

    [Test]
    public async Task FilterByTags_WithSwagger20_RemovesNonMatchingOperations()
    {
        var document = await LoadSwaggerDocument();
        var result = ApiDocumentFilter.FilterByTags(document, ["Bar"]);

        result.Paths.Should().ContainKey("/bar");
        result.Paths.Should().ContainKey("/bar/{id}");
        result.Paths.Should().NotContainKey("/foo");
        result.Paths.Should().NotContainKey("/baz");
    }

    [Test]
    public async Task FilterByTags_WithSwagger20_MultipleTags_KeepsAllMatching()
    {
        var document = await LoadSwaggerDocument();
        var result = ApiDocumentFilter.FilterByTags(document, ["Foo", "Baz"]);

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
        var result = ApiDocumentFilter.FilterByPath(document, ["^/foo"]);

        result.Paths.Should().ContainKey("/foo");
        result.Paths.Should().ContainKey("/foo/{id}");
        result.Paths.Should().NotContainKey("/bar");
        result.Paths.Should().NotContainKey("/baz");
    }

    [Test]
    public async Task FilterByTags_RemovesOperationsWithoutTags()
    {
        var document = ApiDocumentLoader.Load("""
            {
              "openapi": "3.0.1",
              "info": { "title": "Test", "version": "1.0" },
              "paths": {
                "/tagged": {
                  "get": {
                    "operationId": "getTagged",
                    "tags": [ "Foo" ],
                    "responses": { "200": { "description": "ok" } }
                  }
                },
                "/untagged": {
                  "get": {
                    "operationId": "getUntagged",
                    "responses": { "200": { "description": "ok" } }
                  }
                }
              }
            }
            """, null, isYaml: false);

        var result = ApiDocumentFilter.FilterByTags(document, ["Foo"]);

        result.Paths.Should().ContainKey("/tagged");
        result.Paths.Should().NotContainKey("/untagged");
    }
}
