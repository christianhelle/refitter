using System.Globalization;
using System.Text;
using NJsonSchema;
using NSwag;
using Refitter.Core;
using TUnit.Core;

namespace Refitter.Tests.Parity;

/// <summary>
/// Temporary differential test for the NSwag removal: the native document model must describe every parity
/// spec exactly like the NSwag model does. Deleted together with NSwag.
/// </summary>
[Category("Parity")]
public class DocumentModelParityTests
{
    public static IEnumerable<Func<string>> Specs() =>
        ParitySpecs.All.Select(spec => (Func<string>)(() => spec.Id));

    [Test]
    [MethodDataSource(nameof(Specs))]
    public async Task Native_Document_Model_Matches_NSwag(string specId)
    {
        var path = ParitySpecs.Get(specId).Path;

        string expected;
        try
        {
            var nswagDocument = await OpenApiDocumentParser.FromFileAsync(path);
            expected = new NSwagDumper(nswagDocument).Dump();
        }
        catch (Exception exception)
        {
            expected = "!! load failed";
            _ = exception;
        }

        string actual;
        try
        {
            var nativeDocument = ApiDocumentLoader.LoadFile(path);
            actual = new NativeDumper(nativeDocument).Dump();
        }
        catch (Exception exception)
        {
            actual = expected == "!! load failed" ? "!! load failed" : "!! load failed: " + exception;
        }

        if (expected != actual)
        {
            throw new InvalidOperationException(DescribeFirstDifference(expected, actual));
        }
    }

    internal static string DescribeFirstDifference(string expected, string actual)
    {
        var expectedLines = expected.Split('\n');
        var actualLines = actual.Split('\n');
        var index = 0;
        while (index < expectedLines.Length && index < actualLines.Length && expectedLines[index] == actualLines[index])
        {
            index++;
        }

        var context = string.Join("\n", expectedLines.Skip(Math.Max(0, index - 5)).Take(5));
        return $"""
                Models differ at line {index + 1}.
                Context:
                {context}
                Expected: {(index < expectedLines.Length ? expectedLines[index] : "<end>")}
                Actual:   {(index < actualLines.Length ? actualLines[index] : "<end>")}
                """;
    }

    private abstract class DumperBase
    {
        protected readonly StringBuilder Output = new();
        private int depth;

        protected void Line(string text) => Output.Append(' ', depth * 2).Append(text).Append('\n');

        protected void Indent(Action action)
        {
            depth++;
            action();
            depth--;
        }

        protected static string Value(object? value) =>
            value switch
            {
                null => "null",
                string s => "string:" + s,
                bool b => "bool:" + b,
                IFormattable f => value.GetType().Name + ":" + f.ToString(null, CultureInfo.InvariantCulture),
                _ => "json:" + value.ToString()!.Replace("\r\n", "\n"),
            };
    }

    private sealed class NSwagDumper : DumperBase
    {
        private readonly OpenApiDocument document;
        private readonly Dictionary<JsonSchema, string> names = new();

        public NSwagDumper(OpenApiDocument document)
        {
            this.document = document;
            foreach (var definition in document.Definitions)
                names.TryAdd(definition.Value, definition.Key);
        }

        public string Dump()
        {
            Line($"schemaType {document.SchemaType}");
            Line($"info {document.Info?.Title} {document.Info?.Version}");
            foreach (var tag in document.Tags)
                Line($"tag {tag.Name} {tag.Description}");
            foreach (var security in document.SecurityDefinitions)
                Line($"security {security.Key} {security.Value.Type} {security.Value.Name} {security.Value.In} {security.Value.Scheme}");
            foreach (var path in document.Paths)
            {
                Line($"path {path.Key}");
                Indent(() =>
                {
                    foreach (var operation in path.Value.ActualPathItem)
                    {
                        var op = operation.Value;
                        Line($"operation {operation.Key} id={op.OperationId} tags={string.Join(",", op.Tags)} deprecated={op.IsDeprecated}");
                        Indent(() =>
                        {
                            Line($"summary {op.Summary}");
                            Line($"consumes {string.Join(",", op.ActualConsumes)} produces {string.Join(",", op.ActualProduces)}");
                            Line($"security {string.Join(";", op.ActualSecurity?.Select(s => string.Join(",", s.Keys)) ?? [])}");
                            foreach (var parameter in op.ActualParameters)
                            {
                                Line($"parameter {parameter.Name} kind={parameter.Kind} required={parameter.IsRequired} style={parameter.Style} explode={parameter.Explode} format={parameter.CollectionFormat} nullable2={parameter.IsNullable(NJsonSchema.SchemaType.Swagger2)} nullable3={parameter.IsNullable(NJsonSchema.SchemaType.OpenApi3)} binary={parameter.IsBinaryBodyParameter} xml={parameter.IsXmlBodyParameter}");
                                Indent(() => DumpSchema(parameter.ActualSchema, 0));
                            }

                            if (op.ActualRequestBody != null)
                            {
                                Line($"requestBody {op.ActualRequestBody.ActualName} required={op.ActualRequestBody.IsRequired}");
                                foreach (var content in op.ActualRequestBody.Content)
                                {
                                    Line($"content {content.Key}");
                                    Indent(() => DumpSchemaOrNull(content.Value.Schema));
                                }
                            }

                            foreach (var response in op.Responses)
                            {
                                var actual = response.Value.ActualResponse;
                                Line($"response {response.Key} {actual.Description} binary={actual.IsBinary(op)} nullable={actual.IsNullable(document.SchemaType)}");
                                foreach (var content in actual.Content)
                                {
                                    Line($"content {content.Key}");
                                    Indent(() => DumpSchemaOrNull(content.Value.Schema));
                                }
                            }
                        });
                    }
                });
            }

            foreach (var definition in document.Definitions)
            {
                Line($"definition {definition.Key}");
                Indent(() => DumpSchema(definition.Value, 0, skipName: true));
            }

            return Output.ToString();
        }

        private void DumpSchemaOrNull(JsonSchema? schema)
        {
            if (schema == null)
                Line("<null>");
            else
                DumpSchema(schema, 0);
        }

        private void DumpSchema(JsonSchema schema, int level, bool skipName = false)
        {
            if (!skipName && names.TryGetValue(schema, out var name))
            {
                Line($"#{name}");
                return;
            }

            if (level > 12)
            {
                Line("...");
                return;
            }

            var property = schema as JsonSchemaProperty;
            Line($"schema type={schema.Type} format={schema.Format} title={schema.Title} nullableRaw={schema.IsNullableRaw} nullable3={schema.IsNullable(NJsonSchema.SchemaType.OpenApi3)} nullable2={schema.IsNullable(NJsonSchema.SchemaType.Swagger2)} deprecated={schema.IsDeprecated} abstract={schema.IsAbstract} addl={schema.AllowAdditionalProperties} required={string.Join(",", schema.RequiredProperties)}" +
                 (property != null ? $" readOnly={property.IsReadOnly} writeOnly={property.IsWriteOnly} isRequired={property.IsRequired}" : string.Empty));
            Indent(() =>
            {
                if (schema.Description != null)
                    Line($"description {schema.Description}");
                if (schema.Default != null)
                    Line($"default {Value(schema.Default)}");
                if (schema.Example != null)
                    Line($"example {Value(schema.Example)}");
                if (schema.Minimum != null || schema.Maximum != null || schema.MinLength != null || schema.MaxLength != null || schema.Pattern != null || schema.MinItems != 0 || schema.MaxItems != 0 || schema.MultipleOf != null || schema.IsExclusiveMaximum || schema.IsExclusiveMinimum || schema.ExclusiveMaximum != null || schema.ExclusiveMinimum != null)
                    Line($"bounds {schema.Minimum} {schema.Maximum} {schema.IsExclusiveMinimum} {schema.IsExclusiveMaximum} {schema.ExclusiveMinimum} {schema.ExclusiveMaximum} {schema.MinLength} {schema.MaxLength} {schema.Pattern} {schema.MinItems} {schema.MaxItems} {schema.MultipleOf}");
                if (schema.Enumeration.Count > 0)
                    Line($"enum {string.Join("|", schema.Enumeration.Select(Value))}");
                if (schema.EnumerationNames.Count > 0)
                    Line($"enumNames {string.Join("|", schema.EnumerationNames)}");
                if (schema.EnumerationDescriptions.Count > 0)
                    Line($"enumDescriptions {string.Join("|", schema.EnumerationDescriptions)}");
                if (schema.IsFlagEnumerable)
                    Line("flags");
                if (schema.ExtensionData?.Count > 0)
                    Line($"extensions {string.Join(",", schema.ExtensionData.Select(e => e.Key + "=" + (e.Value is JsonSchema ? "(schema)" : e.Value is IDictionary<string, object?> || e.Value is object[] ? "(complex)" : Value(e.Value))))}");
                if (schema.DiscriminatorObject != null)
                {
                    Line($"discriminator {schema.DiscriminatorObject.PropertyName}");
                    foreach (var mapping in schema.DiscriminatorObject.Mapping)
                    {
                        Line($"mapping {mapping.Key}");
                        Indent(() => DumpSchema(mapping.Value, level + 1));
                    }
                }

                if (schema.Reference != null)
                {
                    Line("reference");
                    Indent(() => DumpSchema(schema.Reference, level + 1));
                }

                foreach (var p in schema.Properties)
                {
                    Line($"property {p.Key}");
                    Indent(() => DumpSchema(p.Value, level + 1));
                }

                if (schema.Item != null)
                {
                    Line("item");
                    Indent(() => DumpSchema(schema.Item, level + 1));
                }

                foreach (var item in schema.Items)
                {
                    Line("tupleItem");
                    Indent(() => DumpSchema(item, level + 1));
                }

                if (schema.AdditionalPropertiesSchema != null)
                {
                    Line("additionalProperties");
                    Indent(() => DumpSchema(schema.AdditionalPropertiesSchema, level + 1));
                }

                foreach (var s in schema.AllOf)
                {
                    Line("allOf");
                    Indent(() => DumpSchema(s, level + 1));
                }

                foreach (var s in schema.OneOf)
                {
                    Line("oneOf");
                    Indent(() => DumpSchema(s, level + 1));
                }

                foreach (var s in schema.AnyOf)
                {
                    Line("anyOf");
                    Indent(() => DumpSchema(s, level + 1));
                }

                if (schema.Not != null)
                {
                    Line("not");
                    Indent(() => DumpSchema(schema.Not, level + 1));
                }
            });
        }
    }

    private sealed class NativeDumper : DumperBase
    {
        private readonly ApiDocument document;
        private readonly Dictionary<ApiSchema, string> names = new();

        public NativeDumper(ApiDocument document)
        {
            this.document = document;
            foreach (var definition in document.Definitions)
                names.TryAdd(definition.Value, definition.Key);
        }

        public string Dump()
        {
            Line($"schemaType {document.SchemaType}");
            Line($"info {document.Info?.Title} {document.Info?.Version}");
            foreach (var tag in document.Tags)
                Line($"tag {tag.Name} {tag.Description}");
            foreach (var security in document.SecurityDefinitions)
                Line($"security {security.Key} {security.Value.Type} {security.Value.Name} {security.Value.In} {security.Value.Scheme}");
            foreach (var path in document.Paths)
            {
                Line($"path {path.Key}");
                Indent(() =>
                {
                    foreach (var operation in path.Value.ActualPathItem)
                    {
                        var op = operation.Value;
                        Line($"operation {operation.Key} id={op.OperationId} tags={string.Join(",", op.Tags)} deprecated={op.IsDeprecated}");
                        Indent(() =>
                        {
                            Line($"summary {op.Summary}");
                            Line($"consumes {string.Join(",", op.ActualConsumes)} produces {string.Join(",", op.ActualProduces)}");
                            Line($"security {string.Join(";", op.ActualSecurity?.Select(s => string.Join(",", s.Keys)) ?? [])}");
                            foreach (var parameter in op.ActualParameters)
                            {
                                Line($"parameter {parameter.Name} kind={parameter.Kind} required={parameter.IsRequired} style={StyleName(parameter.Style)} explode={parameter.Explode} format={FormatName(parameter.CollectionFormat)} nullable2={parameter.IsNullable(ApiSchemaType.Swagger2)} nullable3={parameter.IsNullable(ApiSchemaType.OpenApi3)} binary={parameter.IsBinaryBodyParameter} xml={parameter.IsXmlBodyParameter}");
                                Indent(() => DumpSchema(parameter.ActualSchema, 0));
                            }

                            if (op.ActualRequestBody != null)
                            {
                                Line($"requestBody {op.ActualRequestBody.ActualName} required={op.ActualRequestBody.IsRequired}");
                                foreach (var content in op.ActualRequestBody.Content)
                                {
                                    Line($"content {content.Key}");
                                    Indent(() => DumpSchemaOrNull(content.Value.Schema));
                                }
                            }

                            foreach (var response in op.Responses)
                            {
                                var actual = response.Value.ActualResponse;
                                Line($"response {response.Key} {actual.Description} binary={actual.IsBinary(op)} nullable={actual.IsNullable(document.SchemaType)}");
                                foreach (var content in actual.Content)
                                {
                                    Line($"content {content.Key}");
                                    Indent(() => DumpSchemaOrNull(content.Value.Schema));
                                }
                            }
                        });
                    }
                });
            }

            foreach (var definition in document.Definitions)
            {
                Line($"definition {definition.Key}");
                Indent(() => DumpSchema(definition.Value, 0, skipName: true));
            }

            return Output.ToString();
        }

        private static string StyleName(ApiParameterStyle style) =>
            style == ApiParameterStyle.SpaceDelimited ? "SpaceDelimeted" : style.ToString();

        private static string FormatName(ApiParameterCollectionFormat format) => format.ToString();

        private void DumpSchemaOrNull(ApiSchema? schema)
        {
            if (schema == null)
                Line("<null>");
            else
                DumpSchema(schema, 0);
        }

        private void DumpSchema(ApiSchema schema, int level, bool skipName = false)
        {
            if (!skipName && names.TryGetValue(schema, out var name))
            {
                Line($"#{name}");
                return;
            }

            if (level > 12)
            {
                Line("...");
                return;
            }

            var property = schema as ApiSchemaProperty;
            Line($"schema type={TypeName(schema.Type)} format={schema.Format} title={schema.Title} nullableRaw={schema.IsNullableRaw} nullable3={schema.IsNullable(ApiSchemaType.OpenApi3)} nullable2={schema.IsNullable(ApiSchemaType.Swagger2)} deprecated={schema.IsDeprecated} abstract={schema.IsAbstract} addl={schema.AllowAdditionalProperties} required={string.Join(",", schema.RequiredProperties)}" +
                 (property != null ? $" readOnly={property.IsReadOnly} writeOnly={property.IsWriteOnly} isRequired={property.IsRequired}" : string.Empty));
            Indent(() =>
            {
                if (schema.Description != null)
                    Line($"description {schema.Description}");
                if (schema.Default != null)
                    Line($"default {Value(schema.Default)}");
                if (schema.Example != null)
                    Line($"example {Value(schema.Example)}");
                if (schema.Minimum != null || schema.Maximum != null || schema.MinLength != null || schema.MaxLength != null || schema.Pattern != null || schema.MinItems != 0 || schema.MaxItems != 0 || schema.MultipleOf != null || schema.IsExclusiveMaximum || schema.IsExclusiveMinimum || schema.ExclusiveMaximum != null || schema.ExclusiveMinimum != null)
                    Line($"bounds {schema.Minimum} {schema.Maximum} {schema.IsExclusiveMinimum} {schema.IsExclusiveMaximum} {schema.ExclusiveMinimum} {schema.ExclusiveMaximum} {schema.MinLength} {schema.MaxLength} {schema.Pattern} {schema.MinItems} {schema.MaxItems} {schema.MultipleOf}");
                if (schema.Enumeration.Count > 0)
                    Line($"enum {string.Join("|", schema.Enumeration.Select(Value))}");
                if (schema.EnumerationNames.Count > 0)
                    Line($"enumNames {string.Join("|", schema.EnumerationNames)}");
                if (schema.EnumerationDescriptions.Count > 0)
                    Line($"enumDescriptions {string.Join("|", schema.EnumerationDescriptions)}");
                if (schema.IsFlagEnumerable)
                    Line("flags");
                if (schema.ExtensionData?.Count > 0)
                    Line($"extensions {string.Join(",", schema.ExtensionData.Select(e => e.Key + "=" + (IsSchemaLike(e.Value) ? "(schema)" : e.Value is RawJsonObject || e.Value is RawJsonArray ? "(complex)" : Value(e.Value))))}");
                if (schema.DiscriminatorObject != null)
                {
                    Line($"discriminator {schema.DiscriminatorObject.PropertyName}");
                    foreach (var mapping in schema.DiscriminatorObject.Mapping)
                    {
                        Line($"mapping {mapping.Key}");
                        Indent(() => DumpSchema(mapping.Value, level + 1));
                    }
                }

                if (schema.Reference != null)
                {
                    Line("reference");
                    Indent(() => DumpSchema(schema.Reference, level + 1));
                }

                foreach (var p in schema.Properties)
                {
                    Line($"property {p.Key}");
                    Indent(() => DumpSchema(p.Value, level + 1));
                }

                if (schema.Item != null)
                {
                    Line("item");
                    Indent(() => DumpSchema(schema.Item, level + 1));
                }

                foreach (var item in schema.Items)
                {
                    Line("tupleItem");
                    Indent(() => DumpSchema(item, level + 1));
                }

                if (schema.AdditionalPropertiesSchema != null)
                {
                    Line("additionalProperties");
                    Indent(() => DumpSchema(schema.AdditionalPropertiesSchema, level + 1));
                }

                foreach (var s in schema.AllOf)
                {
                    Line("allOf");
                    Indent(() => DumpSchema(s, level + 1));
                }

                foreach (var s in schema.OneOf)
                {
                    Line("oneOf");
                    Indent(() => DumpSchema(s, level + 1));
                }

                foreach (var s in schema.AnyOf)
                {
                    Line("anyOf");
                    Indent(() => DumpSchema(s, level + 1));
                }

                if (schema.Not != null)
                {
                    Line("not");
                    Indent(() => DumpSchema(schema.Not, level + 1));
                }
            });
        }

        private static bool IsSchemaLike(object? value) =>
            value is RawJsonObject obj &&
            (obj.TryGetValue("type", out _) || obj.TryGetValue("properties", out _)) &&
            !(obj.TryGetValue("required", out var required) && required is bool);

        private static string TypeName(ApiObjectType type) => ((JsonObjectType)(int)type).ToString();
    }
}
