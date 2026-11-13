using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.TestUtilities;

namespace Refitter.Tests.OpenApi;

[TestClass]
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
        Path.Combine(TestDirectory.Root, Guid.NewGuid().ToString());

    [TestMethod]
    public async Task Blocks_Remote_Reference_By_Default()
    {
        var root = NewRoot();
        var path = Write(root, "spec.json", MainTemplate.Replace("__REF__", "http://127.0.0.1:1/evil.json"));

        var act = () => ReferenceGuard.ValidateAsync(path, allowRemoteReferences: false);

        await act.Should().ThrowAsync<ReferenceResolutionException>();
    }

    [TestMethod]
    public async Task Allows_Remote_Reference_When_Enabled()
    {
        var root = NewRoot();
        var path = Write(root, "spec.json", MainTemplate.Replace("__REF__", "http://127.0.0.1:1/evil.json"));

        var act = () => ReferenceGuard.ValidateAsync(path, allowRemoteReferences: true);

        await act.Should().NotThrowAsync<ReferenceResolutionException>();
    }

    [TestMethod]
    public async Task Blocks_Absolute_Out_Of_Tree_Reference()
    {
        var root = NewRoot();
        var outside = Write(NewRoot(), "secret.json", Leaf);
        var path = Write(root, "spec.json", MainTemplate.Replace("__REF__", outside.Replace("\\", "/")));

        var act = () => ReferenceGuard.ValidateAsync(path, allowRemoteReferences: false);

        await act.Should().ThrowAsync<ReferenceResolutionException>();
    }

    [TestMethod]
    public async Task Blocks_Parent_Traversal_Reference()
    {
        var root = NewRoot();
        var path = Write(root, "spec.json", MainTemplate.Replace("__REF__", "../../secret.json"));

        var act = () => ReferenceGuard.ValidateAsync(path, allowRemoteReferences: false);

        await act.Should().ThrowAsync<ReferenceResolutionException>();
    }

    [TestMethod]
    public async Task Allows_In_Tree_Relative_Reference()
    {
        var root = NewRoot();
        Write(root, "leaf.json", Leaf);
        var path = Write(root, "spec.json", MainTemplate.Replace("__REF__", "./leaf.json"));

        var act = () => ReferenceGuard.ValidateAsync(path, allowRemoteReferences: false);

        await act.Should().NotThrowAsync();
    }

    [TestMethod]
    public async Task Allows_Internal_Fragment_Reference()
    {
        var root = NewRoot();
        var path = Write(root, "spec.json", MainTemplate.Replace("__REF__", "#/components/schemas/Pet"));

        var act = () => ReferenceGuard.ValidateAsync(path, allowRemoteReferences: false);

        await act.Should().NotThrowAsync();
    }

    [TestMethod]
    public async Task Blocks_Remote_Reference_In_Swagger2_Spec()
    {
        var root = NewRoot();
        var path = Write(root, "swagger.json", Swagger2Template.Replace("__REF__", "http://evil.com/schema.json"));

        var act = () => ReferenceGuard.ValidateAsync(path, allowRemoteReferences: false);

        await act.Should().ThrowAsync<ReferenceResolutionException>();
    }

    [TestMethod]
    public async Task Allows_In_Tree_Reference_In_Swagger2_Spec()
    {
        var root = NewRoot();
        Write(root, "leaf.json", Leaf);
        var path = Write(root, "swagger.json", Swagger2Template.Replace("__REF__", "./leaf.json"));

        var act = () => ReferenceGuard.ValidateAsync(path, allowRemoteReferences: false);

        await act.Should().NotThrowAsync();
    }

    [TestMethod]
    public async Task Blocks_Remote_Reference_In_YAML_Unquoted()
    {
        var root = NewRoot();
        var path = Write(root, "spec.yaml", YamlMainTemplate.Replace("__REF__", "http://evil.com/schema.yaml"));

        var act = () => ReferenceGuard.ValidateAsync(path, allowRemoteReferences: false);

        await act.Should().ThrowAsync<ReferenceResolutionException>();
    }

    [TestMethod]
    public async Task Allows_In_Tree_Reference_In_YAML_Unquoted()
    {
        var root = NewRoot();
        Write(root, "leaf.json", Leaf);
        var path = Write(root, "spec.yaml", YamlMainTemplate.Replace("__REF__", "./leaf.json"));

        var act = () => ReferenceGuard.ValidateAsync(path, allowRemoteReferences: false);

        await act.Should().NotThrowAsync();
    }

    [TestMethod]
    public async Task Blocks_Parent_Traversal_In_YAML_Unquoted()
    {
        var root = NewRoot();
        var path = Write(root, "spec.yaml", YamlMainTemplate.Replace("__REF__", "../secret.yaml#/Pet"));

        var act = () => ReferenceGuard.ValidateAsync(path, allowRemoteReferences: false);

        await act.Should().ThrowAsync<ReferenceResolutionException>();
    }

    [TestMethod]
    public async Task Ignores_Whitespace_OpenApiPath()
    {
        var act = () => ReferenceGuard.ValidateAsync("   ", allowRemoteReferences: false);

        await act.Should().NotThrowAsync();
    }

    [TestMethod]
    public async Task Ignores_Missing_Local_File()
    {
        var path = Path.Combine(NewRoot(), "does-not-exist.json");

        var act = () => ReferenceGuard.ValidateAsync(path, allowRemoteReferences: false);

        await act.Should().NotThrowAsync();
    }

    [TestMethod]
    public async Task Remote_Validation_Blocks_Dot_Relative_Reference_By_Default()
    {
        var content = MainTemplate.Replace("__REF__", "./schema.json#/components/schemas/Pet");

        var act = () => ReferenceGuard.ValidateAsync("https://example.com/openapi.json", content, allowRemoteReferences: false);

        await act.Should().ThrowAsync<ReferenceResolutionException>();
    }

    [TestMethod]
    public async Task Remote_Validation_Allows_Dot_Relative_Reference_When_Enabled()
    {
        var content = MainTemplate.Replace("__REF__", "./schema.json#/components/schemas/Pet");

        var act = () => ReferenceGuard.ValidateAsync("https://example.com/openapi.json", content, allowRemoteReferences: true);

        await act.Should().NotThrowAsync();
    }

    [TestMethod]
    public async Task Remote_Validation_Blocks_Non_Http_Scheme()
    {
        var content = MainTemplate.Replace("__REF__", "file:///etc/passwd");

        var act = () => ReferenceGuard.ValidateAsync("https://example.com/openapi.json", content, allowRemoteReferences: true);

        await act.Should().ThrowAsync<ReferenceResolutionException>();
    }

    [TestMethod]
    public async Task Remote_Validation_Blocks_Bare_Relative_Reference_By_Default()
    {
        var content = MainTemplate.Replace("__REF__", "schema.json#/components/schemas/Pet");

        var act = () => ReferenceGuard.ValidateAsync("https://example.com/openapi.json", content, allowRemoteReferences: false);

        await act.Should().ThrowAsync<ReferenceResolutionException>();
    }

    [TestMethod]
    public async Task Remote_Validation_Allows_Bare_Relative_Reference_When_Enabled()
    {
        var content = MainTemplate.Replace("__REF__", "schema.json#/components/schemas/Pet");

        var act = () => ReferenceGuard.ValidateAsync("https://example.com/openapi.json", content, allowRemoteReferences: true);

        await act.Should().NotThrowAsync();
    }

    [TestMethod]
    public async Task Throws_When_Remote_Entry_Cannot_Be_Downloaded()
    {
        var act = () => ReferenceGuard.ValidateAsync("http://127.0.0.1:1/openapi.json", allowRemoteReferences: true);

        await act.Should().ThrowAsync<ReferenceResolutionException>()
            .WithMessage("*Failed to read OpenAPI document*");
    }

    [TestMethod]
    public async Task Downloads_Remote_Entry_And_Validates_When_Reachable()
    {
        var remoteSpec = MainTemplate.Replace("__REF__", "#/components/schemas/InlineSchema")
            .Replace("\"components\": { \"schemas\": {} }", "\"components\": { \"schemas\": { \"InlineSchema\": { \"type\": \"object\" } } }");
        await using var server = new LocalHttpServer(remoteSpec);

        var act = () => ReferenceGuard.ValidateAsync(server.Url, allowRemoteReferences: false);

        await act.Should().NotThrowAsync();
    }

    [TestMethod]
    public async Task Handles_Circular_Local_References_Without_Recursion_Error()
    {
        var root = NewRoot();
        Write(root, "a.json", MainTemplate.Replace("__REF__", "./b.json"));
        Write(root, "b.json", MainTemplate.Replace("__REF__", "./a.json"));

        var act = () => ReferenceGuard.ValidateAsync(Path.Combine(root, "a.json"), allowRemoteReferences: false);

        await act.Should().NotThrowAsync();
    }

    [TestMethod]
    public async Task Allows_Missing_Local_Referenced_File()
    {
        var root = NewRoot();
        var path = Write(root, "spec.json", MainTemplate.Replace("__REF__", "./missing.json"));

        var act = () => ReferenceGuard.ValidateAsync(path, allowRemoteReferences: false);

        await act.Should().NotThrowAsync();
    }

    [TestMethod]
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

    [TestMethod]
    public async Task Remote_Validation_Blocks_Unresolvable_Reference_By_Default()
    {
        // Neither an absolute URI nor resolvable against the document URI, so it is treated as remote
        var content = MainTemplate.Replace("__REF__", "http://[unparseable");

        var act = () => ReferenceGuard.ValidateAsync("https://example.com/openapi.json", content, allowRemoteReferences: false);

        await act.Should().ThrowAsync<ReferenceResolutionException>();
    }

    [TestMethod]
    public async Task Remote_Validation_Allows_Unresolvable_Reference_When_Enabled()
    {
        var content = MainTemplate.Replace("__REF__", "http://[unparseable");

        var act = () => ReferenceGuard.ValidateAsync("https://example.com/openapi.json", content, allowRemoteReferences: true);

        await act.Should().NotThrowAsync();
    }

    [TestMethod]
    public async Task Remote_Validation_Allows_Unresolvable_Root_Relative_Reference()
    {
        // Unparseable as a URI but root-relative, so it is not treated as a remote reference
        var content = MainTemplate.Replace("__REF__", "//");

        var act = () => ReferenceGuard.ValidateAsync("https://example.com/openapi.json", content, allowRemoteReferences: false);

        await act.Should().NotThrowAsync();
    }

    [TestMethod]
    public async Task Throws_When_Local_File_Cannot_Be_Read()
    {
        var root = NewRoot();
        var path = Write(root, "spec.json", MainTemplate.Replace("__REF__", "#/components/schemas/Pet"));

        // Hold the file exclusively so the guard's read fails. .NET enforces FileShare
        // on Unix too (via flock), so this is deterministic on the Linux CI runner as
        // well as on Windows - verified against .NET 10 on both.
        using var exclusive = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None);

        var act = () => ReferenceGuard.ValidateAsync(path, allowRemoteReferences: false);

        await act.Should().ThrowAsync<ReferenceResolutionException>()
            .WithMessage("*Failed to read OpenAPI document*");
    }

    // GHSA-4f49-pm8q-mx4f: JSON escapes are decoded by the parsers that resolve references,
    // so the guard must see the decoded key and value as well
    private const string EscapedKeyJson = @"{
  ""openapi"": ""3.0.0"",
  ""info"": { ""title"": ""t"", ""version"": ""1"" },
  ""paths"": {},
  ""components"": { ""schemas"": { ""Pet"": { ""\u0024ref"": ""http://127.0.0.1:1/schema.json"" } } }
}";

    private const string EscapedValueJson = @"{
  ""openapi"": ""3.0.0"",
  ""info"": { ""title"": ""t"", ""version"": ""1"" },
  ""paths"": {},
  ""components"": { ""schemas"": { ""Pet"": { ""$ref"": ""http:\/\/127.0.0.1:1\/schema.json"" } } }
}";

    [TestMethod]
    [DataRow(EscapedKeyJson)]
    [DataRow(EscapedValueJson)]
    public async Task Blocks_Json_Escaped_Remote_Reference_By_Default(string contents)
    {
        var path = Write(NewRoot(), "spec.json", contents);

        var act = () => ReferenceGuard.ValidateAsync(path, allowRemoteReferences: false);

        await act.Should().ThrowAsync<ReferenceResolutionException>()
            .WithMessage("*http://127.0.0.1:1/schema.json*");
    }

    [TestMethod]
    [DataRow(EscapedKeyJson)]
    [DataRow(EscapedValueJson)]
    public async Task Remote_Validation_Blocks_Json_Escaped_Remote_Reference_By_Default(string contents)
    {
        var act = () => ReferenceGuard.ValidateAsync("https://example.com/openapi.json", contents, allowRemoteReferences: false);

        await act.Should().ThrowAsync<ReferenceResolutionException>()
            .WithMessage("*http://127.0.0.1:1/schema.json*");
    }

    [TestMethod]
    [DataRow(EscapedKeyJson)]
    [DataRow(EscapedValueJson)]
    public async Task Allows_Json_Escaped_Remote_Reference_When_Enabled(string contents)
    {
        var path = Write(NewRoot(), "spec.json", contents);

        var act = () => ReferenceGuard.ValidateAsync(path, allowRemoteReferences: true);

        await act.Should().NotThrowAsync();
    }

    private const string YamlHeader = @"openapi: 3.0.0
info:
  title: t
  version: '1'
paths: {}
";

    private const string EscapedKeyYaml = YamlHeader + @"components:
  schemas:
    Pet:
      ""\x24ref"": http://127.0.0.1:1/schema.json
";

    private const string EscapedValueYaml = YamlHeader + @"components:
  schemas:
    Pet:
      $ref: ""http:\x2F\x2F127.0.0.1:1\x2Fschema.json""
";

    private const string AliasedValueYaml = YamlHeader + @"x-url: &url http://127.0.0.1:1/schema.json
components:
  schemas:
    Pet:
      $ref: *url
";

    private const string AliasedMappingYaml = YamlHeader + @"x-shared: &shared
  $ref: http://127.0.0.1:1/schema.json
components:
  schemas:
    Pet: *shared
    Cat: *shared
";

    [TestMethod]
    [DataRow(EscapedKeyYaml)]
    [DataRow(EscapedValueYaml)]
    [DataRow(AliasedValueYaml)]
    [DataRow(AliasedMappingYaml)]
    public async Task Blocks_Yaml_Obfuscated_Remote_Reference_By_Default(string contents)
    {
        var path = Write(NewRoot(), "spec.yaml", contents);

        var act = () => ReferenceGuard.ValidateAsync(path, allowRemoteReferences: false);

        await act.Should().ThrowAsync<ReferenceResolutionException>()
            .WithMessage("*http://127.0.0.1:1/schema.json*");
    }

    [TestMethod]
    [DataRow(EscapedKeyYaml)]
    [DataRow(EscapedValueYaml)]
    [DataRow(AliasedValueYaml)]
    public async Task Remote_Validation_Blocks_Yaml_Obfuscated_Remote_Reference_By_Default(string contents)
    {
        var act = () => ReferenceGuard.ValidateAsync("https://example.com/openapi.yaml", contents, allowRemoteReferences: false);

        await act.Should().ThrowAsync<ReferenceResolutionException>()
            .WithMessage("*http://127.0.0.1:1/schema.json*");
    }

    [TestMethod]
    [DataRow(EscapedKeyYaml)]
    [DataRow(EscapedValueYaml)]
    [DataRow(AliasedValueYaml)]
    public async Task Allows_Yaml_Obfuscated_Remote_Reference_When_Enabled(string contents)
    {
        var path = Write(NewRoot(), "spec.yaml", contents);

        var act = () => ReferenceGuard.ValidateAsync(path, allowRemoteReferences: true);

        await act.Should().NotThrowAsync();
    }

    private const string UnterminatedJson = @"{ ""openapi"": ""3.0.0"", ""paths"": { ""$ref"": ";

    private const string UnterminatedYaml = @"openapi: 3.0.0
paths: [unterminated
";

    private const string DuplicateRefKeyYaml = YamlHeader + @"components:
  schemas:
    Pet:
      $ref: '#/components/schemas/Other'
      $ref: http://127.0.0.1:1/schema.json
";

    [TestMethod]
    [DataRow("spec.json", UnterminatedJson)]
    [DataRow("spec.yaml", UnterminatedYaml)]
    [DataRow("spec.yaml", DuplicateRefKeyYaml)]
    public async Task Blocks_Document_That_Cannot_Be_Parsed(string fileName, string contents)
    {
        var path = Write(NewRoot(), fileName, contents);

        var act = () => ReferenceGuard.ValidateAsync(path, allowRemoteReferences: true);

        await act.Should().ThrowAsync<ReferenceResolutionException>()
            .WithMessage("*could not be parsed*");
    }

    [TestMethod]
    [DataRow(UnterminatedJson)]
    [DataRow(UnterminatedYaml)]
    [DataRow(DuplicateRefKeyYaml)]
    public async Task Remote_Validation_Blocks_Document_That_Cannot_Be_Parsed(string contents)
    {
        var act = () => ReferenceGuard.ValidateAsync("https://example.com/openapi.yaml", contents, allowRemoteReferences: true);

        await act.Should().ThrowAsync<ReferenceResolutionException>()
            .WithMessage("*could not be parsed*");
    }

    private const string CommentPrefixedJson =
        @"/*c*/{""openapi"":""3.0.0"",""info"":{""title"":""t"",""version"":""1""},""paths"":{},""components"":{""schemas"":{""Pet"":{""$ref"":""http://127.0.0.1:1/schema.json""}}}}";

    private const string CommentBeforeValueJson =
        @"{""openapi"":""3.0.0"",""info"":{""title"":""t"",""version"":""1""},""paths"":{},""components"":{""schemas"":{""Pet"":{""$ref"": /* c */ ""http://127.0.0.1:1/schema.json""}}}}";

    private const string TrailingCommentJson =
        @"{""openapi"":""3.0.0"",""info"":{""title"":""t"",""version"":""1""},""paths"":{},""components"":{""schemas"":{""Pet"":{""$ref"":""http://127.0.0.1:1/schema.json""}}}}
// trailing comment
";

    private const string DuplicateRefKeyJson =
        @"{""openapi"":""3.0.0"",""info"":{""title"":""t"",""version"":""1""},""paths"":{},""components"":{""schemas"":{""Pet"":{""$ref"":""#/components/schemas/Other"",""$ref"":""http://127.0.0.1:1/schema.json""}}}}";

    [TestMethod]
    [DataRow(CommentPrefixedJson)]
    [DataRow(CommentBeforeValueJson)]
    [DataRow(TrailingCommentJson)]
    [DataRow(DuplicateRefKeyJson)]
    public async Task Blocks_Remote_Reference_In_Json_With_Comments_Or_Duplicate_Keys(string contents)
    {
        var path = Write(NewRoot(), "spec.json", contents);

        var act = () => ReferenceGuard.ValidateAsync(path, allowRemoteReferences: false);

        await act.Should().ThrowAsync<ReferenceResolutionException>()
            .WithMessage("*Remote '$ref' \"http://127.0.0.1:1/schema.json\"*");
    }

    [TestMethod]
    [DataRow(CommentPrefixedJson)]
    [DataRow(CommentBeforeValueJson)]
    [DataRow(TrailingCommentJson)]
    [DataRow(DuplicateRefKeyJson)]
    public async Task Allows_Json_With_Comments_Or_Duplicate_Keys_When_Remote_Enabled(string contents)
    {
        var path = Write(NewRoot(), "spec.json", contents);

        var act = () => ReferenceGuard.ValidateAsync(path, allowRemoteReferences: true);

        await act.Should().NotThrowAsync();
    }

    [TestMethod]
    public async Task Blocks_Document_That_Is_A_Plain_Yaml_Scalar()
    {
        var path = Write(NewRoot(), "spec.yaml", "just some text\n");

        var act = () => ReferenceGuard.ValidateAsync(path, allowRemoteReferences: true);

        await act.Should().ThrowAsync<ReferenceResolutionException>()
            .WithMessage("*could not be parsed*");
    }

    [TestMethod]
    [DataRow("spec.yaml", "openapi: 3.0.0\npaths: {}\nx-extension:\n  $ref:\n    nested: value\n")]
    [DataRow("spec.json", @"{""openapi"":""3.0.0"",""paths"":{},""x-extension"":{""$ref"":5}}")]
    public async Task Ignores_Ref_Keys_Whose_Value_Is_Not_A_String(string fileName, string contents)
    {
        var path = Write(NewRoot(), fileName, contents);

        var act = () => ReferenceGuard.ValidateAsync(path, allowRemoteReferences: false);

        await act.Should().NotThrowAsync();
    }
}
