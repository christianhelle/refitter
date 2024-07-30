namespace Refitter.Tests.Build;

public static class ProjectFileContents
{
    public const string Net70App = @"
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net70</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""Refit"" Version=""6.3.2"" />
    <PackageReference Include=""Newtonsoft.Json"" Version=""13.0.1"" />
    <PackageReference Include=""System.ComponentModel.Annotations"" Version=""4.5.0"" />
    <PackageReference Include=""System.Runtime.Serialization.Primitives"" Version=""4.3.0"" />
    <PackageReference Include=""Microsoft.Extensions.DependencyInjection"" Version=""7.0.0"" />
    <PackageReference Include=""Microsoft.Extensions.Http.Polly"" Version=""7.0.4"" />
    <PackageReference Include=""Microsoft.Extensions.Options.ConfigurationExtensions"" Version=""7.0.0"" />
    <PackageReference Include=""Polly"" Version=""7.2.3"" />
    <PackageReference Include=""Polly.Contrib.WaitAndRetry"" Version=""1.1.1"" />
    <PackageReference Include=""Polly.Extensions.Http"" Version=""3.0.0"" />
    <PackageReference Include=""Refit.HttpClientFactory"" Version=""6.3.2"" />
    <PackageReference Include=""System.Reactive"" Version=""6.0.0"" />
  </ItemGroup>
</Project>";

    public const string Net80ApizrApp = @"
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""System.ComponentModel.Annotations"" Version=""4.5.0"" />
    <PackageReference Include=""System.Runtime.Serialization.Primitives"" Version=""4.3.0"" />
    <PackageReference Include=""Microsoft.Extensions.DependencyInjection"" Version=""8.0.0"" />
    <PackageReference Include=""Microsoft.Extensions.Http.Polly"" Version=""8.0.7"" />
    <PackageReference Include=""Microsoft.Extensions.Http.Resilience"" Version=""8.7.0"" />
    <PackageReference Include=""Microsoft.Extensions.Options.ConfigurationExtensions"" Version=""8.0.0"" />
    <PackageReference Include=""Polly.Contrib.WaitAndRetry"" Version=""1.1.1"" />
    <PackageReference Include=""System.Reactive"" Version=""6.0.1"" />
    <PackageReference Include=""Apizr.Integrations.FileTransfer.Optional"" Version=""6.0.0-preview.7"" />
    <PackageReference Include=""Apizr.Integrations.Mapster"" Version=""6.0.0-preview.7"" />
    <PackageReference Include=""Apizr.Integrations.AutoMapper"" Version=""6.0.0-preview.7"" />
    <PackageReference Include=""Apizr.Integrations.Akavache"" Version=""6.0.0-preview.7"" />
    <PackageReference Include=""Apizr.Integrations.MonkeyCache"" Version=""6.0.0-preview.7"" />
    <PackageReference Include=""Apizr.Extensions.Microsoft.Caching"" Version=""6.0.0-preview.7"" />
    <PackageReference Include=""Apizr.Integrations.Fusillade"" Version=""6.0.0-preview.7"" />
  </ItemGroup>
</Project>";
}