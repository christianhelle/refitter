using AwesomeAssertions;
using Refitter.Core;
using TUnit.Core;

namespace Refitter.Tests;

public class ConstantsCompatibilityTests
{
    [Test]
#pragma warning disable CS0618
    public void RestoredCompatibilityConstantsKeepTheirPublicValues()
    {
        FileExtensionConstants.GeneratedCSharp.Should().Be(".g.cs");
        FileExtensionConstants.CSharp.Should().Be(".cs");
        ContentTypeConstants.Json.Should().Be("application/json");
        ContentTypeConstants.Xml.Should().Be("application/xml");
        PackageConstants.Polly.Should().Be("Polly");
        PackageConstants.Akavache.Should().Be("Akavache");
        PackageConstants.MonkeyCache.Should().Be("MonkeyCache");
        PackageConstants.AutoMapper.Should().Be("AutoMapper");
        PackageConstants.Mapster.Should().Be("Mapster");
        PackageConstants.MediatR.Should().Be("MediatR");
        DotNetTypeConstants.DateTimeOffset.Should().Be("System.DateTimeOffset");
        DotNetTypeConstants.TimeSpan.Should().Be("System.TimeSpan");
        DotNetTypeConstants.Dictionary.Should().Be("System.Collections.Generic.Dictionary");
        FilenameConstants.DefaultOutput.Should().Be("Output.cs");
        FilenameConstants.DefaultSettingsFile.Should().Be(".refitter");
    }
#pragma warning restore CS0618
}
