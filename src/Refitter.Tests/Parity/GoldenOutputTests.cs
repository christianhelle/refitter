using System.Text;
using System.Text.RegularExpressions;
using AwesomeAssertions;
using Refitter.Core;
using TUnit.Core;

namespace Refitter.Tests.Parity;

/// <summary>
/// Golden-output parity harness: generates code for every spec in test/OpenAPI with every
/// settings variant in <see cref="ParityVariants"/> and compares it to a checked-in snapshot.
/// Set REFITTER_UPDATE_SNAPSHOTS=1 to rewrite the snapshots instead of asserting.
/// See docs/nswag-removal-plan.md.
/// </summary>
[Category("Parity")]
public class GoldenOutputTests
{
    private const string UpdateSnapshotsVariable = "REFITTER_UPDATE_SNAPSHOTS";
    private const long LargeSpecThreshold = 500 * 1024;

    private static readonly Regex GeneratedCodeVersion = new(
        """GeneratedCode\("(?<tool>[^"]+)", "[^"]+"\)""",
        RegexOptions.Compiled);

    // Operations without a responses object fail to load today (also skipped by test/smoke-tests.ps1)
    private static readonly HashSet<string> UnsupportedSpecs =
    [
        "v3.1/non-oauth-scopes.json",
        "v3.1/non-oauth-scopes.yaml",
    ];

    public static IEnumerable<Func<ParityCase>> Cases()
    {
        var specsFolder = Path.Combine(RepositoryPaths.Root, "test", "OpenAPI");
        var specs = Directory
            .EnumerateFiles(specsFolder, "*.*", SearchOption.AllDirectories)
            .Where(p => p.EndsWith(".json", StringComparison.OrdinalIgnoreCase)
                        || p.EndsWith(".yaml", StringComparison.OrdinalIgnoreCase))
            .Select(p => Path.GetRelativePath(specsFolder, p).Replace('\\', '/'))
            .Where(p => !UnsupportedSpecs.Contains(p))
            .OrderBy(p => p, StringComparer.Ordinal);

        foreach (var spec in specs)
        {
            // Large specs produce megabytes per variant, so they only snapshot the contract baselines
            var variants = new FileInfo(Path.Combine(specsFolder, spec)).Length > LargeSpecThreshold
                ? ParityVariants.LargeSpecVariants
                : ParityVariants.All.Keys;

            foreach (var variant in variants)
            {
                yield return () => new ParityCase(spec, variant);
            }
        }
    }

    [Test]
    [MethodDataSource(nameof(Cases))]
    public async Task Generated_Code_Matches_Snapshot(ParityCase parityCase)
    {
        var actual = Normalize(await GenerateAsync(parityCase));
        var snapshotPath = parityCase.SnapshotPath;

        if (Environment.GetEnvironmentVariable(UpdateSnapshotsVariable) == "1")
        {
            await UpdateSnapshotAsync(parityCase, actual);
            return;
        }

        // Variants whose output equals the Default variant have no snapshot file of their own
        var expectedPath = File.Exists(snapshotPath) ? snapshotPath : parityCase.DefaultSnapshotPath;
        File.Exists(expectedPath).Should().BeTrue(
            $"snapshot {expectedPath} should exist; run with {UpdateSnapshotsVariable}=1 to create it");

        var expected = Normalize(await File.ReadAllTextAsync(expectedPath));
        if (actual != expected)
        {
            throw new InvalidOperationException(DescribeFirstDifference(parityCase, expectedPath, expected, actual));
        }
    }

    private static async Task UpdateSnapshotAsync(ParityCase parityCase, string actual)
    {
        var snapshotPath = parityCase.SnapshotPath;
        if (!parityCase.IsDefault)
        {
            // Default snapshots are written by their own test case, so generate the baseline here
            var defaultOutput = Normalize(await GenerateAsync(parityCase with { Variant = ParityCase.DefaultVariant }));
            if (actual == defaultOutput)
            {
                if (File.Exists(snapshotPath))
                {
                    File.Delete(snapshotPath);
                }

                return;
            }
        }

        Directory.CreateDirectory(Path.GetDirectoryName(snapshotPath)!);
        await File.WriteAllTextAsync(snapshotPath, actual);
    }

    internal static async Task<string> GenerateAsync(ParityCase parityCase)
    {
        var settings = new RefitGeneratorSettings
        {
            OpenApiPath = Path.Combine(RepositoryPaths.Root, "test", "OpenAPI", parityCase.Spec),
            CodeGeneratorSettings = new CodeGeneratorSettings(),
        };
        ParityVariants.All[parityCase.Variant](settings);

        var generator = await RefitGenerator.CreateAsync(settings);
        if (!settings.GenerateMultipleFiles)
        {
            return generator.Generate();
        }

        var output = new StringBuilder();
        foreach (var file in generator.GenerateMultipleFiles().Files)
        {
            output.AppendLine($"// ===== {file.TypeName} =====");
            output.AppendLine(file.Content);
        }

        return output.ToString();
    }

    internal static string Normalize(string code) =>
        GeneratedCodeVersion
            .Replace(code.Replace("\r\n", "\n"), """GeneratedCode("${tool}", "<version>")""")
            .TrimEnd() + "\n";

    private static string DescribeFirstDifference(
        ParityCase parityCase,
        string expectedPath,
        string expected,
        string actual)
    {
        var expectedLines = expected.Split('\n');
        var actualLines = actual.Split('\n');
        var index = 0;
        while (index < expectedLines.Length
               && index < actualLines.Length
               && expectedLines[index] == actualLines[index])
        {
            index++;
        }

        return $"""
                Generated code for {parityCase} differs from {expectedPath} at line {index + 1}.
                Expected: {(index < expectedLines.Length ? expectedLines[index] : "<end of file>")}
                Actual:   {(index < actualLines.Length ? actualLines[index] : "<end of file>")}
                Run with {UpdateSnapshotsVariable}=1 to accept the new output.
                """;
    }
}

/// <summary>One spec (relative to test/OpenAPI) combined with one <see cref="ParityVariants"/> key.</summary>
public sealed record ParityCase(string Spec, string Variant)
{
    public const string DefaultVariant = "Default";

    public bool IsDefault => Variant == DefaultVariant;

    public string SnapshotPath => GetSnapshotPath(Variant);

    public string DefaultSnapshotPath => GetSnapshotPath(DefaultVariant);

    private string GetSnapshotPath(string variant) =>
        Path.Combine(
            RepositoryPaths.Root,
            "src",
            "Refitter.Tests",
            "Parity",
            "Snapshots",
            Spec.Replace('/', Path.DirectorySeparatorChar),
            $"{variant}.cs.snap");

    public override string ToString() => $"{Spec} [{Variant}]";
}

internal static class RepositoryPaths
{
    public static string Root { get; } = FindRoot();

    private static string FindRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "src", "Refitter.slnx")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName
               ?? throw new InvalidOperationException("Could not locate the repository root (src/Refitter.slnx)");
    }
}
