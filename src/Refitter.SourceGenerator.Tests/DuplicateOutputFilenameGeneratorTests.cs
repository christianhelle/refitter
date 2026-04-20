using FluentAssertions;
using Refit;
using Refitter.Tests.AdditionalFiles.DuplicateOutput.One;
using Refitter.Tests.AdditionalFiles.DuplicateOutput.Two;
using TUnit.Core;

namespace Refitter.SourceGenerators.Tests;

public class DuplicateOutputFilenameGeneratorTests
{
    [Test]
    public void Should_Generate_Both_Types_When_Output_Filenames_Collide_In_Same_Directory()
    {
        typeof(IDuplicateOutputOneApi).Namespace.Should().Be("Refitter.Tests.AdditionalFiles.DuplicateOutput.One");
        typeof(IDuplicateOutputTwoApi).Namespace.Should().Be("Refitter.Tests.AdditionalFiles.DuplicateOutput.Two");
    }

    [Test]
    public void Generated_Types_Should_Contain_Refit_Attributes()
    {
        var firstHasRefitAttributes = typeof(IDuplicateOutputOneApi)
            .GetMethods()
            .SelectMany(m => m.GetCustomAttributes(inherit: false))
            .Any(a => a is HttpMethodAttribute);

        var secondHasRefitAttributes = typeof(IDuplicateOutputTwoApi)
            .GetMethods()
            .SelectMany(m => m.GetCustomAttributes(inherit: false))
            .Any(a => a is HttpMethodAttribute);

        firstHasRefitAttributes.Should().BeTrue();
        secondHasRefitAttributes.Should().BeTrue();
    }
}
