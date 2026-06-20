using System.Diagnostics.CodeAnalysis;
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

        if (PathUtilities.IsYaml(path))
        {
            var yaml = await document
                .SerializeAsYamlAsync(specificationVersion, cancellationToken: cancellationToken)
                .ConfigureAwait(false);

            return await OpenApiYamlDocument
                .FromYamlAsync(yaml, cancellationToken)
                .ConfigureAwait(false);
        }

        var json = await document
            .SerializeAsJsonAsync(specificationVersion, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return await OpenApiDocument
            .FromJsonAsync(json, cancellationToken)
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
        catch
        {
            return null;
        }
    }

    [ExcludeFromCodeCoverage]
    private static void PopulateMissingRequiredFields(
        string openApiPath,
        Result readResult)
    {
        var document = readResult.OpenApiDocument;
        if (document.Info is null)
        {
            document.Info = new()
            {
                Title = Path.GetFileNameWithoutExtension(openApiPath),
                Version = readResult.OpenApiDiagnostic.SpecificationVersion.GetDisplayName()
            };
        }
        else
        {
            document.Info.Title ??= Path.GetFileNameWithoutExtension(openApiPath);
            document.Info.Version ??= readResult.OpenApiDiagnostic.SpecificationVersion.GetDisplayName();
        }
    }
}
