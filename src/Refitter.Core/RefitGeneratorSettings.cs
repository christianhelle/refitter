using System.Diagnostics.CodeAnalysis;

namespace Refitter.Core
{
    [ExcludeFromCodeCoverage]
    public class RefitGeneratorSettings
    {
        public string OpenApiPath { get; set; } = null!;

        public string Namespace { get; set; } = "GeneratedCode";

        public NamingSettings Naming { get; set; } = new();

        public bool GenerateContracts { get; set; } = true;

        public bool GenerateXmlDocCodeComments { get; set; } = true;
    }

    [ExcludeFromCodeCoverage]
    public class NamingSettings
    {
        public bool UseOpenApiTitle { get; set; } = true;

        public string InterfaceName { get; set; } = "ApiClient";
    }
}