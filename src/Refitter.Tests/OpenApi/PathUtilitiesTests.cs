using FluentAssertions;
using Refitter.Core;

namespace Refitter.Tests.OpenApi;


public class PathUtilitiesTests
{
    [Test]
    [Arguments("http://example.com/spec.json")]
    [Arguments("https://example.com/spec.yaml")]
    [Arguments("HTTP://EXAMPLE.COM/SPEC.JSON")]
    [Arguments("https://example.com/path/to/spec.yml")]
    public void IsHttp_Detects_Http_Paths(string path)
    {
        PathUtilities.IsHttp(path).Should().BeTrue();
    }

    [Test]
    [Arguments("/local/path/spec.json")]
    [Arguments("C:\\local\\path\\spec.yaml")]
    [Arguments("relative/path/spec.json")]
    [Arguments("")]
    public void IsHttp_Returns_False_For_NonHttp_Paths(string path)
    {
        PathUtilities.IsHttp(path).Should().BeFalse();
    }

    [Test]
    [Arguments("spec.yaml")]
    [Arguments("spec.yml")]
    [Arguments("spec.YAML")]
    [Arguments("spec.YML")]
    [Arguments("/path/to/spec.yaml")]
    [Arguments("https://example.com/spec.yaml")]
    [Arguments("https://example.com/spec.yaml?query=param")]
    [Arguments("https://example.com/spec.yaml#fragment")]
    [Arguments("spec.YAML")]
    public void IsYaml_Detects_Yaml_Paths(string path)
    {
        PathUtilities.IsYaml(path).Should().BeTrue();
    }

    [Test]
    [Arguments("spec.json")]
    [Arguments("spec.xml")]
    [Arguments("/path/to/spec.json")]
    [Arguments("https://example.com/spec.json")]
    [Arguments("spec")]
    [Arguments("")]
    public void IsYaml_Returns_False_For_NonYaml_Paths(string path)
    {
        PathUtilities.IsYaml(path).Should().BeFalse();
    }
}
