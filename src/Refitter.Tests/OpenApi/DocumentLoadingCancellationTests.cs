using AwesomeAssertions;
using NSwag;
using Refitter.Core;
using Refitter.Tests.Resources;

namespace Refitter.Tests.OpenApi;

public class DocumentLoadingCancellationTests
{
    [Test]
    public async Task FileDocumentStrategy_Propagates_Cancellation()
    {
        var swaggerFile = await TestFile.CreateSwaggerFile(
            EmbeddedResources.GetSwaggerPetstore(SampleOpenSpecifications.SwaggerPetstoreJsonV3),
            "cancel-file.json");
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var strategy = new FileDocumentStrategy();

        var act = async () => await strategy.TryLoadAsync(swaggerFile, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Test]
    public async Task HttpDocumentStrategy_Propagates_Cancellation()
    {
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();
        var strategy = new HttpDocumentStrategy(new HttpClient());

        var act = async () => await strategy.TryLoadAsync("https://example.com/petstore.json", cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Test]
    public async Task DocumentLoader_Propagates_Cancellation_From_Strategy()
    {
        var loader = new DocumentLoader(new IDocumentLoadingStrategy[] { new CancellingStrategy() });

        var act = async () => await loader.LoadAsync("petstore.json");

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Test]
    public void DocumentLoader_Throws_On_Null_Strategies()
    {
        var act = () => new DocumentLoader(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    private sealed class CancellingStrategy : IDocumentLoadingStrategy
    {
        public Task<OpenApiDocument?> TryLoadAsync(string path, CancellationToken cancellationToken = default)
            => throw new OperationCanceledException();
    }
}
