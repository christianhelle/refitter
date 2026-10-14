using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Refit;

namespace Refitter.Tests.TestUtilities;

/// <summary>
/// The Refit implementations a generated interface can be called through.
/// </summary>
public enum RefitClient
{
    /// <summary>Refit's reflection request builder (the Refit.Reflection package).</summary>
    Reflection,

    /// <summary>The client Refit's source generator emits at compile time.</summary>
    Generated,
}

/// <summary>
/// Compiles generated code in memory and calls one of its methods through Refit, capturing the request
/// Refit sends. Verifies that Refit accepts the generated interface at runtime, which compiling the code
/// alone does not.
/// </summary>
public static class RefitRequestCapture
{
    private static readonly CSharpParseOptions ParseOptions = new(LanguageVersion.Latest);
    private static readonly Lazy<ISourceGenerator> StubGenerator = new(LoadStubGenerator);

    /// <summary>
    /// Calls a method of a generated Refit interface and returns the request it sends.
    /// </summary>
    /// <param name="client">The Refit implementation to call the method through.</param>
    /// <param name="code">The generated code containing the interface.</param>
    /// <param name="interfaceName">The name of the interface.</param>
    /// <param name="methodName">The name of the method to call.</param>
    /// <param name="arguments">The arguments to call the method with.</param>
    /// <returns>The request sent by Refit.</returns>
    public static async Task<HttpRequestMessage> SendAsync(
        RefitClient client,
        string code,
        string interfaceName,
        string methodName,
        params object?[] arguments)
    {
        var assembly = Compile(code, generateClient: client == RefitClient.Generated);
        var interfaceType = assembly
            .GetTypes()
            .Single(type => type.IsInterface && type.Name == interfaceName);

        var handler = new CapturingHandler();
        using var httpClient = new HttpClient(handler) { BaseAddress = new Uri("https://localhost") };

        var task = client switch
        {
            RefitClient.Reflection => RequestBuilder.ForType(interfaceType)
                .BuildRestResultFuncForMethod(methodName)(httpClient, arguments!),
            _ => interfaceType.GetMethod(methodName)!.Invoke(
                CreateGeneratedClient(assembly, interfaceType, httpClient),
                BindingFlags.DoNotWrapExceptions,
                null,
                arguments,
                null),
        };
        await (Task)task!;

        return handler.Request ?? throw new InvalidOperationException("Refit did not send a request.");
    }

    private static System.Reflection.Assembly Compile(string code, bool generateClient)
    {
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Append(typeof(RequestBuilder).Assembly.Location)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(path => MetadataReference.CreateFromFile(path));

        Compilation compilation = CSharpCompilation.Create(
            $"Generated{Guid.NewGuid():N}",
            [CSharpSyntaxTree.ParseText(code, ParseOptions)],
            references,
            new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable));

        if (generateClient)
        {
            CSharpGeneratorDriver
                .Create([StubGenerator.Value], parseOptions: ParseOptions)
                .RunGeneratorsAndUpdateCompilation(compilation, out compilation, out _);
        }

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

    // The test project embeds the path, since the generator ships as an analyzer in the Refit package
    private static ISourceGenerator LoadStubGenerator()
    {
        var path = typeof(RefitRequestCapture).Assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .Single(attribute => attribute.Key == "RefitInterfaceStubGenerator")
            .Value!;

        var generatorType = System.Reflection.Assembly.LoadFrom(path)
            .GetTypes()
            .Single(type => !type.IsAbstract && typeof(IIncrementalGenerator).IsAssignableFrom(type));

        return ((IIncrementalGenerator)Activator.CreateInstance(generatorType)!).AsSourceGenerator();
    }

    // Uses the constructor of the generated client that needs no reflection request builder
    private static object CreateGeneratedClient(
        System.Reflection.Assembly assembly,
        Type interfaceType,
        HttpClient httpClient)
    {
        var clientType = assembly
            .GetTypes()
            .Single(type => type.IsClass && interfaceType.IsAssignableFrom(type));

        return Activator.CreateInstance(clientType, httpClient, new RefitSettings())!;
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
