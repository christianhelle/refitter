## MSBuild

A common scenario for generating code from OpenAPI specifications is to do it at build time. This can be achieved using MSBuild tasks. An example of such an approach would be to include a `.refitter` file in the projects directory and execute the Refitter CLI from pre-build events

```xml
<Target Name="Refitter" AfterTargets="PreBuildEvent">
    <Exec WorkingDirectory="$(ProjectDir)" Command="refitter --settings-file .refitter --skip-validation" />
</Target>
```

The snippet above requires that Refitter is installed on the machine as a globally available dotnet tool. This might not be the case if you're running on a build agent from a CI/CD environment. In this case you might want to install Refitter as a local tool using a manifest file, as described in [this tutorial](https://learn.microsoft.com/en-us/dotnet/core/tools/local-tools-how-to-use?WT.mc_id=DT-MVP-5004822)

```xml
<Target Name="Refitter" AfterTargets="PreBuildEvent">
    <Exec WorkingDirectory="$(ProjectDir)" Command="dotnet tool restore" />
    <Exec WorkingDirectory="$(ProjectDir)" Command="refitter --settings-file .refitter --skip-validation" />
</Target>
```

The `dotnet build` process does will probably not have access to the package repository in which to download Refitter from, this is at least the case with Azure Pipelines and Azure Artifacts. To workaround this, you can provide a separate `nuget.config` that only uses `nuget.org` as a `<packageSource>`.

Something like this:

```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <clear />
    <add key="NuGet" value="https://api.nuget.org/v3/index.json" />
  </packageSources>
</configuration>
```

You might want to place the `nuget.config` file in another folder to avoid using it to build the .NET project, then you can specify this when executing `dotnet tool restore`

```xml
<Target Name="Refitter" AfterTargets="PreBuildEvent">
    <Exec WorkingDirectory="$(ProjectDir)" Command="dotnet tool restore --configfile refitter/nuget.config" />
    <Exec WorkingDirectory="$(ProjectDir)" Command="refitter --settings-file .refitter --skip-validation" />
</Target>
```

In the example above, the `nuget.config` file is placed under the `refitter` folder.

### Refitter.MSBuild package

Refitter ships with an MSBuild custom task that is distributed as a NuGet package and includes the Refitter CLI binary. This will simplify generating code from OpenAPI specifications at build time.

To use the package, install `Refitter.MSBuild`

```xml
<ItemGroup>
    <PackageReference Include="Refitter.MSBuild" Version="1.6.3" />
</ItemGroup>
```

The MSBuild package includes a custom `.target` file which executes the `RefitterGenerateTask` custom task and looks something like this:

```xml
<PropertyGroup>
    <RefitterAutoScan Condition="'$(RefitterAutoScan)' == ''">true</RefitterAutoScan>
</PropertyGroup>
<UsingTask TaskName="RefitterGenerateTask" 
           AssemblyFile="$(MSBuildThisFileDirectory)Refitter.MSBuild.dll" 
           Condition="Exists('$(MSBuildThisFileDirectory)Refitter.MSBuild.dll')" />
<PropertyGroup Condition="'$(RefitterOutputPerTargetFramework)' == '' and '$(TargetFramework)' != '' and $(TargetFrameworks.Contains(';'))">
    <RefitterOutputPerTargetFramework>true</RefitterOutputPerTargetFramework>
</PropertyGroup>
<Target Name="RefitterGenerate">
    <PropertyGroup>
        <_RefitterOutputRoot Condition="'$(RefitterOutputPerTargetFramework)' == 'true'">$([MSBuild]::NormalizeDirectory('$(MSBuildProjectDirectory)', '$(IntermediateOutputPath)', 'Refitter'))</_RefitterOutputRoot>
    </PropertyGroup>
    <RefitterGenerateTask ProjectFileDirectory="$(MSBuildProjectDirectory)"
                          DisableLogging="$(RefitterNoLogging)"
                          SkipValidation="$(RefitterSkipValidation)"
                          IncludePatterns="$(RefitterIncludePatterns)"
                          OutputRoot="$(_RefitterOutputRoot)">
        <Output TaskParameter="GeneratedFiles" ItemName="RefitterGeneratedFiles" />
        <Output TaskParameter="SupersededFiles" ItemName="RefitterSupersededFiles" />
    </RefitterGenerateTask>
    <ItemGroup>
        <Compile Remove="@(RefitterSupersededFiles)" MatchOnMetadata="FullPath" MatchOnMetadataOptions="PathLike" />
        <Compile Include="@(RefitterGeneratedFiles)" />
        <FileWrites Include="@(RefitterGeneratedFiles)" Condition="'$(_RefitterOutputRoot)' != ''" />
    </ItemGroup>
</Target>
<Target Name="_RefitterGenerateOnBuild"
        BeforeTargets="CoreCompile"
        DependsOnTargets="RefitterGenerate"
        Condition="'$(RefitterAutoScan)' != 'false'" />
```

The `RefitterGenerateTask` task scans the project folder for `.refitter` files and executes them all. `RefitterAutoScan` defaults to `true`, so regular builds keep their current behavior. Set `<RefitterAutoScan>false</RefitterAutoScan>` to disable the automatic build hook while still allowing explicit generation through `dotnet build -t:RefitterGenerate`.

By default, telemetry collection is enabled, and to opt-out of it you must specify `<RefitterNoLogging>true</RefitterNoLogging>` in the `.csproj` `<PropertyGroup>`.

```xml
<PropertyGroup>
    <RefitterAutoScan>false</RefitterAutoScan>
</PropertyGroup>
```

When `RefitterAutoScan` is `false`, run `dotnet build -t:RefitterGenerate` whenever you want MSBuild to regenerate code from `.refitter` files without re-enabling generation on every normal build. After that explicit generation step, regular `dotnet build` invocations can reuse the generated `.cs` files without re-running the Refitter task.

#### Multi-targeted projects

A project that lists two or more frameworks in `<TargetFrameworks>` (for example `net8.0;net9.0`) has one inner build per framework, and MSBuild runs them in parallel. To keep them from writing the same files at the same time, each inner build writes generated code to its own intermediate output folder instead of the `outputFolder` from the `.refitter` file:

```text
obj/<Configuration>/<TargetFramework>/Refitter/<.refitter path without extension>/<outputFolder>/<outputFilename>
```

For example, `petstore.refitter` with `"outputFolder": "./Generated"` and `"outputFilename": "Petstore.cs"` produces `obj/Debug/net8.0/Refitter/petstore/Generated/Petstore.cs` and `obj/Debug/net9.0/Refitter/petstore/Generated/Petstore.cs`. Each inner build compiles only its own copy, so the generated code can differ per target framework. `dotnet clean` deletes these files.

Copies of the generated files left in `outputFolder` by an older version of Refitter.MSBuild are excluded from compilation, so they don't cause duplicate type errors. You can delete them.

Projects with a single `<TargetFramework>` are not affected and still write to `outputFolder`. So does `dotnet build -t:RefitterGenerate`, which runs in the outer build where no target framework is set.

To write to `outputFolder` in a multi-targeted project as well, set:

```xml
<PropertyGroup>
  <RefitterOutputPerTargetFramework>false</RefitterOutputPerTargetFramework>
</PropertyGroup>
```

Only do this if every target framework generates the same code. The inner builds can then write the same files at the same time. Setting it to `true` in a single-target project writes generated code to `obj` as well.
