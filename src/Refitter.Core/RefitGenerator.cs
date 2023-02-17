using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NJsonSchema.CodeGeneration.CSharp;
using NSwag;
using NSwag.CodeGeneration.CSharp;

namespace Refitter.Core
{
    public class RefitGenerator
    {
        private const string Separator = "    ";

        public async Task<string> Generate(string swaggerFile)
        {
            var document = await (swaggerFile.EndsWith("yaml") || swaggerFile.EndsWith("yml")
                ? OpenApiYamlDocument.FromFileAsync(swaggerFile)
                : OpenApiDocument.FromFileAsync(swaggerFile));

            var settings = new CSharpClientGeneratorSettings
            {
                GenerateClientClasses = false,
                GenerateDtoTypes = true,
                GenerateClientInterfaces = false,
                CSharpGeneratorSettings =
                {
                    JsonLibrary = CSharpJsonLibrary.SystemTextJson
                }
            };

            var generator = new CSharpClientGenerator(document, settings);
            var contracts = generator.GenerateFile();
            var client = GenerateClient(document, generator);

            return new StringBuilder()
                .AppendLine(client)
                .AppendLine()
                .AppendLine(contracts)
                .ToString();
        }

        private static string GenerateClient(OpenApiDocument document, CSharpClientGenerator generator)
        {
            var title = document.Info?.Title?
                            .Replace(" ", string.Empty)
                            .Replace("-", string.Empty)
                            .Replace(".", string.Empty) ??
                        "ApiClient";

            var code = new StringBuilder();
            code.AppendLine("namespace " + generator.Settings.CSharpGeneratorSettings.Namespace)
                .AppendLine("{")
                .AppendLine($"{Separator}using Refit;")
                .AppendLine($"{Separator}using System.Threading.Tasks;")
                .AppendLine($"{Separator}using System.Collections.Generic;")
                .AppendLine()
                .AppendLine($"{Separator}public interface I{ToPascalCase(title)}")
                .AppendLine($"{Separator}{{");

            foreach (var kv in document.Paths)
            {
                foreach (var operations in kv.Value)
                {
                    var operation = operations.Value;
                    var parameters = operation.Parameters
                        .Where(p => p.Kind == OpenApiParameterKind.Path)
                        .Select(p => $"string {p.Name}")
                        .ToList();

                    var returnTypeParameter = operation.Responses.ContainsKey("200")
                        ? generator.GetTypeName(operation.Responses["200"].Schema, true, null)
                        : null;

                    var returnType = returnTypeParameter == null
                        ? "Task"
                        : $"Task<{TrimImportedNamespaces(returnTypeParameter)}>";

                    var verb = ToPascalCase(operations.Key);
                    var name = ToPascalCase(operation.OperationId);
                    var parametersString = string.Join(", ", parameters);
                    code.AppendLine($"{Separator}{Separator}[{verb}(\"{kv.Key}\")]")
                        .AppendLine($"{Separator}{Separator}{returnType} {name}({parametersString});")
                        .AppendLine();
                }
            }

            code.AppendLine($"{Separator}}}")
                .AppendLine("}");

            return code.ToString();
        }

        private static string TrimImportedNamespaces(string returnTypeParameter)
        {
            string[] wellKnownNamespaces = { "System.Collections.Generic" };
            foreach (var wellKnownNamespace in wellKnownNamespaces)
                if (returnTypeParameter.StartsWith(wellKnownNamespace, StringComparison.OrdinalIgnoreCase))
                    return returnTypeParameter.Replace(wellKnownNamespace + ".", string.Empty);
            return returnTypeParameter;
        }

        private static string ToPascalCase(string str)
        {
            return str.Substring(0, 1).ToUpperInvariant() +
                   str.Substring(1, str.Length - 1);
        }
    }
}