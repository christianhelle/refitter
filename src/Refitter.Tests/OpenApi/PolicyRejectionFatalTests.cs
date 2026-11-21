using AwesomeAssertions;
using Refitter.Core;
using Refitter.Tests.TestUtilities;
using OpenApiDocument = NSwag.OpenApiDocument;

namespace Refitter.Tests.OpenApi;

/// <summary>
/// A policy rejection raised while parsing must be fatal. Falling back to another loading strategy would let
/// the OasReader based strategy resolve, by itself, the reference the policy just refused.
/// </summary>
[TestClass]
public class PolicyRejectionFatalTests
{
    // Port 1 refuses connections, so a fetch that was not blocked would surface as an HTTP failure
    private const string Spec =
        "{\"openapi\":\"3.0.0\",\"info\":{\"title\":\"t\",\"version\":\"1\"},\"paths\":{}," +
        "\"components\":{\"schemas\":{\"Pet\":{\"$ref\":\"http://127.0.0.1:1/s.json\"}}}}";

    private static readonly ReferencePolicy Blocking = new(allowRemoteReferences: false);

    private static async Task<string> WriteSpec()
    {
        var folder = TestDirectory.CreateFolder();
        var path = Path.Combine(folder, "spec.json");
        await File.WriteAllTextAsync(path, Spec);
        return path;
    }

    [TestMethod]
    public async Task FileDocumentStrategy_Rethrows_Policy_Rejection()
    {
        var path = await WriteSpec();

        var act = () => new FileDocumentStrategy(Blocking).TryLoadAsync(path);

        await act.Should().ThrowAsync<ReferenceResolutionException>().WithMessage("*127.0.0.1:1*");
    }

    [TestMethod]
    public async Task OpenApiReaderDocumentStrategy_Rethrows_Policy_Rejection()
    {
        // The remote reference sits in a referenced file, so the strategy parses it after the round trip
        var folder = TestDirectory.CreateFolder();
        var path = Path.Combine(folder, "spec.json");
        await File.WriteAllTextAsync(
            path,
            "{\"openapi\":\"3.0.0\",\"info\":{\"title\":\"t\",\"version\":\"1\"},\"paths\":{\"/p\":{\"get\":{" +
            "\"operationId\":\"g\",\"responses\":{\"200\":{\"description\":\"ok\",\"content\":{\"application/json\":{" +
            "\"schema\":{\"$ref\":\"./child.json\"}}}}}}}}}");
        await File.WriteAllTextAsync(
            Path.Combine(folder, "child.json"),
            "{\"type\":\"object\",\"properties\":{\"x\":{\"$ref\":\"http://127.0.0.1:1/s.json\"}}}");

        var act = () => new OpenApiReaderDocumentStrategy(Blocking).TryLoadAsync(path);

        await act.Should().ThrowAsync<ReferenceResolutionException>().WithMessage("*127.0.0.1:1*");
    }

    [TestMethod]
    public async Task DocumentLoader_Surfaces_Policy_Rejection_As_ReferenceResolutionException()
    {
        var path = await WriteSpec();

        var act = () => DocumentLoader.Create(Blocking).LoadAsync(path);

        // Not the generic "All 3 strategies failed" InvalidOperationException
        await act.Should().ThrowAsync<ReferenceResolutionException>().WithMessage("*127.0.0.1:1*");
    }

    [TestMethod]
    public async Task DocumentLoader_Does_Not_Fall_Back_After_Policy_Rejection()
    {
        var fallback = new CountingStrategy();
        var loader = new DocumentLoader(new IDocumentLoadingStrategy[]
        {
            new ThrowingStrategy(new InvalidOperationException(
                "wrapper",
                new ReferenceResolutionException("blocked"))),
            fallback
        });

        var act = () => loader.LoadAsync("spec.json");

        var thrown = await act.Should().ThrowAsync<ReferenceResolutionException>();
        thrown.Which.Message.Should().Be("blocked");
        fallback.Calls.Should().Be(0);
    }

    [TestMethod]
    public async Task DocumentLoader_Still_Falls_Back_After_Ordinary_Failure()
    {
        var fallback = new CountingStrategy();
        var loader = new DocumentLoader(new IDocumentLoadingStrategy[]
        {
            new ThrowingStrategy(new InvalidOperationException("ordinary")),
            fallback
        });

        var act = () => loader.LoadAsync("spec.json");

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*All 2 strategies failed*");
        fallback.Calls.Should().Be(1);
    }

    private sealed class ThrowingStrategy(Exception exception) : IDocumentLoadingStrategy
    {
        public Task<OpenApiDocument?> TryLoadAsync(string path, CancellationToken cancellationToken = default) =>
            throw exception;
    }

    private sealed class CountingStrategy : IDocumentLoadingStrategy
    {
        public int Calls { get; private set; }

        public Task<OpenApiDocument?> TryLoadAsync(string path, CancellationToken cancellationToken = default)
        {
            Calls++;
            return Task.FromResult<OpenApiDocument?>(null);
        }
    }
}
