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
        // For remote URLs, fetch once and validate before parsing
        if (PathUtilities.IsHttp(openApiFile))
        {
            string content;
            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
                using var httpResponse = await client.SendAsync(
                    new HttpRequestMessage(HttpMethod.Get, openApiFile),
                    cancellationToken).ConfigureAwait(false);
                content = await httpResponse.Content.ReadAsStringAsync().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to download OpenAPI document from '{openApiFile}'.", ex);
            }

            await ReferenceGuard.ValidateAsync(openApiFile, content, allowRemoteReferences, cancellationToken)
                .ConfigureAwait(false);

            // Parse the already-fetched content using Microsoft.OpenApi
            var readerSettings = new OpenApiReaderSettings();
            readerSettings.AddYamlReader();
            using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(content));
            var loadResult = await OpenApiDocument.LoadAsync(stream, settings: readerSettings, cancellationToken: cancellationToken).ConfigureAwait(false);
            var msDocument = loadResult.Document;
            var diagnostic = loadResult.Diagnostic ?? new OpenApiDiagnostic();

            var statsVisitor = new OpenApiStats();
            var walker = new OpenApiWalker(statsVisitor);
            walker.Walk(msDocument);

            return new(diagnostic, statsVisitor);
        }

        // For local files, validate first (reads once), then parse with OpenApiMultiFileReader
        await ReferenceGuard.ValidateAsync(openApiFile, allowRemoteReferences, cancellationToken)
            .ConfigureAwait(false);

        var result = await OpenApiMultiFileReader.Read(
            openApiFile,
            cancellationToken: cancellationToken);

        var stats = new OpenApiStats();
        var openApiWalker = new OpenApiWalker(stats);
        openApiWalker.Walk(result.OpenApiDocument);

        return new(
            result.OpenApiDiagnostic,
            stats);
    }
}
