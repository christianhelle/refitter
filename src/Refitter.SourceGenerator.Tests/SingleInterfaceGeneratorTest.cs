﻿using FluentAssertions;
using Refit;
using Refitter.Tests.AdditionalFiles.SingeInterface;
using Xunit;

namespace Refitter.SourceGenerators.Tests;

public class SingleInterfaceGeneratorTest
{
    [Fact]
    public void Should_Type_Exist() =>
        typeof(ISwaggerPetstoreInterface)
            .Namespace
            .Should()
            .Be("Refitter.Tests.AdditionalFiles.SingeInterface");

    [Fact]
    public void Can_Resolve_Refit_Interface() =>
        RestService.For<ISwaggerPetstoreInterface>("https://petstore3.swagger.io/api/v3")
            .Should()
            .NotBeNull();
}