using System.Text;

namespace Refitter.Core;

internal class ApizrOptionsBuilder : IApizrOptionsBuilder
{
    private readonly StringBuilder optionsCode;
    private readonly StringBuilder usingsCode;
    private readonly HashSet<ApizrPackages> packages = new();
    private readonly HashSet<string> usingDirectives = new(StringComparer.Ordinal);
    private bool hasOptions;

    public ApizrOptionsBuilder(string initialOptionsCode, string initialUsings)
    {
        optionsCode = new StringBuilder(initialOptionsCode);
        usingsCode = new StringBuilder(initialUsings);
        usingsCode.AppendLine();

        foreach (var line in initialUsings.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
        {
            usingDirectives.Add(line.Trim());
        }
    }

    public bool HasOptions => hasOptions;

    public void WithBaseAddress(string baseUrl, string duplicateStrategy)
    {
        optionsCode.AppendLine();
        optionsCode.Append($"                .WithBaseAddress(\"{baseUrl}\", {duplicateStrategy})");
        hasOptions = true;
    }

    public void WithDelegatingHandler(string handlerType)
    {
        optionsCode.AppendLine();
        optionsCode.Append($"                .WithDelegatingHandler<{handlerType}>()");
        hasOptions = true;
    }

    public void ConfigureHttpClientBuilder(Action<StringBuilder> configure)
    {
        optionsCode.AppendLine();
        configure(optionsCode);
        hasOptions = true;
    }

    public void AppendOptionsCode(string code)
    {
        optionsCode.Append(code);
        hasOptions = true;
    }

    public void AddPackage(ApizrPackages package)
    {
        packages.Add(package);
    }

    public void AddUsing(string usingDirective)
    {
        if (!usingDirectives.Add(usingDirective.Trim()))
            return;

        usingsCode.Append("    ");
        usingsCode.AppendLine(usingDirective);
    }

    public string BuildOptionsCode()
    {
        if (hasOptions)
            optionsCode.Append(";");
        else
            optionsCode.Clear();

        return optionsCode.ToString();
    }

    public string GetUsings() => usingsCode.ToString();

    public List<ApizrPackages> GetPackages() => packages.ToList();
}
