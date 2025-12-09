using NJsonSchema;
using NJsonSchema.CodeGeneration;
using NJsonSchema.CodeGeneration.CSharp;

namespace Refitter.Core;

internal class CustomCSharpTypeResolver : CSharpTypeResolver
{
    private readonly CodeGeneratorSettings? _codeGeneratorSettings;
    private readonly CSharpTypeResolver? _baseResolver;

    public CustomCSharpTypeResolver(
        CSharpGeneratorSettings settings,
        CodeGeneratorSettings? codeGeneratorSettings,
        CSharpTypeResolver? baseResolver = null)
        : base(settings)
    {
        _codeGeneratorSettings = codeGeneratorSettings;
        _baseResolver = baseResolver;
    }

    public override string Resolve(JsonSchema schema, bool isNullable, string? typeNameHint)
    {
        // Check if we have a type override for this schema's type and format combination
        if (_codeGeneratorSettings?.TypeOverrides?.Length > 0 &&
            !string.IsNullOrEmpty(schema.Format))
        {
            // Convert JsonObjectType enum to lowercase string (e.g., JsonObjectType.String -> "string")
            var typeString = schema.Type.ToString().ToLowerInvariant();
            var formatPattern = $"{typeString}:{schema.Format}";
            var typeOverride = _codeGeneratorSettings.TypeOverrides.FirstOrDefault(
                to => to.FormatPattern?.Equals(formatPattern, StringComparison.OrdinalIgnoreCase) == true);

            if (typeOverride != null && !string.IsNullOrEmpty(typeOverride.TypeName))
            {
                // Return the custom type, optionally with nullable wrapper
                return isNullable && Settings.GenerateNullableReferenceTypes
                    ? $"{typeOverride.TypeName}?"
                    : typeOverride.TypeName;
            }
        }

        // If we have a base resolver (from the parent generator), use it
        // This ensures we maintain all the schema registrations
        if (_baseResolver != null)
        {
            return _baseResolver.Resolve(schema, isNullable, typeNameHint);
        }

        return base.Resolve(schema, isNullable, typeNameHint);
    }
}
