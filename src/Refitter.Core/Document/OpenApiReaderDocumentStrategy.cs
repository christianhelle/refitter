using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;
using OpenApiDocument = NSwag.OpenApiDocument;

namespace Refitter.Core;

internal sealed class OpenApiReaderDocumentStrategy : IFailureReportingStrategy
{
    private readonly ReferencePolicy policy;

    public OpenApiReaderDocumentStrategy()
        : this(ReferencePolicy.Unrestricted)
    {
    }

    public OpenApiReaderDocumentStrategy(ReferencePolicy policy)
    {
        this.policy = policy;
    }

    public async Task<OpenApiDocument?> TryLoadAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        try
        {
            return await LoadAsync(path, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException
                                       and not TaskCanceledException)
        {
            if (ReferenceResolutionException.FindIn(ex) is { } rejection)
                throw rejection;

            return null;
        }
    }

    public async Task<OpenApiDocument?> LoadAsync(
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
            if (ReferenceResolutionException.FindIn(ex) is { } rejection)
                throw rejection;

            // With no fallback, the reader's own failure is the one to report
            var fallback = await FallbackToNSwagAsync(path, cancellationToken)
                .ConfigureAwait(false);

            if (fallback is null)
                throw;

            return fallback;
        }
    }

    private async Task<OpenApiDocument> SerializeRoundTripAsync(
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

            return await OpenApiDocumentParser
                .ParseAsync(yaml, path, isYaml: true, policy, cancellationToken)
                .ConfigureAwait(false);
        }

        var json = await document
            .SerializeAsJsonAsync(specificationVersion, cancellationToken: cancellationToken)
            .ConfigureAwait(false);

        return await OpenApiDocumentParser
            .ParseAsync(json, path, isYaml: false, policy, cancellationToken)
            .ConfigureAwait(false);
    }

    // When the reader fails, NSwag reads the document itself. Its error is the one worth reporting, because the
    // reader's covers only a document with external references.
    private async Task<OpenApiDocument?> FallbackToNSwagAsync(
        string path,
        CancellationToken cancellationToken)
    {
        if (PathUtilities.IsHttp(path))
            return null;

        return await OpenApiDocumentParser.FromFileAsync(path, policy, cancellationToken)
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
