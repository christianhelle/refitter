using NSwag;
using OpenApiDocument = NSwag.OpenApiDocument;

namespace Refitter.Core;

internal sealed class FileDocumentStrategy : IDocumentLoadingStrategy
{
    public async Task<OpenApiDocument?> TryLoadAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        if (PathUtilities.IsHttp(path))
            return null;

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            string content;
            using (var reader = new StreamReader(path))
            {
                content = NumericBoundsSanitizer.Sanitize(await reader.ReadToEndAsync().ConfigureAwait(false));
            }

            return PathUtilities.IsYaml(path)
                ? await OpenApiYamlDocument.FromYamlAsync(content, path, cancellationToken).ConfigureAwait(false)
                : await OpenApiDocument.FromJsonAsync(content, path, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            if (ex is OperationCanceledException or TaskCanceledException)
                throw;

            return null;
        }
    }
}
