using AwesomeAssertions;
using Refitter.Core;

namespace Refitter.Tests;

[TestClass]
public class RunnerDiagnosticKindTests
{
    [TestMethod]
    public async Task RunAsync_Reports_All_Paths_Filtered_Kind()
    {
        var directory = Path.Combine(AppContext.BaseDirectory, nameof(RunnerDiagnosticKindTests), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var spec = Path.Combine(directory, "spec.json");
            await File.WriteAllTextAsync(
                spec,
                """
                {
                  "openapi": "3.0.0",
                  "info": { "title": "Test API", "version": "1.0.0" },
                  "paths": {
                    "/pets": { "get": { "operationId": "GetPets", "responses": { "200": { "description": "ok" } } } }
                  }
                }
                """);
            var settings = new RefitGeneratorSettings
            {
                OpenApiPath = spec,
                IncludePathMatches = ["^/nothing$"],
            };

            var result = await new RefitterRunner().RunAsync(settings, writer: null, validator: null);

            result.Diagnostics.Should().ContainSingle(d => d.Kind == RunnerDiagnosticKind.AllPathsFiltered);
        }
        finally
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}
