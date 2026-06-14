using System.Text;
using Refitter.Core.Settings;

namespace Refitter.Core;

internal class ApizrOptionsBuilder : IApizrOptionsBuilder
{
    private readonly StringBuilder _optionsCode;
    private readonly StringBuilder _usingsCode;
    private readonly List<ApizrPackages> _packages = new();
    private bool _hasOptions;

    public ApizrOptionsBuilder(string initialOptionsCode, string initialUsings)
    {
        _optionsCode = new StringBuilder(initialOptionsCode);
        _usingsCode = new StringBuilder(initialUsings);
        _usingsCode.AppendLine();
    }

    public bool HasOptions => _hasOptions;

    public void WithBaseAddress(string baseUrl, string duplicateStrategy)
    {
        _optionsCode.AppendLine();
        _optionsCode.Append($"                .WithBaseAddress(\"{baseUrl}\", {duplicateStrategy})");
        _hasOptions = true;
    }

    public void WithDelegatingHandler(string handlerType)
    {
        _optionsCode.AppendLine();
        _optionsCode.Append($"                .WithDelegatingHandler<{handlerType}>()");
        _hasOptions = true;
    }

    public void ConfigureHttpClientBuilder(Action<StringBuilder> configure)
    {
        _optionsCode.AppendLine();
        configure(_optionsCode);
        _hasOptions = true;
    }

    public void AppendOptionsCode(string code)
    {
        _optionsCode.Append(code);
        _hasOptions = true;
    }

    public void AddPackage(ApizrPackages package)
    {
        _packages.Add(package);
    }

    public void AddUsing(string usingDirective)
    {
        _usingsCode.Append("    ");
        _usingsCode.AppendLine(usingDirective);
    }

    public string BuildOptionsCode()
    {
        if (_hasOptions)
            _optionsCode.Append(";");
        else
            _optionsCode.Clear();

        return _optionsCode.ToString();
    }

    public string GetUsings() => _usingsCode.ToString();

    public List<ApizrPackages> GetPackages() => _packages;
}
