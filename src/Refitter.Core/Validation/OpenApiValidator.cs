using Refitter.Core.Validation.Reading;

namespace Refitter.Core.Validation;

/// <summary>
/// Validates an OpenAPI specification file and collects statistics about its contents.
/// </summary>
/// <remarks>
/// The diagnostics and statistics reproduce those of Microsoft.OpenApi (MIT license), which Refitter used to
/// validate with: the same problems are reported, at the same JSON pointers and with the same messages.
/// </remarks>
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
        cancellationToken.ThrowIfCancellationRequested();

        byte[] content;
        Uri? baseUrl = null;
        if (PathUtilities.IsHttp(openApiFile))
        {
            string text;
            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
                using var httpResponse = await client.SendAsync(
                    new HttpRequestMessage(HttpMethod.Get, openApiFile),
                    cancellationToken).ConfigureAwait(false);
                text = await httpResponse.Content
                    .ReadAsStringWithCancellationAsync(cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                throw new InvalidOperationException($"Failed to download OpenAPI document from '{openApiFile}'.", ex);
            }

            await ReferenceGuard.ValidateAsync(openApiFile, text, allowRemoteReferences, cancellationToken)
                .ConfigureAwait(false);

            content = System.Text.Encoding.UTF8.GetBytes(text);
        }
        else
        {
            await ReferenceGuard.ValidateAsync(openApiFile, allowRemoteReferences, cancellationToken)
                .ConfigureAwait(false);

            content = ReadFile(openApiFile);

            // Microsoft.OpenApi reads local files relative to their folder
            baseUrl = new Uri($"file://{new FileInfo(openApiFile).DirectoryName}{Path.DirectorySeparatorChar}");
        }

        var (document, diagnostics) = SpecDocumentReader.Read(content, baseUrl);

        // Microsoft.OpenApi resolves references against the components the document was read with
        var registered = document?.Components?.Copy();
        if (PathUtilities.IsHttp(openApiFile))
        {
            // Microsoft.OpenApi only applied its validation rules to documents downloaded from a URL
            SpecRuleValidator.Validate(document, diagnostics);
        }
        else if (document != null)
        {
            ExternalReferenceMerger.Merge(document, openApiFile, registered);
        }

        var statistics = SpecStatistics.Count(document);
        AttributeStringValidator.Validate(document, registered, diagnostics);
        return new OpenApiValidationResult(diagnostics, statistics);
    }

    private static byte[] ReadFile(string openApiFile)
    {
        try
        {
            return File.ReadAllBytes(openApiFile);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException
                                       or System.Security.SecurityException)
        {
            throw new InvalidOperationException("Could not open the file at " + openApiFile, ex);
        }
    }
}
