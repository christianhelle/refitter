using NSwag;

using System.Text;
using System.Text.RegularExpressions;

namespace Refitter.Core;

/// <summary>
/// Generates Refit clients and interfaces based on an OpenAPI specification.
/// </summary>
public class RefitGenerator
{
    private readonly RefitGeneratorSettings settings;
    private readonly OpenApiDocument document;
    private readonly CSharpClientGeneratorFactory factory;

    private RefitGenerator(RefitGeneratorSettings settings, OpenApiDocument document)
    {
        this.settings = settings;
        this.document = document;
        factory = new CSharpClientGeneratorFactory(settings, document);
    }

    /// <summary>
    /// Creates a new instance of the <see cref="RefitGenerator"/> class asynchronously.
    /// </summary>
    /// <param name="settings">The settings used to configure the generator.</param>
    /// <returns>A new instance of the <see cref="RefitGenerator"/> class.</returns>
    public static async Task<RefitGenerator> CreateAsync(RefitGeneratorSettings settings)
    {
        OpenApiDocument document;
        if (IsHttp(settings.OpenApiPath) && IsYaml(settings.OpenApiPath))
        {
            document = await OpenApiYamlDocument.FromUrlAsync(settings.OpenApiPath);
        }
        else if (IsHttp(settings.OpenApiPath))
        {
            document = await OpenApiDocument.FromUrlAsync(settings.OpenApiPath);
        }
        else if (IsYaml(settings.OpenApiPath))
        {
            document = await OpenApiYamlDocument.FromFileAsync(settings.OpenApiPath);
        }
        else
        {
            document = await OpenApiDocument.FromFileAsync(settings.OpenApiPath);
        }

        ProcessTagFilters(document, settings.IncludeTags);
        ProcessPathFilters(document, settings.IncludePathMatches);

        return new RefitGenerator(settings, document);
    }

    private static void ProcessTagFilters(OpenApiDocument document,
        string[] includeTags)
    {
        if (includeTags.Length == 0)
        {
            return;
        }
        var clonedPaths = document.Paths
            .Where(x => x.Value != null)
            .ToArray();
        foreach (var path in clonedPaths)
        {
            var methods = path.Value
                .Where(x => x.Value != null)
                .ToArray();
            foreach (var method in methods)
            {
                var exclude = true;
                foreach (var tag in includeTags)
                {
                    if (method.Value.Tags?.Any(x => x == tag) == true)
                    {
                        exclude = false;
                    }
                }
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
        var regexes = pathMatchExpressions.Select(x => new Regex(x, RegexOptions.Compiled)).ToArray();

        var clonedPaths = document.Paths.ToArray();
        foreach (var path in clonedPaths)
        {
            var exclude = true;
            foreach (var regex in regexes)
            {
                if (regex.IsMatch(path.Key))
                {
                    exclude = false;
                }
            }
            if (exclude)
            {
                document.Paths.Remove(path.Key);
            }
        }
    }

    /// <summary>
    /// Generates Refit clients and interfaces based on an OpenAPI specification and returns the generated code as a string.
    /// </summary>
    /// <returns>The generated code as a string.</returns>
    public string Generate()
    {
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

        var client = GenerateClient(interfaceGenerator);
        return new StringBuilder()
            .AppendLine(client)
            .AppendLine()
            .AppendLine(settings.GenerateContracts ? contracts : string.Empty)
            .ToString();
    }

    /// <summary>
    /// Generates the client code based on the specified interface generator.
    /// </summary>
    /// <param name="interfaceGenerator">The interface generator used to generate the client code.</param>
    /// <returns>The generated client code as a string.</returns>
    private string GenerateClient(IRefitInterfaceGenerator interfaceGenerator)
    {
        var code = new StringBuilder();
        GenerateAutoGeneratedHeader(code);
        code.AppendLine(RefitInterfaceImports.GenerateNamespaceImports(settings))
            .AppendLine();

        if (settings.AdditionalNamespaces.Any())
        {
            foreach (var ns in settings.AdditionalNamespaces)
            {
                code.AppendLine($"using {ns};");
            }

            code.AppendLine();
        }

        code.AppendLine($$"""
            namespace {{settings.Namespace}}
            {
            {{interfaceGenerator.GenerateCode()}}
            }
            """);

        return code.ToString();
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

    /// <summary>
    /// Determines whether the specified path is an HTTP URL.
    /// </summary>
    /// <param name="path">The path to check.</param>
    /// <returns>True if the path is an HTTP URL, otherwise false.</returns>
    private static bool IsHttp(string path)
    {
        return path.StartsWith("http://") || path.StartsWith("https://");
    }

    /// <summary>
    /// Determines whether the specified path is a YAML file.
    /// </summary>
    /// <param name="path">The path to check.</param>
    /// <returns>True if the path is a YAML file, otherwise false.</returns>
    private static bool IsYaml(string path)
    {
        return path.EndsWith("yaml") || path.EndsWith("yml");
    }
}