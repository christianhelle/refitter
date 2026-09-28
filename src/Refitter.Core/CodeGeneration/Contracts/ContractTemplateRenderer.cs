using System.Collections.Concurrent;
using System.Globalization;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using Fluid;
using Fluid.Ast;
using Fluid.Values;
using Parlot.Fluent;

namespace Refitter.Core;

/// <summary>
/// Renders the Liquid templates the contracts are generated from (see Templates). Templates are looked up in
/// <see cref="ContractGeneratorSettings.TemplateDirectory"/> first. See THIRD-PARTY-NOTICES.md.
/// </summary>
internal sealed class ContractTemplateRenderer
{
    /// <summary>The Refitter version shown in the generated code attributes.</summary>
    internal static readonly string ToolchainVersion =
        typeof(ContractTemplateRenderer).Assembly.GetName().Version?.ToString() ?? string.Empty;

    private const string SettingsKey = "__settings";
    private const string TemplateKey = "__template";
    private const string EmptyTemplateMarker = "__EMPTY-TEMPLATE__";

    private static readonly Regex TabCountRegex = new(
        @"(\s*)?\{%(-)?\s+template\s+([a-zA-Z0-9_.]+)(\s*?.*?)\s(-)?%}",
        RegexOptions.Compiled | RegexOptions.Singleline,
        TimeSpan.FromSeconds(5));

    private static readonly Regex CSharpDocsRegex = new(
        "(\n( )*)([^\n]*?) \\| csharpdocs }}",
        RegexOptions.Compiled | RegexOptions.Singleline,
        TimeSpan.FromSeconds(5));

    private static readonly Regex TabRegex = new(
        "(\n( )*)([^\n]*?) \\| tab }}",
        RegexOptions.Compiled | RegexOptions.Singleline,
        TimeSpan.FromSeconds(5));

    private static readonly Regex EmptyTemplateCleanupRegex = new(
        "^[ ]+__EMPTY-TEMPLATE__$[\\n]{0,1}",
        RegexOptions.Multiline | RegexOptions.Compiled,
        TimeSpan.FromSeconds(5));

    private static readonly ConcurrentDictionary<(string Template, string Directory), IFluidTemplate> Templates = new();

    private static readonly ContractTemplateParser Parser = new();

    private static readonly TemplateOptions Options = CreateOptions();

    private readonly ContractGeneratorSettings settings;

    public ContractTemplateRenderer(ContractGeneratorSettings settings)
    {
        this.settings = settings;
    }

    public string Render(string template, object model)
    {
        var context = new TemplateContext(model, Options);
        context.AmbientValues.Add(SettingsKey, this);
        context.SetValue("ToolchainVersion", ToolchainVersion);
        return Render(template, context);
    }

    private string Render(string template, TemplateContext context)
    {
        try
        {
            var templateDirectory = settings.TemplateDirectory ?? string.Empty;
            var fluidTemplate = Templates.GetOrAdd(
                (template, templateDirectory),
                _ => Parser.Parse(Preprocess(LoadTemplate(template, templateDirectory))));

            context.AmbientValues[TemplateKey] = template;
            var output = fluidTemplate.Render(context).Replace("\r", string.Empty).Trim('\n');
            return EmptyTemplateCleanupRegex
                .Replace(output, string.Empty)
                .Replace("\n" + EmptyTemplateMarker + "\n", "\n")
                .Replace(EmptyTemplateMarker, string.Empty);
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException(
                "Error while rendering Liquid template CSharp/" + template + ": \n" + exception.Message,
                exception);
        }
    }

    private static string Preprocess(string content)
    {
        var text = "\n" + content.Replace("\r", string.Empty);
        text = TabCountRegex.Replace(text, match =>
        {
            var whitespace = match.Groups[1].Value;
            var tag = whitespace + "{%" + match.Groups[2].Value + " template '" + match.Groups[3].Value + "' ";
            if (whitespace.Length > 0 && whitespace[0] == '\n')
                tag += whitespace.TrimStart('\n').Length / 4 + " ";

            return tag + match.Groups[5].Value + "%}";
        });

        text = CSharpDocsRegex.Replace(
            text,
            m => m.Groups[1].Value + m.Groups[3].Value + " | csharpdocs: " + m.Groups[1].Value.Length / 4 + " }}");
        text = TabRegex.Replace(
            text,
            m => m.Groups[1].Value + m.Groups[3].Value + " | tab: " + m.Groups[1].Value.Length / 4 + " }}");
        return text;
    }

    private static string LoadTemplate(string template, string templateDirectory)
    {
        if (!template.EndsWith("!", StringComparison.Ordinal) && !string.IsNullOrEmpty(templateDirectory))
        {
            foreach (var directory in templateDirectory.Split([';'], StringSplitOptions.RemoveEmptyEntries))
            {
                var path = Path.Combine(directory, template + ".liquid");
                if (File.Exists(path))
                    return File.ReadAllText(path);
            }
        }

        var name = "Refitter.Core.Templates." + template.TrimEnd('!') + ".liquid";
        using var stream = typeof(ContractTemplateRenderer).Assembly.GetManifestResourceStream(name)
            ?? throw new InvalidOperationException("Could not load template '" + template.TrimEnd('!') + "' for language 'CSharp'.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    private static TemplateOptions CreateOptions()
    {
        var options = new TemplateOptions
        {
            MemberAccessStrategy = new UnsafeMemberAccessStrategy(),
            CultureInfo = CultureInfo.InvariantCulture,
            Greedy = false,
        };

        options.Filters.AddFilter("csharpdocs", (input, arguments, _) =>
            new ValueTask<FluidValue>(new StringValue(
                ConversionUtilities.ConvertCSharpDocs(input.ToStringValue(), (int)arguments.At(0).ToNumberValue()))));
        options.Filters.AddFilter("tab", (input, arguments, _) =>
            new ValueTask<FluidValue>(new StringValue(
                ConversionUtilities.Tab(input.ToStringValue(), (int)arguments.At(0).ToNumberValue()))));
        options.Filters.AddFilter("lowercamelcase", (input, arguments, _) =>
            new ValueTask<FluidValue>(new StringValue(
                ConversionUtilities.ConvertToLowerCamelCase(input.ToStringValue(), arguments["firstCharacterMustBeAlpha"].ToBooleanValue()))));
        options.Filters.AddFilter("uppercamelcase", (input, arguments, _) =>
            new ValueTask<FluidValue>(new StringValue(
                ConversionUtilities.ConvertToUpperCamelCase(input.ToStringValue(), arguments["firstCharacterMustBeAlpha"].ToBooleanValue()))));
        options.Filters.AddFilter("literal", (input, _, _) =>
            new ValueTask<FluidValue>(new StringValue(
                ConversionUtilities.ConvertToStringLiteral(input.ToStringValue(), "\"", "\""),
                encode: false)));
        options.Filters.AddFilter("rtrimquestionmark", (input, _, _) =>
            new ValueTask<FluidValue>(new StringValue(input.ToStringValue().TrimEnd('?'), encode: false)));
        return options;
    }

    private sealed class ContractTemplateParser : FluidParser
    {
        public ContractTemplateParser()
            : base(new FluidParserOptions())
        {
            RegisterParserTag("template", Parsers.OneOrMany(Primary), RenderTemplate);
        }

        private static ValueTask<Completion> RenderTemplate(
            IReadOnlyList<Expression> arguments,
            TextWriter writer,
            TextEncoder encoder,
            TemplateContext context)
        {
            var template = ((LiteralExpression)arguments[0]).Value.ToStringValue();
            var tabCount = -1;
            if (arguments.Count > 1 && arguments[1] is LiteralExpression literal)
                tabCount = (int)literal.Value.ToNumberValue();

            var renderer = (ContractTemplateRenderer)context.AmbientValues[SettingsKey];
            var currentTemplate = (string)context.AmbientValues[TemplateKey];
            template = !string.IsNullOrEmpty(template) ? template : currentTemplate + "!";

            string output;
            context.EnterChildScope();
            try
            {
                output = renderer.Render(template, context);
            }
            finally
            {
                context.ReleaseScope();
                context.AmbientValues[TemplateKey] = currentTemplate;
            }

            if (string.IsNullOrWhiteSpace(output))
            {
                writer.Write(EmptyTemplateMarker);
            }
            else if (tabCount > 0)
            {
                writer.Write(ConversionUtilities.Tab(output, tabCount));
            }
            else
            {
                writer.Write(output);
            }

            return new ValueTask<Completion>(Completion.Normal);
        }
    }

    private sealed class UnsafeMemberAccessStrategy : DefaultMemberAccessStrategy
    {
        private readonly ConcurrentDictionary<Type, object?> handledTypes = new();
        private readonly MemberAccessStrategy baseMemberAccessStrategy = new DefaultMemberAccessStrategy();

        public override IMemberAccessor GetAccessor(Type type, string name)
        {
            var accessor = baseMemberAccessStrategy.GetAccessor(type, name);
            if (accessor != null)
                return accessor;

            if (!handledTypes.ContainsKey(type))
            {
                baseMemberAccessStrategy.Register(type);
                handledTypes.TryAdd(type, null);
            }

            return baseMemberAccessStrategy.GetAccessor(type, name);
        }
    }
}
