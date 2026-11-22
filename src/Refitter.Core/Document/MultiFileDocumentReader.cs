using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;
using OpenApiDocument = NSwag.OpenApiDocument;

namespace Refitter.Core;

/// <summary>
/// Reads a multi-file document with OasReader, which merges external references that NSwag cannot follow by
/// itself, and hands the merged document back to NSwag.
/// </summary>
internal static class MultiFileDocumentReader
{
    /// <summary>
    /// Returns <c>null</c> when the document has no external references or the reader cannot read it. A
    /// rejection is fatal and is thrown. The merged document is parsed with the referenced files of
    /// <paramref name="set"/>.
    /// </summary>
    public static async Task<OpenApiDocument?> TryReadAsync(
        string path,
        DocumentSet set,
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

            return await SerializeRoundTripAsync(readResult, path, specificationVersion, set, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            if (ReferenceResolutionException.FindIn(ex) is { } rejection)
                throw rejection;

            return null;
        }
    }

    private static async Task<OpenApiDocument> SerializeRoundTripAsync(
        Result readResult,
        string path,
        OpenApiSpecVersion specificationVersion,
        DocumentSet set,
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

            return await OpenApiDocumentParser
                .ParseAsync(OpenApiDocumentReader.WithRootContent(set, yaml), cancellationToken)
                .ConfigureAwait(false);
        }

        var json = await document
            .SerializeAsJsonAsync(specificationVersion, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return await OpenApiDocumentParser
            .ParseAsync(OpenApiDocumentReader.WithRootContent(set, json), cancellationToken)
            .ConfigureAwait(false);
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
