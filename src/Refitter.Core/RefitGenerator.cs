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
        public async Task<string> Generate(string swaggerFile)
        {
            var document = await (swaggerFile.EndsWith("yaml") || swaggerFile.EndsWith("yml")
                ? OpenApiYamlDocument.FromFileAsync(swaggerFile)
                : OpenApiDocument.FromFileAsync(swaggerFile));

            var contracts = GenerateContracts(document);
            var client = GenerateClient(document);

            return new StringBuilder()
                .AppendLine(client)
                .AppendLine()
                .AppendLine(contracts)
                .ToString();
        }

        private static string GenerateClient(OpenApiDocument document)
        {
            var code = new StringBuilder();
            code.AppendLine("using Refit;")
                .AppendLine()
                .AppendLine("public interface IApiClient")
                .AppendLine("{");

            foreach (var kv in document.Paths)
            {
                foreach (var operations in kv.Value)
                {
                    var operation = operations.Value;
                    var parameters = operation.Parameters
                        .Where(p => p.Kind == OpenApiParameterKind.Path)
                        .Select(p => $"string {p.Name}")
                        .ToList();

                    var method = ToPascalCase(operations.Key);
                    var returnTypeParameter = operation.Responses.ContainsKey("200")
                        ? "object"
                        : null;
                    var returnType = returnTypeParameter == null ? "Task" : $"Task<{returnTypeParameter}>";

                    var name = ToPascalCase(operation.OperationId);
                    var parametersString = string.Join(", ", parameters);
                    code.AppendLine($"\t[{method}(\"{kv.Key}\")]")
                        .AppendLine($"\t{returnType} {name}({parametersString});")
                        .AppendLine();
                }
            }

            code.Remove(code.Length - 3, 2)
                .AppendLine("}")
                .AppendLine();

            return code.ToString();
        }

        private static string ToPascalCase(string str)
        {
            return str.Substring(0, 1).ToUpperInvariant() +
                   str.Substring(1, str.Length - 1);
        }

        private static string GenerateContracts(OpenApiDocument document)
        {
            var settings = new CSharpClientGeneratorSettings
            {
                GenerateClientClasses = false,
                GenerateDtoTypes = true,
                GenerateClientInterfaces = false,
            };

            settings.CSharpGeneratorSettings.JsonLibrary = CSharpJsonLibrary.SystemTextJson;
            var generator = new CSharpClientGenerator(document, settings);
            return generator.GenerateFile();
        }
    }
}