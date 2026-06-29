using FluentAssertions;
using Refitter.Core;
using Refitter.Tests.TestUtilities;

namespace Refitter.Tests.OpenApi;

public class ReferenceGuardTests
{
    private const string MainTemplate = @"{
  ""openapi"": ""3.0.0"",
  ""info"": { ""title"": ""t"", ""version"": ""1"" },
  ""paths"": { ""/p"": { ""get"": { ""operationId"": ""g"", ""responses"": { ""200"": {
    ""description"": ""ok"", ""content"": { ""application/json"": { ""schema"": { ""$ref"": ""__REF__"" } } } } } } } },
  ""components"": { ""schemas"": {} }
}";

    private const string Swagger2Template = @"{
  ""swagger"": ""2.0"",
  ""info"": { ""title"": ""t"", ""version"": ""1"" },
  ""paths"": { ""/p"": { ""get"": { ""operationId"": ""g"", ""responses"": { ""200"": {
    ""description"": ""ok"", ""schema"": { ""$ref"": ""__REF__"" } } } } } },
  ""definitions"": {}
}";

    private const string YamlMainTemplate = @"openapi: 3.0.0
info:
  title: t
  version: '1'
paths:
  /p:
    get:
      operationId: g
      responses:
        '200':
          description: ok
          content:
            application/json:
              schema:
                $ref: __REF__
components:
  schemas: {}
";

    private const string Leaf = @"{ ""type"": ""object"", ""properties"": { ""ok"": { ""type"": ""string"" } } }";

    private static string Write(string dir, string name, string contents)
    {
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, name);
        File.WriteAllText(path, contents);
        return path;
    }

    private static string NewRoot() =>
        Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

    [Test]
    public async Task Blocks_Remote_Reference_By_Default()
    {
        var root = NewRoot();
        var path = Write(root, "spec.json", MainTemplate.Replace("__REF__", "http://127.0.0.1:1/evil.json"));

        var act = () => ReferenceGuard.ValidateAsync(path, allowRemoteReferences: false);

        await act.Should().ThrowAsync<ReferenceResolutionException>();
    }

    [Test]
    public async Task Allows_Remote_Reference_When_Enabled()
    {
        var root = NewRoot();
        var path = Write(root, "spec.json", MainTemplate.Replace("__REF__", "http://127.0.0.1:1/evil.json"));

        var act = () => ReferenceGuard.ValidateAsync(path, allowRemoteReferences: true);

        await act.Should().NotThrowAsync<ReferenceResolutionException>();
    }

    [Test]
    public async Task Blocks_Absolute_Out_Of_Tree_Reference()
    {
        var root = NewRoot();
        var outside = Write(NewRoot(), "secret.json", Leaf);
        var path = Write(root, "spec.json", MainTemplate.Replace("__REF__", outside.Replace("\\", "/")));

        var act = () => ReferenceGuard.ValidateAsync(path, allowRemoteReferences: false);

        await act.Should().ThrowAsync<ReferenceResolutionException>();
    }

    [Test]
    public async Task Blocks_Parent_Traversal_Reference()
    {
        var root = NewRoot();
        var path = Write(root, "spec.json", MainTemplate.Replace("__REF__", "../../secret.json"));

        var act = () => ReferenceGuard.ValidateAsync(path, allowRemoteReferences: false);

        await act.Should().ThrowAsync<ReferenceResolutionException>();
    }

    [Test]
    public async Task Allows_In_Tree_Relative_Reference()
    {
        var root = NewRoot();
        Write(root, "leaf.json", Leaf);
        var path = Write(root, "spec.json", MainTemplate.Replace("__REF__", "./leaf.json"));

        var act = () => ReferenceGuard.ValidateAsync(path, allowRemoteReferences: false);

        await act.Should().NotThrowAsync();
    }

    [Test]
    public async Task Allows_Internal_Fragment_Reference()
    {
        var root = NewRoot();
        var path = Write(root, "spec.json", MainTemplate.Replace("__REF__", "#/components/schemas/Pet"));

        var act = () => ReferenceGuard.ValidateAsync(path, allowRemoteReferences: false);

        await act.Should().NotThrowAsync();
    }

    [Test]
    public async Task Blocks_Remote_Reference_In_Swagger2_Spec()
    {
        var root = NewRoot();
        var path = Write(root, "swagger.json", Swagger2Template.Replace("__REF__", "http://evil.com/schema.json"));

        var act = () => ReferenceGuard.ValidateAsync(path, allowRemoteReferences: false);

        await act.Should().ThrowAsync<ReferenceResolutionException>();
    }

    [Test]
    public async Task Allows_In_Tree_Reference_In_Swagger2_Spec()
    {
        var root = NewRoot();
        Write(root, "leaf.json", Leaf);
        var path = Write(root, "swagger.json", Swagger2Template.Replace("__REF__", "./leaf.json"));

        var act = () => ReferenceGuard.ValidateAsync(path, allowRemoteReferences: false);

        await act.Should().NotThrowAsync();
    }

    [Test]
    public async Task Blocks_Remote_Reference_In_YAML_Unquoted()
    {
        var root = NewRoot();
        var path = Write(root, "spec.yaml", YamlMainTemplate.Replace("__REF__", "http://evil.com/schema.yaml"));

        var act = () => ReferenceGuard.ValidateAsync(path, allowRemoteReferences: false);

        await act.Should().ThrowAsync<ReferenceResolutionException>();
    }

    [Test]
    public async Task Allows_In_Tree_Reference_In_YAML_Unquoted()
    {
        var root = NewRoot();
        Write(root, "leaf.json", Leaf);
        var path = Write(root, "spec.yaml", YamlMainTemplate.Replace("__REF__", "./leaf.json"));

        var act = () => ReferenceGuard.ValidateAsync(path, allowRemoteReferences: false);

        await act.Should().NotThrowAsync();
    }

    [Test]
    public async Task Blocks_Parent_Traversal_In_YAML_Unquoted()
    {
        var root = NewRoot();
        var path = Write(root, "spec.yaml", YamlMainTemplate.Replace("__REF__", "../secret.yaml#/Pet"));

        var act = () => ReferenceGuard.ValidateAsync(path, allowRemoteReferences: false);

        await act.Should().ThrowAsync<ReferenceResolutionException>();
    }

    [Test]
    public async Task Ignores_Whitespace_OpenApiPath()
    {
        var act = () => ReferenceGuard.ValidateAsync("   ", allowRemoteReferences: false);

        await act.Should().NotThrowAsync();
    }

    [Test]
    public async Task Ignores_Missing_Local_File()
    {
        var path = Path.Combine(NewRoot(), "does-not-exist.json");

        var act = () => ReferenceGuard.ValidateAsync(path, allowRemoteReferences: false);

        await act.Should().NotThrowAsync();
    }

    [Test]
    public async Task Remote_Validation_Blocks_Dot_Relative_Reference_By_Default()
    {
        var content = MainTemplate.Replace("__REF__", "./schema.json#/components/schemas/Pet");

        var act = () => ReferenceGuard.ValidateAsync("https://example.com/openapi.json", content, allowRemoteReferences: false);

        await act.Should().ThrowAsync<ReferenceResolutionException>();
    }

    [Test]
    public async Task Remote_Validation_Allows_Dot_Relative_Reference_When_Enabled()
    {
        var content = MainTemplate.Replace("__REF__", "./schema.json#/components/schemas/Pet");

        var act = () => ReferenceGuard.ValidateAsync("https://example.com/openapi.json", content, allowRemoteReferences: true);

        await act.Should().NotThrowAsync();
    }

    [Test]
    public async Task Remote_Validation_Blocks_Non_Http_Scheme()
    {
        var content = MainTemplate.Replace("__REF__", "file:///etc/passwd");

        var act = () => ReferenceGuard.ValidateAsync("https://example.com/openapi.json", content, allowRemoteReferences: true);

        await act.Should().ThrowAsync<ReferenceResolutionException>();
    }

    [Test]
    public async Task Remote_Validation_Blocks_Bare_Relative_Reference_By_Default()
    {
        var content = MainTemplate.Replace("__REF__", "schema.json#/components/schemas/Pet");

        var act = () => ReferenceGuard.ValidateAsync("https://example.com/openapi.json", content, allowRemoteReferences: false);

        await act.Should().ThrowAsync<ReferenceResolutionException>();
    }

    [Test]
    public async Task Remote_Validation_Allows_Bare_Relative_Reference_When_Enabled()
    {
        var content = MainTemplate.Replace("__REF__", "schema.json#/components/schemas/Pet");

        var act = () => ReferenceGuard.ValidateAsync("https://example.com/openapi.json", content, allowRemoteReferences: true);

        await act.Should().NotThrowAsync();
    }

    [Test]
    public async Task Throws_When_Remote_Entry_Cannot_Be_Downloaded()
    {
        var act = () => ReferenceGuard.ValidateAsync("http://127.0.0.1:1/openapi.json", allowRemoteReferences: true);

        await act.Should().ThrowAsync<ReferenceResolutionException>()
            .WithMessage("*Failed to read OpenAPI document*");
    }

    [Test]
    public async Task Downloads_Remote_Entry_And_Validates_When_Reachable()
    {
        var remoteSpec = MainTemplate.Replace("__REF__", "#/components/schemas/InlineSchema")
            .Replace("\"components\": { \"schemas\": {} }", "\"components\": { \"schemas\": { \"InlineSchema\": { \"type\": \"object\" } } }");
        await using var server = new LocalHttpServer(remoteSpec);

        var act = () => ReferenceGuard.ValidateAsync(server.Url, allowRemoteReferences: false);

        await act.Should().NotThrowAsync();
    }

    [Test]
    public async Task Handles_Circular_Local_References_Without_Recursion_Error()
    {
        var root = NewRoot();
        Write(root, "a.json", MainTemplate.Replace("__REF__", "./b.json"));
        Write(root, "b.json", MainTemplate.Replace("__REF__", "./a.json"));

        var act = () => ReferenceGuard.ValidateAsync(Path.Combine(root, "a.json"), allowRemoteReferences: false);

        await act.Should().NotThrowAsync();
    }

    [Test]
    public async Task Allows_Missing_Local_Referenced_File()
    {
        var root = NewRoot();
        var path = Write(root, "spec.json", MainTemplate.Replace("__REF__", "./missing.json"));

        var act = () => ReferenceGuard.ValidateAsync(path, allowRemoteReferences: false);

        await act.Should().NotThrowAsync();
    }

    [Test]
    public async Task Throws_OperationCanceled_When_Cancellation_Requested()
    {
        var root = NewRoot();
        var path = Write(root, "spec.json", MainTemplate.Replace("__REF__", "./leaf.json"));
        Write(root, "leaf.json", Leaf);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var act = () => ReferenceGuard.ValidateAsync(path, allowRemoteReferences: false, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
