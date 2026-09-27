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
    /// <exception cref="UnsupportedSpecificationVersionException">Thrown when the document declares a specification version that is not supported.</exception>
    public static async Task<OpenApiValidationResult> Validate(
        string openApiFile,
        bool allowRemoteReferences = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await ValidateCoreAsync(openApiFile, allowRemoteReferences, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OpenApiUnsupportedSpecVersionException exception)
        {
            throw new UnsupportedSpecificationVersionException(exception.SpecificationVersion, exception);
        }
    }

    private static async Task<OpenApiValidationResult> ValidateCoreAsync(
        string openApiFile,
        bool allowRemoteReferences,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

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
                content = await httpResponse.Content
                    .ReadAsStringWithCancellationAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
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

            var statistics = new OpenApiStats();
            var walker = new OpenApiWalker(new OpenApiStatsVisitor(statistics));
            walker.Walk(msDocument);

            AttributeStringValidator.Validate(msDocument, diagnostic);

            return new(ToValidationDiagnostics(diagnostic), statistics);
        }

        // For local files, validate first (reads once), then parse with OpenApiMultiFileReader
        await ReferenceGuard.ValidateAsync(openApiFile, allowRemoteReferences, cancellationToken)
            .ConfigureAwait(false);

        var result = await OpenApiMultiFileReader.Read(
            openApiFile,
            cancellationToken: cancellationToken);

        var stats = new OpenApiStats();
        var openApiWalker = new OpenApiWalker(new OpenApiStatsVisitor(stats));
        openApiWalker.Walk(result.OpenApiDocument);

        AttributeStringValidator.Validate(result.OpenApiDocument, result.OpenApiDiagnostic);

        return new(
            ToValidationDiagnostics(result.OpenApiDiagnostic),
            stats);
    }

    private static ValidationDiagnostics ToValidationDiagnostics(OpenApiDiagnostic diagnostic)
    {
        var diagnostics = new ValidationDiagnostics
        {
            SpecificationVersion = diagnostic.SpecificationVersion switch
            {
                OpenApiSpecVersion.OpenApi3_0 => OpenApiSpecificationVersion.OpenApi3_0,
                OpenApiSpecVersion.OpenApi3_1 => OpenApiSpecificationVersion.OpenApi3_1,
                OpenApiSpecVersion.OpenApi3_2 => OpenApiSpecificationVersion.OpenApi3_2,
                _ => OpenApiSpecificationVersion.OpenApi2_0,
            },
        };

        foreach (var error in diagnostic.Errors)
            diagnostics.Errors.Add(new ValidationIssue(error.Pointer, error.Message));

        foreach (var warning in diagnostic.Warnings)
            diagnostics.Warnings.Add(new ValidationIssue(warning.Pointer, warning.Message));

        return diagnostics;
    }
}
