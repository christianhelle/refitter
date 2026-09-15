using System.Text.Json;
using FluentAssertions;
using Refitter.Core;
using TUnit.Core;

namespace Refitter.Tests.SettingsFile;


public class JsonSchemaConsistencyTests
{
    [Test]
    public async Task JsonSchema_Should_Only_Describe_Known_Settings()
    {
        var schemaPath = FindRepoFile("docs", "json-schema.json");
        using var document = JsonDocument.Parse(await File.ReadAllTextAsync(schemaPath));
        var propertyNames = document.RootElement
            .GetProperty("properties")
            .EnumerateObject()
            .Select(property => property.Name)
            .ToList();

        var settingNames = typeof(RefitGeneratorSettings)
            .GetProperties()
            .Select(property => property.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        propertyNames.Should().Contain("returnIAsyncEnumerable");
        foreach (var propertyName in propertyNames)
        {
            settingNames.Should().Contain(
                propertyName,
                $"docs/json-schema.json documents '{propertyName}' which is not a setting");
        }
    }

    private static string FindRepoFile(params string[] relativeSegments)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "global.json")))
            {
                var candidate = directory.FullName;
                foreach (var segment in relativeSegments)
                {
                    candidate = Path.Combine(candidate, segment);
                }

                File.Exists(candidate).Should().BeTrue($"expected {candidate} to exist");
                return candidate;
            }

            directory = directory.Parent;
        }

        throw new InvalidOperationException("Unable to locate the repository root (global.json not found)");
    }
}
