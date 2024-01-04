using NSwag;

using System.Text;
using System.Text.RegularExpressions;

using Microsoft.OpenApi;
using Microsoft.OpenApi.Extensions;
using Microsoft.OpenApi.Readers;

namespace Refitter.Core;

/// <summary>
/// Generates Refit clients and interfaces based on an OpenAPI specification.
/// </summary>
public class RefitGenerator(RefitGeneratorSettings settings, OpenApiDocument document)
{
    /// <summary>
    /// Creates a new instance of the <see cref="RefitGenerator"/> class asynchronously.
    /// </summary>
    /// <param name="settings">The settings used to configure the generator.</param>
    /// <returns>A new instance of the <see cref="RefitGenerator"/> class.</returns>
    public static async Task<RefitGenerator> CreateAsync(RefitGeneratorSettings settings)
    {
        var openApiDocument = await GetOpenApiDocument(settings);

        ProcessTagFilters(openApiDocument, settings.IncludeTags);
        ProcessPathFilters(openApiDocument, settings.IncludePathMatches);
        ProcessContractFilter(openApiDocument, settings.TrimUnusedSchema, settings.KeepSchemaPatterns);

        return new RefitGenerator(settings, openApiDocument);
    }

    private static async Task<OpenApiDocument> GetOpenApiDocument(RefitGeneratorSettings settings)
    {
        var specialCharacters = new[]
        {
            ":"
        };
        
        return specialCharacters.Aggregate(
            await OpenApiDocumentFactory.CreateAsync(settings.OpenApiPath),
            SanitizePath);
    }

    private static OpenApiDocument SanitizePath(
        OpenApiDocument openApiDocument, 
        string stringToRemove)
    {
        var paths = openApiDocument.Paths.Keys
            .Where(pathKey => pathKey.Contains(stringToRemove))
            .ToArray();

        foreach (var path in paths)
        {
            var value = openApiDocument.Paths[path];
            openApiDocument.Paths.Remove(path);
            openApiDocument.Paths.Add(path.Replace(stringToRemove, string.Empty), value);
        }

        return openApiDocument;
    }

    private static void ProcessContractFilter(OpenApiDocument openApiDocument, bool removeUnusedSchema, string[] includeSchemaMatches)
    {
        if (!removeUnusedSchema)
        {
            return;
        }
        var cleaner = new SchemaCleaner(openApiDocument, includeSchemaMatches);
        cleaner.RemoveUnreferencedSchema();
    }

    private static void ProcessTagFilters(OpenApiDocument document, IReadOnlyCollection<string> includeTags)
    {
        if (includeTags.Count == 0)
        {
            return;
        }
        var clonedPaths = document.Paths.Where(pair => pair.Value != null)
            // as we modify the document.Paths
            // we have to enumerate on a snapshot of the items
            .ToArray();
        foreach (var path in clonedPaths)
        {
            var methods = path.Value.Where(pair => pair.Value != null)
                // same reason as with document.Paths
                .ToArray();
            foreach (var method in methods)
            {
                var exclude = method.Value.Tags?.Exists(includeTags.Contains) != true;
                if (exclude)
                {
                    path.Value.Remove(method.Key);
                }
                if (path.Value.Count == 0)
                {
                    document.Paths.Remove(path.Key);
                }
            }
        }
    }

    private static void ProcessPathFilters(OpenApiDocument document,
        string[] pathMatchExpressions)
    {
        if (pathMatchExpressions.Length == 0)
        {
            return;
        }
        
        // compile all expressions here once, as we will use them more than once
        var regexes = pathMatchExpressions.Select(x => new Regex(x, RegexOptions.Compiled)).ToList();
        var paths = document.Paths.Keys
            .Where(pathKey => regexes.TrueForAll(regex => !regex.IsMatch(pathKey)))
            .ToArray();

        foreach (string pathKey in paths)
        {
            document.Paths.Remove(pathKey);
        }
    }

    /// <summary>
    /// Generates Refit clients and interfaces based on an OpenAPI specification and returns the generated code as a string.
    /// </summary>
    /// <returns>The generated code as a string.</returns>
    public string Generate()
    {
        var factory = new CSharpClientGeneratorFactory(settings, document);
        var generator = factory.Create();
        var contracts = RefitInterfaceImports
            .GetImportedNamespaces(settings)
            .Aggregate(
                generator.GenerateFile(),
                (current, import) => current.Replace($"{import}.", string.Empty));

        IRefitInterfaceGenerator interfaceGenerator = settings.MultipleInterfaces switch
        {
            MultipleInterfaces.ByEndpoint => new RefitMultipleInterfaceGenerator(settings, document, generator),
            MultipleInterfaces.ByTag => new RefitMultipleInterfaceByTagGenerator(settings, document, generator),
            _ => new RefitInterfaceGenerator(settings, document, generator),
        };

        var generatedCode = GenerateClient(interfaceGenerator);
        return new StringBuilder()
            .AppendLine(generatedCode.SourceCode)
            .AppendLine()
            .AppendLine(settings.GenerateContracts ? contracts : string.Empty)
            .AppendLine(DependencyInjectionGenerator.Generate(settings, generatedCode.InterfaceNames))
            .ToString()
            .TrimEnd();
    }

    /// <summary>
    /// Generates the client code based on the specified interface generator.
    /// </summary>
    /// <param name="interfaceGenerator">The interface generator used to generate the client code.</param>
    /// <returns>The generated client code as a string.</returns>
    private RefitGeneratedCode GenerateClient(IRefitInterfaceGenerator interfaceGenerator)
    {
        var code = new StringBuilder();
        GenerateAutoGeneratedHeader(code);
        
        code.AppendLine()
            .AppendLine(RefitInterfaceImports.GenerateNamespaceImports(settings))
            .AppendLine();

        if (settings.AdditionalNamespaces.Any())
        {
            foreach (var ns in settings.AdditionalNamespaces)
            {
                code.AppendLine($"using {ns};");
            }

            code.AppendLine();
        }

        if (settings.OptionalParameters)
        {
            code.AppendLine("#nullable enable");
        }

        var refitInterfaces = interfaceGenerator.GenerateCode();
        code.AppendLine($$"""
                          namespace {{settings.Namespace}}
                          {
                          {{refitInterfaces}}
                          }
                          """);

        return new RefitGeneratedCode(code.ToString(), refitInterfaces.InterfaceNames);
    }

    /// <summary>
    /// Generates the auto-generated header if the setting is enabled.
    /// </summary>
    /// <param name="code">The string builder to append the header to.</param>
    private void GenerateAutoGeneratedHeader(StringBuilder code)
    {
        if (!settings.AddAutoGeneratedHeader)
            return;

        code.AppendLine("""
            // <auto-generated>
            //     This code was generated by Refitter.
            // </auto-generated>

            """);
    }
}