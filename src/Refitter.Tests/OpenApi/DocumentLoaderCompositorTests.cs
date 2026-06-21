using FluentAssertions;
using NSwag;
using Refitter.Core;

namespace Refitter.Tests.OpenApi;


[Category("Unit")]
public class DocumentLoaderCompositorTests
{
    [Test]
    public async Task Throws_On_Empty_Path()
    {
        var loader = new DocumentLoader();
        var act = async () => await loader.LoadAsync("");

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Test]
    public async Task Throws_On_Null_Path()
    {
        var loader = new DocumentLoader();
        var act = async () => await loader.LoadAsync(null!);

        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Test]
    public async Task Throws_When_All_Strategies_Fail()
    {
        var strategies = new IDocumentLoadingStrategy[]
        {
            new FailingStrategy(),
            new FailingStrategy()
        };
        var loader = new DocumentLoader(strategies);

        var act = async () => await loader.LoadAsync("test.json");

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Test]
    public async Task Returns_First_Successful_Result()
    {
        var strategies = new IDocumentLoadingStrategy[]
        {
            new NullReturningStrategy(),
            new SuccessStrategy("First Success"),
            new SuccessStrategy("Second Success")
        };
        var loader = new DocumentLoader(strategies);

        var result = await loader.LoadAsync("test.json");

        result.Should().NotBeNull();
        result.Info.Title.Should().Be("First Success");
    }

    [Test]
    public async Task Skips_Strategies_That_Return_Null()
    {
        var strategies = new IDocumentLoadingStrategy[]
        {
            new NullReturningStrategy(),
            new NullReturningStrategy(),
            new SuccessStrategy("Third Time")
        };
        var loader = new DocumentLoader(strategies);

        var result = await loader.LoadAsync("test.json");

        result.Should().NotBeNull();
        result.Info.Title.Should().Be("Third Time");
    }

    [Test]
    public async Task Loads_Valid_File_With_Default_Strategies()
    {
        var spec = @"{
  ""openapi"": ""3.0.0"",
  ""info"": { ""title"": ""Default Strategies Test"", ""version"": ""1.0.0"" },
  ""paths"": {}
}";
        var swaggerFile = await TestFile.CreateSwaggerFile(spec, "default-test.json");
        var loader = new DocumentLoader();

        var result = await loader.LoadAsync(swaggerFile);

        result.Should().NotBeNull();
        result.Info.Title.Should().Be("Default Strategies Test");
    }

    private sealed class NullReturningStrategy : IDocumentLoadingStrategy
    {
        public Task<OpenApiDocument?> TryLoadAsync(string path, CancellationToken cancellationToken = default)
            => Task.FromResult<OpenApiDocument?>(null);
    }

    private sealed class SuccessStrategy : IDocumentLoadingStrategy
    {
        private readonly string title;

        public SuccessStrategy(string title)
        {
            this.title = title;
        }

        public Task<OpenApiDocument?> TryLoadAsync(string path, CancellationToken cancellationToken = default)
        {
            var doc = new OpenApiDocument
            {
                Info = new() { Title = title, Version = "1.0.0" }
            };
            return Task.FromResult<OpenApiDocument?>(doc);
        }
    }

    private sealed class FailingStrategy : IDocumentLoadingStrategy
    {
        public Task<OpenApiDocument?> TryLoadAsync(string path, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("Strategy failed");
    }
}
