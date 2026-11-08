using System.Diagnostics.CodeAnalysis;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Refitter.SourceGenerator;

/// <summary>
/// Passes the project's analyzer config options through to the Refit generator, except that it re-enables
/// the generator that Refitter's MSBuild targets disabled for the regular compiler pass.
/// </summary>
[ExcludeFromCodeCoverage]
internal sealed class RefitGeneratorOptionsProvider : AnalyzerConfigOptionsProvider
{
    private readonly AnalyzerConfigOptionsProvider inner;

    public RefitGeneratorOptionsProvider(AnalyzerConfigOptionsProvider inner)
    {
        this.inner = inner;
        GlobalOptions = new EnabledOptions(inner.GlobalOptions);
    }

    public override AnalyzerConfigOptions GlobalOptions { get; }

    public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => inner.GetOptions(tree);

    public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => inner.GetOptions(textFile);

    private sealed class EnabledOptions : AnalyzerConfigOptions
    {
        private const string DisableRefitSourceGenerator = "build_property.DisableRefitSourceGenerator";

        private readonly AnalyzerConfigOptions inner;

        public EnabledOptions(AnalyzerConfigOptions inner)
        {
            this.inner = inner;
        }

        // NotNullWhenAttribute isn't accessible on netstandard2.0, which makes the override differ in nullability
#pragma warning disable CS8765
        public override bool TryGetValue(string key, out string? value)
#pragma warning restore CS8765
        {
            if (KeyComparer.Equals(key, DisableRefitSourceGenerator))
            {
                value = "false";
                return true;
            }

            return inner.TryGetValue(key, out value);
        }
    }
}
