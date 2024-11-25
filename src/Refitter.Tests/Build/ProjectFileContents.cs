namespace Refitter.Tests.Build;

public static class ProjectFileContents
{
    public const string Net80App = @"
<Project Sdk=""Microsoft.NET.Sdk"">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include=""System.Text.Json"" Version=""8.0.5"" />
    <PackageReference Include=""System.ComponentModel.Annotations"" Version=""4.5.0"" />
    <PackageReference Include=""System.Runtime.Serialization.Primitives"" Version=""4.3.0"" />
    <PackageReference Include=""Microsoft.Extensions.DependencyInjection"" Version=""8.0.1"" />
    <PackageReference Include=""Microsoft.Extensions.Http.Polly"" Version=""8.0.11"" />
    <PackageReference Include=""Microsoft.Extensions.Http.Resilience"" Version=""8.10.0"" />
    <PackageReference Include=""Microsoft.Extensions.Options.ConfigurationExtensions"" Version=""8.0.0"" />
    <PackageReference Include=""Polly.Contrib.WaitAndRetry"" Version=""1.1.1"" />
    <PackageReference Include=""System.Reactive"" Version=""6.0.1"" />
    <PackageReference Include=""Apizr.Integrations.FileTransfer.MediatR"" Version=""6.4.0"" />
    <PackageReference Include=""Apizr.Integrations.Mapster"" Version=""6.4.0"" />
    <PackageReference Include=""Apizr.Integrations.AutoMapper"" Version=""6.4.0"" />
    <PackageReference Include=""Apizr.Integrations.Akavache"" Version=""6.4.0"" />
    <PackageReference Include=""Apizr.Integrations.MonkeyCache"" Version=""6.4.0"" />
    <PackageReference Include=""Apizr.Extensions.Microsoft.Caching"" Version=""6.4.0"" />
    <PackageReference Include=""Apizr.Integrations.Fusillade"" Version=""6.4.0"" />
  </ItemGroup>
</Project>";
}
