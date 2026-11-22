using System.Text;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;

namespace Refitter.Core.Validation;

/// <summary>
/// Validates an OpenAPI specification file and collects statistics about its contents.
/// </summary>
public static class OpenApiValidator
{
    /// <summary>
    /// Validates an OpenAPI specification file and returns validation diagnostics and statistics.
    /// </summary>
    /// <param name="openApiFile">The path to the OpenAPI specification file.</param>
    /// <param name="allowRemoteReferences">When false, remote and out-of-tree <c>$ref</c> references are rejected.</param>
    /// <param name="cancellationToken">A cancellation token to cancel the operation.</param>
    /// <returns>A <see cref="OpenApiValidationResult"/> containing diagnostics and element counts.</returns>
    public static async Task<OpenApiValidationResult> Validate(
        string openApiFile,
        bool allowRemoteReferences = false,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        DocumentSet set;

        // For remote URLs, fetch once and validate before parsing
        if (PathUtilities.IsHttp(openApiFile))
        {
            string content;
            try
            {
                content = await RemoteDocumentDownloader
                    .DownloadAsync(openApiFile, cancellationToken: cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                throw new InvalidOperationException($"Failed to download OpenAPI document from '{openApiFile}': {ex.Message}", ex);
            }

            set = await OpenApiDocumentReader.ReadAsync(openApiFile, content, allowRemoteReferences, cancellationToken)
                .ConfigureAwait(false);
        }
        else
        {
            // Check the references first
            set = await OpenApiDocumentReader.ReadAsync(openApiFile, allowRemoteReferences, cancellationToken)
                .ConfigureAwait(false);
        }

        return await ValidateAsync(set, cancellationToken).ConfigureAwait(false);
    }

    /// <summary>
    /// Validates a document set. The referenced files are served from the set, so the files that are
    /// validated are the files that were read and checked.
    /// </summary>
    internal static async Task<OpenApiValidationResult> ValidateAsync(
        DocumentSet set,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var isLocal = set.RootDocumentPath is not null;

        // The diagnostics are those of the OpenAPI document alone. A local document is checked against no
        // rules, as the referenced files are not part of it. A remote one is checked against the default rules
        var diagnosticsSettings = new OpenApiReaderSettings();
        if (isLocal)
            diagnosticsSettings.RuleSet = ValidationRuleSet.GetEmptyRuleSet();

        var diagnosticsResult = await LoadAsync(set.RootJson, diagnosticsSettings, cancellationToken)
            .ConfigureAwait(false);
        var diagnostic = diagnosticsResult.Diagnostic ?? new OpenApiDiagnostic();

        var document = diagnosticsResult.Document;
        if (isLocal)
        {
            // A second reading follows the external references, so that their components are available
            var documentSettings = new OpenApiReaderSettings
            {
                LoadExternalRefs = true,
                BaseUrl = new Uri(set.RootDocumentPath!),
                CustomExternalLoader = new DocumentSetStreamLoader(set),
            };
            document = (await LoadAsync(set.RootJson, documentSettings, cancellationToken).ConfigureAwait(false)).Document;
        }

        var stats = new OpenApiStats();
        var walker = new OpenApiWalker(stats);
        IDictionary<string, IOpenApiSecurityScheme>? externalSecuritySchemes = null;
        if (document is not null)
        {
            walker.Walk(document);

            if (isLocal)
            {
                var external = CollectExternalComponents(document);
                var components = external.Snapshot();
                walker.Walk(new OpenApiDocument { Components = components });
                externalSecuritySchemes = components.SecuritySchemes;
            }
        }

        AttributeStringValidator.Validate(document, diagnostic, externalSecuritySchemes);

        return new(diagnostic, stats);
    }

    // Referenced components can reference more components, so collecting repeats until nothing new is found
    private static ExternalComponentCollector CollectExternalComponents(OpenApiDocument document)
    {
        var collector = new ExternalComponentCollector();
        var walker = new OpenApiWalker(collector);
        walker.Walk(document);

        var count = -1;
        while (count != collector.Count)
        {
            count = collector.Count;
            walker.Walk(new OpenApiDocument { Components = collector.Snapshot() });
        }

        return collector;
    }

    private static Task<ReadResult> LoadAsync(
        string json,
        OpenApiReaderSettings settings,
        CancellationToken cancellationToken)
    {
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        return OpenApiDocument.LoadAsync(stream, "json", settings, cancellationToken);
    }
}
