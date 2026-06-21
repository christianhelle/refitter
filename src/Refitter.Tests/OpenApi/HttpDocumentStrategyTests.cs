using System.Net;
using FluentAssertions;
using Refitter.Core;

namespace Refitter.Tests.OpenApi;


[Category("Unit")]
public class HttpDocumentStrategyTests
{
    [Test]
    public async Task Returns_Null_For_File_Paths()
    {
        var strategy = new HttpDocumentStrategy();
        var result = await strategy.TryLoadAsync("/local/path/spec.json");

        result.Should().BeNull();
    }

    [Test]
    public async Task Returns_Null_When_Http_Fails()
    {
        var handler = new MockHttpMessageHandler(
            (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound)));
        var httpClient = new HttpClient(handler);
        var strategy = new HttpDocumentStrategy(httpClient);

        var result = await strategy.TryLoadAsync("https://example.com/spec.json");

        result.Should().BeNull();
    }

    [Test]
    public async Task Loads_Json_Spec_From_Http()
    {
        var spec = @"{
  ""openapi"": ""3.0.0"",
  ""info"": { ""title"": ""Test API"", ""version"": ""1.0.0"" },
  ""paths"": {}
}";
        var handler = new MockHttpMessageHandler(
            (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(spec)
            }));
        var httpClient = new HttpClient(handler);
        var strategy = new HttpDocumentStrategy(httpClient);

        var result = await strategy.TryLoadAsync("https://example.com/spec.json");

        result.Should().NotBeNull();
        result!.Info.Title.Should().Be("Test API");
    }

    [Test]
    public async Task Loads_Yaml_Spec_From_Http()
    {
        var spec = @"openapi: 3.0.0
info:
  title: YAML Test API
  version: 1.0.0
paths: {}";
        var handler = new MockHttpMessageHandler(
            (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(spec)
            }));
        var httpClient = new HttpClient(handler);
        var strategy = new HttpDocumentStrategy(httpClient);

        var result = await strategy.TryLoadAsync("https://example.com/spec.yaml");

        result.Should().NotBeNull();
        result!.Info.Title.Should().Be("YAML Test API");
    }

    [Test]
    public async Task Returns_Null_For_Invalid_Http_Content()
    {
        var handler = new MockHttpMessageHandler(
            (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("not valid openapi content")
            }));
        var httpClient = new HttpClient(handler);
        var strategy = new HttpDocumentStrategy(httpClient);

        var result = await strategy.TryLoadAsync("https://example.com/spec.json");

        result.Should().BeNull();
    }

    private sealed class MockHttpMessageHandler : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler;

        public MockHttpMessageHandler(
            Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> handler)
        {
            this.handler = handler;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            return handler(request, cancellationToken);
        }
    }
}
