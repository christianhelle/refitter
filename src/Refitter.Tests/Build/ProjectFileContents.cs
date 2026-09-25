using System.Text.RegularExpressions;

namespace Refitter.Tests.Build;

public static class ProjectFileContents
{
    public const string Net80App =
        @"
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <NoWarn>NU1510;NU1903;CS1573;CS1591;SYSLIB1034</NoWarn>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""Refit.HttpClientFactory"" Version=""16.1.0"" />
    <PackageReference Include=""System.Text.Json"" Version=""10.0.12"" />
    <PackageReference Include=""System.ComponentModel.Annotations"" Version=""5.0.0"" />
    <PackageReference Include=""System.Runtime.Serialization.Primitives"" Version=""4.3.0"" />
    <PackageReference Include=""Microsoft.Extensions.DependencyInjection"" Version=""10.0.12"" />
    <PackageReference Include=""Microsoft.Extensions.Http.Polly"" Version=""10.0.12"" />
    <PackageReference Include=""Microsoft.Extensions.Http.Resilience"" Version=""10.10.0"" />
    <PackageReference Include=""Microsoft.Extensions.Options.ConfigurationExtensions"" Version=""10.0.12"" />
    <PackageReference Include=""Polly.Contrib.WaitAndRetry"" Version=""1.1.1"" />
    <PackageReference Include=""System.Reactive"" Version=""7.0.0"" />
    <PackageReference Include=""Apizr.Integrations.FileTransfer.MediatR"" Version=""6.4.2"" />
    <PackageReference Include=""Apizr.Integrations.Mapster"" Version=""6.4.2"" />
    <PackageReference Include=""Apizr.Integrations.AutoMapper"" Version=""6.4.2"" />
    <PackageReference Include=""Apizr.Integrations.Akavache"" Version=""6.4.2"" />
    <PackageReference Include=""Apizr.Integrations.MonkeyCache"" Version=""6.4.2"" />
    <PackageReference Include=""Apizr.Extensions.Microsoft.Caching"" Version=""6.4.2"" />
    <PackageReference Include=""Apizr.Integrations.Fusillade"" Version=""6.4.2"" />
  </ItemGroup>
</Project>";

    public const string Net80AppWithWarningsAsErrors =
        @"
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <NoWarn>NU1510;NU1903;CS1573;CS1591;SYSLIB1034</NoWarn>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""Refit.HttpClientFactory"" Version=""16.1.0"" />
    <PackageReference Include=""System.Text.Json"" Version=""10.0.12"" />
    <PackageReference Include=""System.ComponentModel.Annotations"" Version=""5.0.0"" />
    <PackageReference Include=""System.Runtime.Serialization.Primitives"" Version=""4.3.0"" />
    <PackageReference Include=""Microsoft.Extensions.DependencyInjection"" Version=""10.0.12"" />
    <PackageReference Include=""Microsoft.Extensions.Http.Polly"" Version=""10.0.12"" />
    <PackageReference Include=""Microsoft.Extensions.Http.Resilience"" Version=""10.10.0"" />
    <PackageReference Include=""Microsoft.Extensions.Options.ConfigurationExtensions"" Version=""10.0.12"" />
    <PackageReference Include=""Polly.Contrib.WaitAndRetry"" Version=""1.1.1"" />
    <PackageReference Include=""System.Reactive"" Version=""7.0.0"" />
    <PackageReference Include=""Apizr.Integrations.FileTransfer.MediatR"" Version=""6.4.2"" />
    <PackageReference Include=""Apizr.Integrations.Mapster"" Version=""6.4.2"" />
    <PackageReference Include=""Apizr.Integrations.AutoMapper"" Version=""6.4.2"" />
    <PackageReference Include=""Apizr.Integrations.Akavache"" Version=""6.4.2"" />
    <PackageReference Include=""Apizr.Integrations.MonkeyCache"" Version=""6.4.2"" />
    <PackageReference Include=""Apizr.Extensions.Microsoft.Caching"" Version=""6.4.2"" />
    <PackageReference Include=""Apizr.Integrations.Fusillade"" Version=""6.4.2"" />
  </ItemGroup>
</Project>";

    public const string Net90App =
        @"
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <NoWarn>NU1510;NU1903;CS1573;CS1591;SYSLIB1034</NoWarn>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""Refit.HttpClientFactory"" Version=""16.1.0"" />
    <PackageReference Include=""System.Text.Json"" Version=""10.0.12"" />
    <PackageReference Include=""System.ComponentModel.Annotations"" Version=""5.0.0"" />
    <PackageReference Include=""System.Runtime.Serialization.Primitives"" Version=""4.3.0"" />
    <PackageReference Include=""Microsoft.Extensions.DependencyInjection"" Version=""10.0.12"" />
    <PackageReference Include=""Microsoft.Extensions.Http.Polly"" Version=""10.0.12"" />
    <PackageReference Include=""Microsoft.Extensions.Http.Resilience"" Version=""10.10.0"" />
    <PackageReference Include=""Microsoft.Extensions.Options.ConfigurationExtensions"" Version=""10.0.12"" />
    <PackageReference Include=""Polly.Contrib.WaitAndRetry"" Version=""1.1.1"" />
    <PackageReference Include=""System.Reactive"" Version=""7.0.0"" />
    <PackageReference Include=""Apizr.Integrations.FileTransfer.MediatR"" Version=""6.4.2"" />
    <PackageReference Include=""Apizr.Integrations.Mapster"" Version=""6.4.2"" />
    <PackageReference Include=""Apizr.Integrations.AutoMapper"" Version=""6.4.2"" />
    <PackageReference Include=""Apizr.Integrations.Akavache"" Version=""6.4.2"" />
    <PackageReference Include=""Apizr.Integrations.MonkeyCache"" Version=""6.4.2"" />
    <PackageReference Include=""Apizr.Extensions.Microsoft.Caching"" Version=""6.4.2"" />
    <PackageReference Include=""Apizr.Integrations.Fusillade"" Version=""6.4.2"" />
  </ItemGroup>
</Project>";

    public const string Net90AppWithWarningsAsErrors =
        @"
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <NoWarn>NU1510;NU1903;CS1573;CS1591;SYSLIB1034</NoWarn>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""Refit.HttpClientFactory"" Version=""16.1.0"" />
    <PackageReference Include=""System.Text.Json"" Version=""10.0.12"" />
    <PackageReference Include=""System.ComponentModel.Annotations"" Version=""5.0.0"" />
    <PackageReference Include=""System.Runtime.Serialization.Primitives"" Version=""4.3.0"" />
    <PackageReference Include=""Microsoft.Extensions.DependencyInjection"" Version=""10.0.12"" />
    <PackageReference Include=""Microsoft.Extensions.Http.Polly"" Version=""10.0.12"" />
    <PackageReference Include=""Microsoft.Extensions.Http.Resilience"" Version=""10.10.0"" />
    <PackageReference Include=""Microsoft.Extensions.Options.ConfigurationExtensions"" Version=""10.0.12"" />
    <PackageReference Include=""Polly.Contrib.WaitAndRetry"" Version=""1.1.1"" />
    <PackageReference Include=""System.Reactive"" Version=""7.0.0"" />
    <PackageReference Include=""Apizr.Integrations.FileTransfer.MediatR"" Version=""6.4.2"" />
    <PackageReference Include=""Apizr.Integrations.Mapster"" Version=""6.4.2"" />
    <PackageReference Include=""Apizr.Integrations.AutoMapper"" Version=""6.4.2"" />
    <PackageReference Include=""Apizr.Integrations.Akavache"" Version=""6.4.2"" />
    <PackageReference Include=""Apizr.Integrations.MonkeyCache"" Version=""6.4.2"" />
    <PackageReference Include=""Apizr.Extensions.Microsoft.Caching"" Version=""6.4.2"" />
    <PackageReference Include=""Apizr.Integrations.Fusillade"" Version=""6.4.2"" />
  </ItemGroup>
</Project>";

    public const string Net100App =
        @"
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <NoWarn>NU1510;NU1903;CS1573;CS1591;SYSLIB1034</NoWarn>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""Refit.HttpClientFactory"" Version=""16.1.0"" />
    <PackageReference Include=""System.Text.Json"" Version=""10.0.12"" />
    <PackageReference Include=""System.ComponentModel.Annotations"" Version=""5.0.0"" />
    <PackageReference Include=""System.Runtime.Serialization.Primitives"" Version=""4.3.0"" />
    <PackageReference Include=""Microsoft.Extensions.DependencyInjection"" Version=""10.0.12"" />
    <PackageReference Include=""Microsoft.Extensions.Http.Polly"" Version=""10.0.12"" />
    <PackageReference Include=""Microsoft.Extensions.Http.Resilience"" Version=""10.10.0"" />
    <PackageReference Include=""Microsoft.Extensions.Options.ConfigurationExtensions"" Version=""10.0.12"" />
    <PackageReference Include=""Polly.Contrib.WaitAndRetry"" Version=""1.1.1"" />
    <PackageReference Include=""System.Reactive"" Version=""7.0.0"" />
    <PackageReference Include=""Apizr.Integrations.FileTransfer.MediatR"" Version=""6.4.2"" />
    <PackageReference Include=""Apizr.Integrations.Mapster"" Version=""6.4.2"" />
    <PackageReference Include=""Apizr.Integrations.AutoMapper"" Version=""6.4.2"" />
    <PackageReference Include=""Apizr.Integrations.Akavache"" Version=""6.4.2"" />
    <PackageReference Include=""Apizr.Integrations.MonkeyCache"" Version=""6.4.2"" />
    <PackageReference Include=""Apizr.Extensions.Microsoft.Caching"" Version=""6.4.2"" />
    <PackageReference Include=""Apizr.Integrations.Fusillade"" Version=""6.4.2"" />
  </ItemGroup>
</Project>";

    public const string Net100AppWithWarningsAsErrors =
        @"
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <GenerateDocumentationFile>true</GenerateDocumentationFile>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <NoWarn>NU1510;NU1903;CS1573;CS1591;SYSLIB1034</NoWarn>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""Refit.HttpClientFactory"" Version=""16.1.0"" />
    <PackageReference Include=""System.Text.Json"" Version=""10.0.12"" />
    <PackageReference Include=""System.ComponentModel.Annotations"" Version=""5.0.0"" />
    <PackageReference Include=""System.Runtime.Serialization.Primitives"" Version=""4.3.0"" />
    <PackageReference Include=""Microsoft.Extensions.DependencyInjection"" Version=""10.0.12"" />
    <PackageReference Include=""Microsoft.Extensions.Http.Polly"" Version=""10.0.12"" />
    <PackageReference Include=""Microsoft.Extensions.Http.Resilience"" Version=""10.10.0"" />
    <PackageReference Include=""Microsoft.Extensions.Options.ConfigurationExtensions"" Version=""10.0.12"" />
    <PackageReference Include=""Polly.Contrib.WaitAndRetry"" Version=""1.1.1"" />
    <PackageReference Include=""System.Reactive"" Version=""7.0.0"" />
    <PackageReference Include=""Apizr.Integrations.FileTransfer.MediatR"" Version=""6.4.2"" />
    <PackageReference Include=""Apizr.Integrations.Mapster"" Version=""6.4.2"" />
    <PackageReference Include=""Apizr.Integrations.AutoMapper"" Version=""6.4.2"" />
    <PackageReference Include=""Apizr.Integrations.Akavache"" Version=""6.4.2"" />
    <PackageReference Include=""Apizr.Integrations.MonkeyCache"" Version=""6.4.2"" />
    <PackageReference Include=""Apizr.Extensions.Microsoft.Caching"" Version=""6.4.2"" />
    <PackageReference Include=""Apizr.Integrations.Fusillade"" Version=""6.4.2"" />
  </ItemGroup>
</Project>";

    // Apizr 6.x is compiled against the strong-named Refit 8.0.0 assembly, while newer Refit
    // releases are not strong-named, so Apizr output only compiles against Refit 8.
    // CS0105 is suppressed until https://github.com/christianhelle/refitter/issues/1264 is fixed.
    public static readonly string Net80ApizrApp = Regex
        .Replace(
            Net80App,
            @"(<PackageReference Include=""Refit\.HttpClientFactory"" Version="")[^""]+",
            "${1}8.0.0")
        .Replace("<NoWarn>NU1510;", "<NoWarn>CS0105;NU1510;");

    public static readonly string Net100Refit11App = Regex.Replace(
        Net100App,
        @"(<PackageReference Include=""Refit\.HttpClientFactory"" Version="")[^""]+",
        "${1}11.2.0");
}
