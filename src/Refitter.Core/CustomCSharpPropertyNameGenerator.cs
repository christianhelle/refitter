using NJsonSchema;
using NJsonSchema.CodeGeneration.CSharp;

namespace Refitter.Core;

internal class CustomCSharpPropertyNameGenerator : CSharpPropertyNameGenerator
{
    public override string Generate(JsonSchemaProperty property) =>
        string.IsNullOrWhiteSpace(property.Name) ? "_" : base.Generate(property);
}