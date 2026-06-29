using FluentAssertions;
using Refitter.Core;

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
}
