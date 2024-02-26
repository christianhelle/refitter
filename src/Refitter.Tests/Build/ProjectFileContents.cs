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
}