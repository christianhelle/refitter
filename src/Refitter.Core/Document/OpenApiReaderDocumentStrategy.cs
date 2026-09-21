using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;
using NSwag;
using OpenApiDocument = NSwag.OpenApiDocument;

namespace Refitter.Core;

internal sealed class OpenApiReaderDocumentStrategy : IDocumentLoadingStrategy
{
    public async Task<OpenApiDocument?> TryLoadAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            var readResult = await OpenApiMultiFileReader
                .Read(path, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            if (!readResult.ContainedExternalReferences)
                return null;

            var specificationVersion = readResult.OpenApiDiagnostic.SpecificationVersion;
            PopulateMissingRequiredFields(path, readResult);

            return await SerializeRoundTripAsync(readResult, path, specificationVersion, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException
                                       and not TaskCanceledException)
        {
            return await FallbackToNSwagAsync(path, cancellationToken)
                .ConfigureAwait(false);
        }
    }

    private static async Task<OpenApiDocument> SerializeRoundTripAsync(
        Result readResult,
        string path,
        OpenApiSpecVersion specificationVersion,
        CancellationToken cancellationToken)
    {
        var document = readResult.OpenApiDocument;

        // Microsoft.OpenApi does not inline external components when it reads a
        // multi-file document - the serialized output still carries the original
        // relative "$ref". NSwag can only follow those refs when it is told which
        // document they are relative to, so the path must be passed along.
        if (PathUtilities.IsYaml(path))
        {
            var yaml = await document
                .SerializeAsYamlAsync(specificationVersion, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return await OpenApiYamlDocument
                .FromYamlAsync(yaml, path, cancellationToken)
                .ConfigureAwait(false);
        }

        var json = await document
            .SerializeAsJsonAsync(specificationVersion, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return await OpenApiDocument
            .FromJsonAsync(json, path, cancellationToken)
            .ConfigureAwait(false);
    }

    private static async Task<OpenApiDocument?> FallbackToNSwagAsync(
        string path,
        CancellationToken cancellationToken)
    {
        if (PathUtilities.IsHttp(path))
            return null;

        try
        {
            return PathUtilities.IsYaml(path)
                ? await OpenApiYamlDocument.FromFileAsync(path, cancellationToken)
                    .ConfigureAwait(false)
                : await OpenApiDocument.FromFileAsync(path, cancellationToken)
                    .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return null;
        }
    }

    private static void PopulateMissingRequiredFields(
        string openApiPath,
        Result readResult)
    {
        // The reader always materializes Info, so the ??= below is only a guard.
        var info = readResult.OpenApiDocument.Info ??= new();
        info.Title ??= Path.GetFileNameWithoutExtension(openApiPath);
        info.Version ??= readResult.OpenApiDiagnostic.SpecificationVersion.GetDisplayName();
    }
}
