using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NJsonSchema;
using NJsonSchema.CodeGeneration.CSharp;
using NSwag;
using NSwag.CodeGeneration;
using NSwag.CodeGeneration.CSharp;

namespace Refitter.Core
{
    public class RefitGenerator
    {
        private const string Separator = "    ";

        public async Task<string> Generate(string swaggerFile, string defaultNamespace)
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
                    Namespace = defaultNamespace,
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
                .AppendLine($"{Separator}public interface I{CapitalizeFirstCharacter(title)}")
                .AppendLine($"{Separator}{{");

            foreach (var kv in document.Paths)
            {
                foreach (var operations in kv.Value)
                {
                    var operation = operations.Value;

                    var returnTypeParameter = operation.Responses.ContainsKey("200")
                        ? generator.GetTypeName(operation.Responses["200"].Schema, true, null)
                        : null;

                    var returnType = returnTypeParameter == null
                        ? "Task"
                        : $"Task<{TrimImportedNamespaces(returnTypeParameter)}>";

                    var verb = CapitalizeFirstCharacter(operations.Key);
                    var name = ToPascalCase(CapitalizeFirstCharacter(operation.OperationId));

                    var parameters = GetParameters(generator, operation);
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

        private static string ToPascalCase(string operationId)
        {
            var parts = operationId.Split('-');
            for (var i = 0; i < parts.Length; i++)
            {
                parts[i] = CapitalizeFirstCharacter(parts[i]);
            }
            return string.Join(string.Empty, parts);
        }

        private static IEnumerable<string> GetParameters(CSharpClientGenerator generator, OpenApiOperation operation)
        {
            var routeParameters = operation.Parameters
                .Where(p => p.Kind == OpenApiParameterKind.Path)
                .Select(p => $"{generator.GetTypeName(p.ActualTypeSchema, true, null)} {p.Name}")
                .ToList();

            var bodyParameters = operation.Parameters
                .Where(p => p.Kind == OpenApiParameterKind.Body)
                .Select(p => $"[Body]{GetBodyParameterType(generator, p)} {p.Name}")
                .ToList();

            var parameters = new List<string>();
            parameters.AddRange(routeParameters);
            parameters.AddRange(bodyParameters);
            return parameters;
        }

        private static string GetBodyParameterType(IClientGenerator generator, JsonSchema schema) =>
            TrimImportedNamespaces(
                FindSupportedType(
                    generator.GetTypeName(
                        schema.ActualTypeSchema,
                        true,
                        null)));

        private static string FindSupportedType(string typeName) =>
            typeName == "FileResponse" ? "FileParameter" : typeName;

        private static string TrimImportedNamespaces(string returnTypeParameter)
        {
            string[] wellKnownNamespaces = { "System.Collections.Generic" };
            foreach (var wellKnownNamespace in wellKnownNamespaces)
                if (returnTypeParameter.StartsWith(wellKnownNamespace, StringComparison.OrdinalIgnoreCase))
                    return returnTypeParameter.Replace(wellKnownNamespace + ".", string.Empty);
            return returnTypeParameter;
        }

        private static string CapitalizeFirstCharacter(string str)
        {
            return str.Substring(0, 1).ToUpperInvariant() +
                   str.Substring(1, str.Length - 1);
        }
    }
}