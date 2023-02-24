using System.Diagnostics.CodeAnalysis;

namespace Refitter.Core
{
    [ExcludeFromCodeCoverage]
    public class RefitGeneratorSettings
    {
        public string OpenApiPath { get; set; }

        public string Namespace { get; set; } = "GeneratedCode";

        public NamingSettings Naming { get; set; } = new();

        public bool GenerateContracts { get; set; } = true;

        public bool GenerateCodeComments { get; set; }
    }

    [ExcludeFromCodeCoverage]
    public class NamingSettings
    {
        public bool UseOpenApiTitle { get; set; } = true;

        public string ClassName { get; set; } = "ApiClient";
    }
}