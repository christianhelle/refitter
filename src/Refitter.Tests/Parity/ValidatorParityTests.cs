using System.Text;
using AwesomeAssertions;
using Refitter.Core.Validation;
using Refitter.Tests.TestUtilities;
using TUnit.Core;

namespace Refitter.Tests.Parity;

/// <summary>
/// Golden-output parity harness for <see cref="OpenApiValidator"/>: validates every spec in test/OpenAPI,
/// the test resources, the embedded scenario specs and the ValidatorFixtures folder, both as a local file
/// and served over HTTP, and compares the diagnostics and statistics to a checked-in snapshot.
/// Set REFITTER_UPDATE_SNAPSHOTS=1 to rewrite the snapshots instead of asserting.
/// </summary>
[Category("Parity")]
public class ValidatorParityTests
{
    private const string UpdateSnapshotsVariable = "REFITTER_UPDATE_SNAPSHOTS";

    private static readonly string TestsFolder = Path.Combine(RepositoryPaths.Root, "src", "Refitter.Tests");
    private static readonly string SnapshotsFolder = Path.Combine(TestsFolder, "Parity", "ValidatorSnapshots");

    public static IEnumerable<Func<ValidatorParitySpec>> Cases() =>
        ValidatorParitySpecs.All.Select(spec => (Func<ValidatorParitySpec>)(() => spec));

    [Test]
    [MethodDataSource(nameof(Cases))]
    public async Task Validation_Matches_Snapshot(ValidatorParitySpec spec)
    {
        var actual = await DescribeAsync(spec);
        var snapshotPath = GetSnapshotPath(spec);

        if (Environment.GetEnvironmentVariable(UpdateSnapshotsVariable) == "1")
        {
            Directory.CreateDirectory(Path.GetDirectoryName(snapshotPath)!);
            await File.WriteAllTextAsync(snapshotPath, actual);
            return;
        }

        File.Exists(snapshotPath).Should().BeTrue(
            $"snapshot {snapshotPath} should exist; run with {UpdateSnapshotsVariable}=1 to create it");

        var expected = (await File.ReadAllTextAsync(snapshotPath)).Replace("\r\n", "\n");
        actual.Should().Be(expected, $"validating {spec.Id} should match {snapshotPath}");
    }

    [Test]
    public void Every_Snapshot_Belongs_To_A_Spec()
    {
        var expected = ValidatorParitySpecs.All
            .Select(GetSnapshotPath)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var orphans = Directory
            .EnumerateFiles(SnapshotsFolder, "*.txt", SearchOption.AllDirectories)
            .Where(path => !expected.Contains(path))
            .ToList();

        orphans.Should().BeEmpty("snapshots of removed specs should be deleted");
    }

    private static string GetSnapshotPath(ValidatorParitySpec spec) =>
        Path.Combine(SnapshotsFolder, spec.Id.Replace('/', Path.DirectorySeparatorChar) + ".txt");

    private static async Task<string> DescribeAsync(ValidatorParitySpec spec)
    {
        var output = new StringBuilder();
        var directory = Path.GetDirectoryName(spec.Path)!;

        output.Append("== file\n");
        output.Append(Normalize(await ValidateAsync(spec.Path), directory, null));

        var content = await File.ReadAllTextAsync(spec.Path);
        await using var server = new LocalHttpServer(content, IsYaml(spec.Path) ? "application/yaml" : "application/json");
        output.Append("== url\n");
        output.Append(Normalize(await ValidateAsync(server.Url), directory, server.Url));

        return output.ToString();
    }

    private static async Task<string> ValidateAsync(string openApiPath)
    {
        OpenApiValidationResult result;
        try
        {
            result = await OpenApiValidator.Validate(openApiPath);
        }
        catch (Exception exception)
        {
            return $"exception: {DescribeException(exception)}\n";
        }

        var output = new StringBuilder();
        output.Append($"version: {result.Diagnostics.SpecificationVersion}\n");
        foreach (var error in result.Diagnostics.Errors)
        {
            output.Append($"error: {error.Pointer} | {error.Message}\n");
        }

        foreach (var warning in result.Diagnostics.Warnings)
        {
            output.Append($"warning: {warning.Pointer} | {warning.Message}\n");
        }

        var statistics = result.Statistics;
        output.Append(
            $"statistics: path items {statistics.PathItemCount}, operations {statistics.OperationCount}, " +
            $"parameters {statistics.ParameterCount}, request bodies {statistics.RequestBodyCount}, " +
            $"responses {statistics.ResponseCount}, links {statistics.LinkCount}, " +
            $"callbacks {statistics.CallbackCount}, schemas {statistics.SchemaCount}, " +
            $"headers {statistics.HeaderCount}\n");
        return output.ToString();
    }

    private static string DescribeException(Exception exception)
    {
        var kind = exception.GetType().Name switch
        {
            "OpenApiUnsupportedSpecVersionException" => "UnsupportedSpecificationVersion",
            var name => name,
        };

        return $"{kind}: {exception.Message}";
    }

    // The spec folder and the server port differ per run, so they are replaced with placeholders
    private static string Normalize(string text, string directory, string? url)
    {
        if (url != null)
        {
            text = text.Replace(url, "<url>");
        }

        var trimmed = directory.TrimEnd('\\', '/');
        foreach (var form in new[] { trimmed, trimmed.Replace('\\', '/') })
        {
            text = text.Replace(form, "<dir>");
        }

        return text.Replace("\r\n", "\n");
    }

    private static bool IsYaml(string path) =>
        path.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase)
        || path.EndsWith(".yml", StringComparison.OrdinalIgnoreCase);
}

/// <summary>A spec validated by <see cref="ValidatorParityTests"/>: its snapshot id and the file it is loaded from.</summary>
public sealed record ValidatorParitySpec(string Id, string Path)
{
    public override string ToString() => Id;
}

/// <summary>
/// The OpenAPI documents the validator parity harness validates: every spec in test/OpenAPI, the test
/// resources, the embedded scenario specs from <see cref="ParitySpecs"/>, and the deliberately broken
/// specs in the ValidatorFixtures folder (a folder there is one multi-file spec named openapi.*).
/// </summary>
public static class ValidatorParitySpecs
{
    private static readonly Lazy<IReadOnlyList<ValidatorParitySpec>> AllSpecs = new(Load);

    public static IReadOnlyList<ValidatorParitySpec> All => AllSpecs.Value;

    private static IReadOnlyList<ValidatorParitySpec> Load()
    {
        var testsFolder = Path.Combine(RepositoryPaths.Root, "src", "Refitter.Tests");
        var fixturesFolder = Path.Combine(testsFolder, "Parity", "ValidatorFixtures");

        return FilesIn("openapi", Path.Combine(RepositoryPaths.Root, "test", "OpenAPI"), SearchOption.AllDirectories)
            .Concat(FilesIn("resources", Path.Combine(testsFolder, "Resources"), SearchOption.AllDirectories))
            .Concat(FilesIn("fixtures", fixturesFolder, SearchOption.TopDirectoryOnly))
            .Concat(MultiFileFixtures(fixturesFolder))
            .Concat(ParitySpecs.All
                .Where(spec => spec.Id.StartsWith("scenarios/", StringComparison.Ordinal))
                .Select(spec => new ValidatorParitySpec(spec.Id, spec.Path)))
            .ToList();
    }

    private static IEnumerable<ValidatorParitySpec> FilesIn(string prefix, string folder, SearchOption searchOption) =>
        Directory
            .EnumerateFiles(folder, "*.*", searchOption)
            .Where(IsSpecFile)
            .Select(path => new ValidatorParitySpec(
                $"{prefix}/{Path.GetRelativePath(folder, path).Replace('\\', '/')}",
                path))
            .OrderBy(spec => spec.Id, StringComparer.Ordinal);

    private static IEnumerable<ValidatorParitySpec> MultiFileFixtures(string fixturesFolder) =>
        Directory
            .EnumerateDirectories(fixturesFolder)
            .Select(folder => Directory.EnumerateFiles(folder, "openapi.*").Single())
            .Select(path => new ValidatorParitySpec(
                $"fixtures/{Path.GetFileName(Path.GetDirectoryName(path))}",
                path))
            .OrderBy(spec => spec.Id, StringComparer.Ordinal);

    private static bool IsSpecFile(string path) =>
        path.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
        || path.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase);
}
