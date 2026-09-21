using AwesomeAssertions;
using Refitter.Core;
using Refitter.Core.Validation;

namespace Refitter.Tests;

public class GuardClauseTests
{
    /// <summary>
    /// Loopback port 1: nothing listens there, so no DNS lookup and no outbound
    /// connection is made even if the early cancellation check ever regresses.
    /// </summary>
    private const string UnreachableRemoteDocument = "http://127.0.0.1:1/openapi.json";

    [Test]
    public async Task OpenApiValidator_Propagates_Cancellation_For_Remote_Document()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var act = async () => await OpenApiValidator.Validate(
            UnreachableRemoteDocument,
            allowRemoteReferences: true,
            cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Test]
    public async Task OpenApiDocumentFactory_Propagates_Cancellation_For_Remote_Document()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var act = async () => await OpenApiDocumentFactory.CreateAsync(
            UnreachableRemoteDocument,
            allowRemoteReferences: true,
            cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Test]
    public async Task RefitGenerator_CreateAsync_Throws_On_Null_Settings()
    {
        var act = async () => await RefitGenerator.CreateAsync(null!);

        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
