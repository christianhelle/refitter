using Refitter.Core.Validation.Reading;

namespace Refitter.Core.Validation;

/// <summary>
/// Validates an OpenAPI specification file without Microsoft.OpenApi. It reproduces the diagnostics and
/// statistics of the Microsoft.OpenApi based <see cref="OpenApiValidator"/>, which it will replace.
/// </summary>
internal static class NativeOpenApiValidator
{
    public static async Task<OpenApiValidationResult> Validate(
        string openApiFile,
        bool allowRemoteReferences = false,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        byte[] content;
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
        }

        var (document, diagnostics) = SpecDocumentReader.Read(content);
        return new OpenApiValidationResult(diagnostics, SpecStatistics.Count(document));
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
