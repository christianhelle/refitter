using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using AwesomeAssertions;
using Refitter.Core;
using TUnit.Core;

namespace Refitter.Tests.SettingsFile;


public class JsonSchemaConsistencyTests
{
    [Test]
    public async Task JsonSchema_Should_Match_Serializable_Settings()
    {
        string schemaPath = FindRepoFile("docs", "json-schema.json");
        using JsonDocument document = JsonDocument.Parse(await File.ReadAllTextAsync(schemaPath));
        List<string> propertyNames = document.RootElement
            .GetProperty("properties")
            .EnumerateObject()
            .Select(property => property.Name)
            .ToList();

        List<string> settingNames = typeof(RefitGeneratorSettings)
            .GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(property => property.GetCustomAttribute<JsonIgnoreAttribute>()?.Condition != JsonIgnoreCondition.Always)
            .Select(property => property.GetCustomAttribute<JsonPropertyNameAttribute>()?.Name
                ?? JsonNamingPolicy.CamelCase.ConvertName(property.Name))
            .ToList();

        propertyNames.Should().Contain("returnIAsyncEnumerable");
        propertyNames.Should().BeEquivalentTo(
            settingNames,
            "docs/json-schema.json should describe every serializable setting and no unsupported properties");
    }

    private static string FindRepoFile(params string[] relativeSegments)
    {
        DirectoryInfo? directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "global.json")))
            {
                string candidate = directory.FullName;
                foreach (string segment in relativeSegments)
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
