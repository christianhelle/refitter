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
            return contracts;
        }

        private static string GenerateClient(OpenApiDocument document)
        {
            var code = new StringBuilder();
            code.AppendLine("public interface IApiClient")
                .AppendLine("{");

            foreach (var kv in document.Paths)
            {
                var path = kv.Key;
                var item = kv.Value;
            }

            code.AppendLine("}")
                .AppendLine();
            
            return code.ToString();
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