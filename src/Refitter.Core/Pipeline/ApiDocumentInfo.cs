
namespace Refitter.Core;

/// <summary>
/// Describes the OpenAPI document that code is generated from, after filtering and schema trimming.
/// </summary>
public sealed class ApiDocumentInfo
{
    internal ApiDocumentInfo(ApiDocument document)
    {
        Title = document.Info?.Title;
        Version = document.Info?.Version;
        Paths = document.Paths.Keys.ToList();
        SchemaNames = document.Definitions.Keys.ToList();
    }

    /// <summary>Gets the title of the API.</summary>
    public string? Title { get; }

    /// <summary>Gets the version of the API.</summary>
    public string? Version { get; }

    /// <summary>Gets the paths that code is generated for.</summary>
    public IReadOnlyList<string> Paths { get; }

    /// <summary>Gets the names of the schemas that contracts are generated for.</summary>
    public IReadOnlyList<string> SchemaNames { get; }
}
