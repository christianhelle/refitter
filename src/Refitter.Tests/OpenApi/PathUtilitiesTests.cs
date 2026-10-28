using AwesomeAssertions;
using Refitter.Core;

namespace Refitter.Tests.OpenApi;


[TestClass]
public class PathUtilitiesTests
{
    [TestMethod]
    [DataRow("http://example.com/spec.json")]
    [DataRow("https://example.com/spec.yaml")]
    [DataRow("HTTP://EXAMPLE.COM/SPEC.JSON")]
    [DataRow("https://example.com/path/to/spec.yml")]
    public void IsHttp_Detects_Http_Paths(string path)
    {
        PathUtilities.IsHttp(path).Should().BeTrue();
    }

    [TestMethod]
    [DataRow("/local/path/spec.json")]
    [DataRow("C:\\local\\path\\spec.yaml")]
    [DataRow("relative/path/spec.json")]
    [DataRow("")]
    public void IsHttp_Returns_False_For_NonHttp_Paths(string path)
    {
        PathUtilities.IsHttp(path).Should().BeFalse();
    }

    [TestMethod]
    [DataRow("spec.yaml")]
    [DataRow("spec.yml")]
    [DataRow("spec.YAML")]
    [DataRow("spec.YML")]
    [DataRow("/path/to/spec.yaml")]
    [DataRow("https://example.com/spec.yaml")]
    [DataRow("https://example.com/spec.yaml?query=param")]
    [DataRow("https://example.com/spec.yaml#fragment")]
    public void IsYaml_Detects_Yaml_Paths(string path)
    {
        PathUtilities.IsYaml(path).Should().BeTrue();
    }

    [TestMethod]
    [DataRow("spec.json")]
    [DataRow("spec.xml")]
    [DataRow("/path/to/spec.json")]
    [DataRow("https://example.com/spec.json")]
    [DataRow("spec")]
    [DataRow("")]
    public void IsYaml_Returns_False_For_NonYaml_Paths(string path)
    {
        PathUtilities.IsYaml(path).Should().BeFalse();
    }
}
