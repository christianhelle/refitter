using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Refit;

namespace Refitter.Tests.TestUtilities;

/// <summary>
/// Compiles generated code in memory and calls one of its methods through Refit's reflection request builder,
/// capturing the request Refit sends. Verifies that Refit accepts the generated interface at runtime, which
/// compiling the code alone does not.
/// </summary>
public static class RefitRequestCapture
{
    /// <summary>
    /// Calls a method of a generated Refit interface and returns the request it sends.
    /// </summary>
    /// <param name="code">The generated code containing the interface.</param>
    /// <param name="interfaceName">The name of the interface.</param>
    /// <param name="methodName">The name of the method to call.</param>
    /// <param name="arguments">The arguments to call the method with.</param>
    /// <returns>The request sent by Refit.</returns>
    public static async Task<HttpRequestMessage> SendAsync(
        string code,
        string interfaceName,
        string methodName,
        params object?[] arguments)
    {
        var interfaceType = Compile(code)
            .GetTypes()
            .Single(type => type.IsInterface && type.Name == interfaceName);

        var handler = new CapturingHandler();
        using var client = new HttpClient(handler) { BaseAddress = new Uri("https://localhost") };

        var restMethod = RequestBuilder.ForType(interfaceType).BuildRestResultFuncForMethod(methodName);
        await (Task)restMethod(client, arguments!)!;

        return handler.Request ?? throw new InvalidOperationException("Refit did not send a request.");
    }

    private static System.Reflection.Assembly Compile(string code)
    {
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Append(typeof(RequestBuilder).Assembly.Location)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(path => MetadataReference.CreateFromFile(path));

        var compilation = CSharpCompilation.Create(
            $"Generated{Guid.NewGuid():N}",
            [CSharpSyntaxTree.ParseText(code)],
            references,
            new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable));

        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        if (!result.Success)
        {
            var errors = result.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
            throw new InvalidOperationException(
                $"Generated code does not compile:{Environment.NewLine}{string.Join(Environment.NewLine, errors)}");
        }

        return System.Reflection.Assembly.Load(stream.ToArray());
    }

    private sealed class CapturingHandler : HttpMessageHandler
    {
        public HttpRequestMessage? Request { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Request = request;
            return Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK));
        }
    }
}
