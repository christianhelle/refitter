using System.Text;
using Refitter.Core.Settings;

namespace Refitter.Core;

internal interface IApizrOptionsBuilder
{
    void WithBaseAddress(string baseUrl, string duplicateStrategy);
    void WithDelegatingHandler(string handlerType);
    void ConfigureHttpClientBuilder(Action<StringBuilder> configure);
    void AppendOptionsCode(string code);
    void AddPackage(ApizrPackages package);
    void AddUsing(string usingDirective);
    bool HasOptions { get; }
    string BuildOptionsCode();
    string GetUsings();
    List<ApizrPackages> GetPackages();
}
