using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Refitter.Tests.Parity;

/// <summary>
/// The OpenAPI documents the golden-output parity harness generates code from: every spec in
/// test/OpenAPI, plus every spec embedded as a string constant in the test project (the scenario
/// tests hold most of the edge cases).
/// </summary>
public static class ParitySpecs
{
    private const string ScenarioPrefix = "scenarios/";
    private const long LargeSpecThreshold = 500 * 1024;

    private static readonly Regex YamlSpecMarker = new(@"(?m)^\s*[""']?(openapi|swagger)[""']?\s*:", RegexOptions.Compiled);
    private static readonly Regex JsonSpecMarker = new(@"""(openapi|swagger)""\s*:", RegexOptions.Compiled);

    // Operations without a responses object fail to load today (also skipped by test/smoke-tests.ps1)
    private static readonly HashSet<string> UnsupportedFileSpecs =
    [
        "v3.1/non-oauth-scopes.json",
        "v3.1/non-oauth-scopes.yaml",
    ];

    private static readonly Lazy<IReadOnlyList<ParitySpec>> AllSpecs = new(Load);

    public static IReadOnlyList<ParitySpec> All => AllSpecs.Value;

    public static ParitySpec Get(string id) => All.First(spec => spec.Id == id);

    private static IReadOnlyList<ParitySpec> Load() =>
        LoadFileSpecs()
            .Concat(LoadEmbeddedSpecs())
            .ToList();

    private static IEnumerable<ParitySpec> LoadFileSpecs()
    {
        var specsFolder = Path.Combine(RepositoryPaths.Root, "test", "OpenAPI");
        return Directory
            .EnumerateFiles(specsFolder, "*.*", SearchOption.AllDirectories)
            .Where(p => p.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
                        || p.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase))
            .Select(p => (Path: p, Id: Path.GetRelativePath(specsFolder, p).Replace('\\', '/')))
            .Where(p => !UnsupportedFileSpecs.Contains(p.Id))
            .OrderBy(p => p.Id, StringComparer.Ordinal)
            .Select(p => new ParitySpec(p.Id, p.Path, new FileInfo(p.Path).Length > LargeSpecThreshold));
    }

    private static IEnumerable<ParitySpec> LoadEmbeddedSpecs()
    {
        var seenContents = new HashSet<string>(StringComparer.Ordinal);
        var root = Path.Combine(Path.GetTempPath(), "refitter-parity");

        var fields = typeof(ParitySpecs).Assembly
            .GetTypes()
            .Where(type => type.Namespace?.StartsWith("Refitter.Tests.Parity", StringComparison.Ordinal) != true)
            .SelectMany(type => type.GetFields(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic))
            .Where(field => field is { IsLiteral: true } && field.FieldType == typeof(string))
            .Select(field => (Field: field, Content: (string)field.GetRawConstantValue()!))
            .Where(item => IsSpec(item.Content))
            .OrderBy(item => GetId(item.Field), StringComparer.Ordinal);

        foreach (var (field, content) in fields)
        {
            // Checkouts can differ in line endings, so specs that only differ in them are the same spec
            if (!seenContents.Add(content.Replace("\r\n", "\n")))
                continue;

            var id = GetId(field);
            var isJson = content.TrimStart().StartsWith("{", StringComparison.Ordinal);
            var directory = Path.Combine(root, Hash(id));
            var path = Path.Combine(directory, isJson ? "openapi.json" : "openapi.yaml");
            Directory.CreateDirectory(directory);
            if (!File.Exists(path) || File.ReadAllText(path) != content)
            {
                File.WriteAllText(path, content);
            }

            yield return new ParitySpec(id, path, IsLarge: false);
        }
    }

    private static bool IsSpec(string content) =>
        content.TrimStart().StartsWith("{", StringComparison.Ordinal)
            ? JsonSpecMarker.IsMatch(content)
            : YamlSpecMarker.IsMatch(content);

    private static string GetId(FieldInfo field) =>
        $"{ScenarioPrefix}{field.DeclaringType!.FullName!.Replace("Refitter.Tests.", string.Empty).Replace('+', '.')}.{field.Name}";

    private static string Hash(string value)
    {
        using var sha = SHA256.Create();
        return Convert.ToHexString(sha.ComputeHash(Encoding.UTF8.GetBytes(value)))[..16];
    }
}

/// <summary>A spec in the parity corpus: its snapshot id and the file it is loaded from.</summary>
public sealed record ParitySpec(string Id, string Path, bool IsLarge);
