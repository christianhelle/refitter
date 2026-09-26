using System.Text;
using AwesomeAssertions;
using Refitter.Core;

namespace Refitter.Tests.Apizr;

public class ApizrOptionsBuilderTests
{
    [Test]
    public void HasOptions_Is_False_For_Fresh_Builder()
    {
        var builder = new ApizrOptionsBuilder(string.Empty, string.Empty);

        builder.HasOptions.Should().BeFalse();
    }

    [Test]
    public void HasOptions_Is_True_After_Adding_Base_Address()
    {
        var builder = new ApizrOptionsBuilder(string.Empty, string.Empty);

        builder.WithBaseAddress("https://example.com", "DuplicateStrategy.Ignore");

        builder.HasOptions.Should().BeTrue();
    }

    [Test]
    public void BuildOptionsCode_Clears_Code_When_No_Options_Were_Added()
    {
        var builder = new ApizrOptionsBuilder("pre-existing", string.Empty);

        var result = builder.BuildOptionsCode();

        result.Should().BeEmpty();
    }

    [Test]
    public void BuildOptionsCode_Appends_Semicolon_When_Options_Were_Added()
    {
        var builder = new ApizrOptionsBuilder(string.Empty, string.Empty);
        builder.AppendOptionsCode("options");

        var result = builder.BuildOptionsCode();

        result.Should().Be("options;");
    }

    [Test]
    public void AddUsing_Appends_Indented_Using_Directive()
    {
        var builder = new ApizrOptionsBuilder(string.Empty, string.Empty);

        builder.AddUsing("using Refit;");

        builder.GetUsings().Should().Contain("    using Refit;");
    }

    [Test]
    public void AddUsing_Ignores_Directive_Already_Added()
    {
        var builder = new ApizrOptionsBuilder(string.Empty, string.Empty);

        builder.AddUsing("using Refit;");
        builder.AddUsing("using Refit;");

        builder.GetUsings().Should().Be(Environment.NewLine + "    using Refit;" + Environment.NewLine);
    }

    [Test]
    public void AddUsing_Ignores_Directive_In_Initial_Usings()
    {
        var initialUsings = "using System;" + Environment.NewLine + "    using Polly;";
        var builder = new ApizrOptionsBuilder(string.Empty, initialUsings);

        builder.AddUsing("using Polly;");

        builder.GetUsings().Should().Be(initialUsings + Environment.NewLine);
    }

    [Test]
    public void AddPackage_Collects_Distinct_Packages()
    {
        var builder = new ApizrOptionsBuilder(string.Empty, string.Empty);

        builder.AddPackage(ApizrPackages.Apizr);
        builder.AddPackage(ApizrPackages.Apizr);
        builder.AddPackage(ApizrPackages.Apizr_Integrations_Akavache);

        builder.GetPackages().Should().HaveCount(2);
    }

    [Test]
    public void ConfigureHttpClientBuilder_Invokes_Configuration_And_Marks_Options()
    {
        var builder = new ApizrOptionsBuilder(string.Empty, string.Empty);

        builder.ConfigureHttpClientBuilder(sb => sb.Append("configured"));

        builder.HasOptions.Should().BeTrue();
        builder.BuildOptionsCode().Should().Contain("configured");
    }

    [Test]
    public void WithDelegatingHandler_Emits_Generic_Handler_Registration()
    {
        var builder = new ApizrOptionsBuilder(string.Empty, string.Empty);

        builder.WithDelegatingHandler("MyHandler");

        builder.BuildOptionsCode().Should().Contain("WithDelegatingHandler<MyHandler>()");
    }

    [Test]
    public void BuildOptionsCode_Starts_From_Initial_Options_Code()
    {
        var builder = new ApizrOptionsBuilder("initial", string.Empty);
        builder.AppendOptionsCode("more");

        builder.BuildOptionsCode().Should().StartWith("initial");
    }

    [Test]
    public void GetUsings_Terminates_Initial_Usings_With_New_Line()
    {
        var builder = new ApizrOptionsBuilder(string.Empty, "using System;");

        builder.GetUsings().Should().Be("using System;" + Environment.NewLine);
    }

    [Test]
    public void ConfigureHttpClientBuilder_Receives_Builder_For_Appending()
    {
        var builder = new ApizrOptionsBuilder(string.Empty, string.Empty);

        builder.ConfigureHttpClientBuilder(sb => sb.Append(new StringBuilder().Append("nested")));

        builder.BuildOptionsCode().Should().Contain("nested");
    }
}
