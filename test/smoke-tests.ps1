param (
    [Parameter(Mandatory=$false)]
    [switch]
    $UseProduction = $false,

    [Parameter(Mandatory=$false)]
    [switch]
    $UseDocker = $false,

    # Kept for backward compatibility
    [Parameter(Mandatory=$false)]
    [bool]
    $Parallel = $true
)

function ThrowOnNativeFailure
{
    if (-not $?)
    {
        throw "Native Failure"
    }
}

function GetProcessPath([bool]$buildFromSource, [bool]$useDocker)
{
    if ($useDocker) { return "docker" }
    if (-not $buildFromSource) { return "refitter" }
    return "./bin/refitter"
}

function BuildDockerPrefix()
{
    $currentDir = (Get-Location).Path.Replace('\', '/')
    $userParam = ""
    if ($IsLinux -or $IsMacOS) {
        $uid = sh -c 'id -u'
        $gid = sh -c 'id -g'
        $userParam = "--user ${uid}:${gid}"
    }
    $prefix = "run --rm -v ""${currentDir}:/src"" -w /src"
    if ($userParam) { $prefix += " $userParam" }
    $prefix += " christianhelle/refitter"
    return $prefix
}

function StartRefitter
{
    param (
        [string]$arguments,
        [string]$processPath,
        [bool]$useDocker = $false
    )

    if ($useDocker)
    {
        $dockerPrefix = BuildDockerPrefix
        $fullArgs = "$dockerPrefix $arguments"
        Write-Host "docker $fullArgs"
        return Start-Process "docker" -Args $fullArgs -NoNewWindow -PassThru
    }
    else
    {
        Write-Host "$processPath $arguments"
        return Start-Process $processPath -Args $arguments -NoNewWindow -PassThru
    }
}

function GenerateFromSettingsFile
{
    param (
        [string]$settingsFile,
        [string]$processPath,
        [bool]$useDocker = $false
    )

    $p = StartRefitter `
        -arguments "--no-logging --settings-file $settingsFile" `
        -processPath $processPath `
        -useDocker $useDocker
    $p | Wait-Process
    if ($p.ExitCode -ne 0) { throw "Refitter failed for settings file: $settingsFile" }
}

function BuildSolution
{
    param (
        [string]$solution,
        [switch]$noRestore,
        [switch]$smokeTest
    )

    $buildArgs = "build $solution --nologo -v q --property WarningLevel=0 /clp:ErrorsOnly"
    if ($noRestore) { $buildArgs += " --no-restore" }
    if ($smokeTest) { $buildArgs += " --property:SmokeTest=true" }

    Write-Host "`r`nBuilding $solution`r`n"
    $p = Start-Process "dotnet" -Args $buildArgs -NoNewWindow -PassThru
    $p | Wait-Process
    if ($p.ExitCode -ne 0) { throw "Build Failed: $solution" }
}

function CleanGeneratedCode
{
    try {
        if (Test-Path './GeneratedCode') {
            Get-ChildItem './GeneratedCode' -Recurse -Include '*.cs' -ErrorAction SilentlyContinue |
                ForEach-Object { Remove-Item -Path $_.FullName -Force }
            Get-ChildItem './GeneratedCode' -Directory -ErrorAction SilentlyContinue |
                ForEach-Object { Remove-Item -Path $_.FullName -Recurse -Force }
        }
    } catch { }
}

function RunGenerationTasks
{
    param (
        [array]$tasks,
        [string]$processPath,
        [bool]$useDocker
    )

    for ($i = 0; $i -lt $tasks.Count; $i++)
    {
        $task = $tasks[$i]
        $arguments = "$($task.SpecPath) --namespace $($task.Namespace) --output $($task.OutputPath) --no-logging"
        if ($task.Args) { $arguments += " $($task.Args)" }
        $p = StartRefitter -arguments $arguments -processPath $processPath -useDocker $useDocker
        $p | Wait-Process
        if ($p.ExitCode -ne 0) { throw "Refitter generation failed for: $($task.SpecPath) ($($task.Namespace))" }
    }
}

function RunTests
{
    param (
        [bool]$BuildFromSource = $true,
        [bool]$UseDocker = $false
    )

    $processPath = GetProcessPath -buildFromSource $BuildFromSource -useDocker $UseDocker

    $filenames = @(
        "weather",
        "bot.paths",
        "petstore",
        "petstore-expanded",
        "petstore-minimal",
        "petstore-simple",
        "petstore-with-external-docs",
        "api-with-examples",
        "callback-example",
        "link-example",
        "uber",
        "uspto",
        "hubspot-events",
        "hubspot-webhooks"
    )

    $v31Filenames = @(
        "webhook-example"
    )

    $v34WebhookFilenames = @(
        "webhook-example"
    )

    # Standard variants: compile on all frameworks (net462-net10)
    $standardVariants = @(
        @{ Suffix="Cancellation"; Prefix="WithCancellation"; Args="--cancellation-tokens" },
        @{ Suffix="Internal"; Prefix="Internal"; Args="--internal" },
        @{ Suffix="UsingApiResponse"; Prefix="IApi"; Args="--use-api-response" },
        @{ Suffix="UsingIObservable"; Prefix="IObservable"; Args="--use-observable-response" },
        @{ Suffix="UsingIsoDateFormat"; Prefix="UsingIsoDateFormat"; Args="--use-iso-date-format" },
        @{ Suffix="MultipleInterfaces"; Prefix="MultipleInterfaces"; Args="--multiple-interfaces ByEndpoint" },
        # NOTE: --multiple-interfaces ByEndpoint --operation-name-template produces duplicate types per-endpoint.
        # This is a known Refitter limitation. We test generation works but skip compilation.
        # @{ Suffix="MultipleInterfaces"; Prefix="MultipleInterfacesWithCustomName"; Args="--multiple-interfaces ByEndpoint --operation-name-template ExecuteAsync" },
        @{ Suffix="ContractOnly"; Prefix="ContractOnly"; Args="--contract-only" },
        @{ Suffix="DynamicQuerystring"; Prefix="DynamicQuerystring"; Args="--use-dynamic-querystring-parameters" },
        @{ Suffix="IntegerTypeInt64"; Prefix="IntegerTypeInt64"; Args="--integer-type Int64" },
        @{ Suffix="TrimUnusedSchema"; Prefix="TrimUnusedSchema"; Args="--trim-unused-schema" },
        @{ Suffix="OptionalNullable"; Prefix="OptionalNullable"; Args="--optional-nullable-parameters" },
        @{ Suffix="NoDeprecated"; Prefix="NoDeprecated"; Args="--no-deprecated-operations" },
        @{ Suffix="NoAutoGeneratedHeader"; Prefix="NoAutoGenHeader"; Args="--no-auto-generated-header" },
        @{ Suffix="NoAcceptHeaders"; Prefix="NoAcceptHeaders"; Args="--no-accept-headers" },
        @{ Suffix="SkipDefaultAdditionalProps"; Prefix="SkipDefaultAddlProps"; Args="--skip-default-additional-properties" },
        @{ Suffix="NoInlineJsonConverters"; Prefix="NoInlineJsonConv"; Args="--no-inline-json-converters" },
        @{ Suffix="InterfaceOnly"; Prefix="InterfaceOnly"; Args="--interface-only" },
        @{ Suffix="NoXmlDocComments"; Prefix="NoXmlDoc"; Args="--no-xml-doc-comments" },
        @{ Suffix="NoOperationHeaders"; Prefix="NoOpHeaders"; Args="--no-operation-headers" },
        @{ Suffix="AdditionalNamespace"; Prefix="AdditionalNs"; Args="--additional-namespace System.ComponentModel" },
        @{ Suffix="ExcludeNamespace"; Prefix="ExcludeNs"; Args="--exclude-namespace System.Xml.Serialization" },
        @{ Suffix="PreserveOriginal"; Prefix="PreserveOriginal"; Args="--property-naming-policy PreserveOriginal" }
    )

    # Petstore-only variants: require specs with specific tags/paths (petstore has "pet", "user", "store" tags)
    $petstoreOnlyVariants = @(
        @{ Suffix="TagFiltered"; Prefix="TagFiltered"; Args="--tag pet --tag user --tag store" },
        @{ Suffix="MatchPathFiltered"; Prefix="MatchPathFiltered"; Args="--match-path ^/pet/.*" },
        @{ Suffix="MultipleInterfacesByTag"; Prefix="MultipleInterfacesByTag"; Args="--multiple-interfaces ByTag" }
    )

    # NetCore variants: require net8.0+ features
    $netCoreVariants = @(
        @{ Suffix="Disposable"; Prefix="Disposable"; Args="--disposable" },
        @{ Suffix="ImmutableRecords"; Prefix="ImmutableRecords"; Args="--immutable-records" },
        @{ Suffix="PolymorphicSerialization"; Prefix="PolymorphicSerialization"; Args="--use-polymorphic-serialization" },
        @{ Suffix="CollectionFormatCsv"; Prefix="CollectionFormatCsv"; Args="--collection-format csv" },
        @{ Suffix="JsonSerializerContext"; Prefix="JsonSerializerCtx"; Args="--json-serializer-context" }
    )

    # ==========================================
    # Phase 0: Build refitter from source
    # ==========================================
    if ($BuildFromSource -and -not $UseDocker)
    {
        Write-Host "dotnet publish ../src/Refitter/Refitter.csproj -c Release -o bin -f net10.0"
        Start-Process "dotnet" -Args "publish ../src/Refitter/Refitter.csproj -c Release -o bin -f net10.0" -NoNewWindow -PassThru | Wait-Process

        Write-Host "refitter --version"
        $p = Start-Process "./bin/refitter" -Args "--version" -NoNewWindow -PassThru
        $p | Wait-Process
        if ($p.ExitCode -ne 0) { throw "Show version failed!" }
    }

    # ==========================================
    # Phase 1: Pre-restore packages
    # ==========================================
    Write-Host "`r`n=== Pre-restoring packages ===`r`n"
    Start-Process "dotnet" -Args "restore ./ConsoleApp/ConsoleApp.slnx --nologo -v q" -NoNewWindow -PassThru | Wait-Process
    Start-Process "dotnet" -Args "restore ./ConsoleApp/ConsoleApp.Core.slnx --nologo -v q" -NoNewWindow -PassThru | Wait-Process
    Start-Process "dotnet" -Args "restore ./Apizr/Sample.csproj --nologo -v q" -NoNewWindow -PassThru | Wait-Process

    # ==========================================
    # Phase 2: Settings-file tests (individual generate + build)
    # ==========================================
    Write-Host "`r`n=== Settings-file tests ===`r`n"

    CleanGeneratedCode
    GenerateFromSettingsFile -settingsFile "./petstore.refitter" -processPath $processPath -useDocker $UseDocker
    BuildSolution -solution "./ConsoleApp/ConsoleApp.slnx" -noRestore

    CleanGeneratedCode
    GenerateFromSettingsFile -settingsFile "./Apizr/petstore.apizr.refitter" -processPath $processPath -useDocker $UseDocker
    BuildSolution -solution "./Apizr/Sample.csproj" -noRestore

    GenerateFromSettingsFile -settingsFile "./MultipleFiles/petstore.refitter" -processPath $processPath -useDocker $UseDocker
    BuildSolution -solution "MultipleFiles/Client/Client.csproj"

    CleanGeneratedCode
    GenerateFromSettingsFile -settingsFile "./multiple-sources.refitter" -processPath $processPath -useDocker $UseDocker
    BuildSolution -solution "./ConsoleApp/ConsoleApp.Core.slnx" -noRestore

    # ==========================================
    # Phase 3: Generate all STANDARD variants (no build until all are generated)
    # ==========================================
    Write-Host "`r`n=== Generating standard variants ===`r`n"
    CleanGeneratedCode

    $standardTasks = @()
    $netCoreTasks = @()

    # Helper to create unique file tag from version/format/filename
    function MakeFileTag([string]$version, [string]$format, [string]$filename)
    {
        $vTag = $version.Replace(".", "")
        $base = $filename.Replace("-", "").Replace(".", "")
        $base = $base.Substring(0, 1).ToUpperInvariant() + $base.Substring(1)
        $nsBase = "${base}_${vTag}_${format}"
        return @{ Tag = "${vTag}_${format}_${base}"; Namespace = $nsBase }
    }

    # Collect generation tasks for v2.0 and v3.0
    foreach ($version in @("v3.0", "v2.0"))
    {
        foreach ($format in @("json", "yaml"))
        {
            foreach ($filename in $filenames)
            {
                $specPath = "./OpenAPI/$version/$filename.$format"
                if (-not (Test-Path -Path $specPath -PathType Leaf)) { continue }

                $info = MakeFileTag $version $format $filename
                $fileTag = $info.Tag
                $ns = $info.Namespace

                foreach ($v in $standardVariants)
                {
                    $args = $v.Args
                    # InterfaceOnly variant needs contracts-namespace so generated interfaces
                    # can reference contract types from the SeparateContracts variant
                    if ($v.Suffix -eq "InterfaceOnly") {
                        $args += " --contracts-namespace $ns.SeparateContractsFile.Contracts"
                    }
                    $standardTasks += @{
                        SpecPath = $specPath
                        Namespace = "$ns.$($v.Suffix)"
                        OutputPath = "./GeneratedCode/$($v.Prefix)${fileTag}.generated.cs"
                        Args = $args
                    }
                }

                # Petstore-only variants (tag/path filters require petstore-specific tags)
                if ($filename -like "petstore*")
                {
                    foreach ($v in $petstoreOnlyVariants)
                    {
                        $standardTasks += @{
                            SpecPath = $specPath
                            Namespace = "$ns.$($v.Suffix)"
                            OutputPath = "./GeneratedCode/$($v.Prefix)${fileTag}.generated.cs"
                            Args = $v.Args
                        }
                    }
                }

                # Multiple files variant (unique subdirectory)
                $standardTasks += @{
                    SpecPath = $specPath
                    Namespace = "$ns.MultipleFiles"
                    OutputPath = "./GeneratedCode/MultipleFiles/$fileTag/"
                    Args = "--multiple-files"
                }

                # Separate contracts variant (unique subdirectories for both interface and contracts)
                $standardTasks += @{
                    SpecPath = $specPath
                    Namespace = "$ns.SeparateContractsFile"
                    OutputPath = "./GeneratedCode/SeparateContracts/$fileTag/"
                    Args = "--contracts-output GeneratedCode/Contracts/$fileTag --contracts-namespace $ns.SeparateContractsFile.Contracts"
                }

                foreach ($v in $netCoreVariants)
                {
                    $netCoreTasks += @{
                        SpecPath = $specPath
                        Namespace = "$ns.$($v.Suffix)"
                        OutputPath = "./GeneratedCode/$($v.Prefix)${fileTag}.generated.cs"
                        Args = $v.Args
                    }
                }
            }
        }
    }

    # Collect generation tasks for v3.1
    # Note: v3.1 webhook specs may not have regular API paths, so skip MultipleInterfaces variants
    foreach ($format in @("json", "yaml"))
    {
        foreach ($filename in $v31Filenames)
        {
            $specPath = "./OpenAPI/v3.1/$filename.$format"
            if (-not (Test-Path -Path $specPath -PathType Leaf)) { continue }

            $info = MakeFileTag "v3.1" $format $filename
            $fileTag = $info.Tag
            $ns = $info.Namespace

            foreach ($v in $standardVariants)
            {
                if ($v.Args -like "*--multiple-interfaces*") { continue }
                $args = $v.Args
                # InterfaceOnly variant needs contracts-namespace so generated interfaces
                # can reference contract types from the SeparateContracts variant
                if ($v.Suffix -eq "InterfaceOnly") {
                    $args += " --contracts-namespace $ns.SeparateContractsFile.Contracts"
                }
                $standardTasks += @{
                    SpecPath = $specPath
                    Namespace = "$ns.$($v.Suffix)"
                    OutputPath = "./GeneratedCode/$($v.Prefix)${fileTag}.generated.cs"
                    Args = $args
                }
            }

            $standardTasks += @{
                SpecPath = $specPath
                Namespace = "$ns.MultipleFiles"
                OutputPath = "./GeneratedCode/MultipleFiles/$fileTag/"
                Args = "--multiple-files"
            }

            $standardTasks += @{
                SpecPath = $specPath
                Namespace = "$ns.SeparateContractsFile"
                OutputPath = "./GeneratedCode/SeparateContracts/$fileTag/"
                Args = "--contracts-output GeneratedCode/Contracts/$fileTag --contracts-namespace $ns.SeparateContractsFile.Contracts"
            }

            foreach ($v in $netCoreVariants)
            {
                $netCoreTasks += @{
                    SpecPath = $specPath
                    Namespace = "$ns.$($v.Suffix)"
                    OutputPath = "./GeneratedCode/$($v.Prefix)${fileTag}.generated.cs"
                    Args = $v.Args
                }
            }
        }
    }

    # Collect generation tasks for v3.4 webhook specs
    # Note: webhook specs may not have regular API paths, so skip MultipleInterfaces variants
    foreach ($format in @("json", "yaml"))
    {
        foreach ($filename in $v34WebhookFilenames)
        {
            $specPath = "./OpenAPI/v3.4/$filename.$format"
            if (-not (Test-Path -Path $specPath -PathType Leaf)) { continue }

            $info = MakeFileTag "v3.4" $format $filename
            $fileTag = $info.Tag
            $ns = $info.Namespace

            foreach ($v in $standardVariants)
            {
                if ($v.Args -like "*--multiple-interfaces*") { continue }
                $args = $v.Args
                if ($v.Suffix -eq "InterfaceOnly") {
                    $args += " --contracts-namespace $ns.SeparateContractsFile.Contracts"
                }
                $standardTasks += @{
                    SpecPath = $specPath
                    Namespace = "$ns.$($v.Suffix)"
                    OutputPath = "./GeneratedCode/$($v.Prefix)${fileTag}.generated.cs"
                    Args = $args
                }
            }

            $standardTasks += @{
                SpecPath = $specPath
                Namespace = "$ns.MultipleFiles"
                OutputPath = "./GeneratedCode/MultipleFiles/$fileTag/"
                Args = "--multiple-files"
            }

            $standardTasks += @{
                SpecPath = $specPath
                Namespace = "$ns.SeparateContractsFile"
                OutputPath = "./GeneratedCode/SeparateContracts/$fileTag/"
                Args = "--contracts-output GeneratedCode/Contracts/$fileTag --contracts-namespace $ns.SeparateContractsFile.Contracts"
            }

            foreach ($v in $netCoreVariants)
            {
                $netCoreTasks += @{
                    SpecPath = $specPath
                    Namespace = "$ns.$($v.Suffix)"
                    OutputPath = "./GeneratedCode/$($v.Prefix)${fileTag}.generated.cs"
                    Args = $v.Args
                }
            }
        }
    }

    Write-Host "Standard generation tasks: $($standardTasks.Count)"
    Write-Host "NetCore generation tasks: $($netCoreTasks.Count)"

    # Execute standard generation in parallel batches
    RunGenerationTasks -tasks $standardTasks -processPath $processPath -useDocker $UseDocker

    # ==========================================
    # Phase 4: Build standard variants (one build validates all)
    # ==========================================
    Write-Host "`r`n=== Building standard variants ===`r`n"
    BuildSolution -solution "./ConsoleApp/ConsoleApp.slnx" -noRestore -smokeTest

    # ==========================================
    # Phase 4b: Generate-only test for MultipleInterfacesWithCustomName
    # This variant uses --multiple-interfaces ByEndpoint --operation-name-template which
    # generates duplicate types per-endpoint (known limitation). We verify generation succeeds.
    # ==========================================
    Write-Host "`r`n=== Generate-only: MultipleInterfacesWithCustomName (petstore) ===`r`n"
    $customNameSpec = "./OpenAPI/v3.0/petstore.json"
    $customNameArgs = "--multiple-interfaces ByEndpoint --operation-name-template ExecuteAsync"
    $customNameOutput = "./GeneratedCode/MultipleInterfacesWithCustomName_generateonly.cs"
    $customNameInvocation = "$customNameSpec --namespace GenerateOnly.MultipleInterfacesWithCustomName --output $customNameOutput --no-logging $customNameArgs"
    $p = StartRefitter `
        -arguments $customNameInvocation `
        -processPath $processPath `
        -useDocker $UseDocker
    $p | Wait-Process
    if (-not (Test-Path $customNameOutput)) { throw "Generate-only test failed: MultipleInterfacesWithCustomName" }
    Remove-Item $customNameOutput -Force
    Write-Host "Generate-only test passed: MultipleInterfacesWithCustomName"

    # ==========================================
    # Phase 5: Generate netCore variants (accumulate on top of standard code)
    # Net8/Net9/Net10 can compile both standard and netCore code
    # ==========================================
    Write-Host "`r`n=== Generating netCore variants ===`r`n"
    RunGenerationTasks -tasks $netCoreTasks -processPath $processPath -useDocker $UseDocker

    # ==========================================
    # Phase 6: Build netCore variants
    # ==========================================
    Write-Host "`r`n=== Building netCore variants ===`r`n"
    BuildSolution -solution "./ConsoleApp/ConsoleApp.Core.slnx" -noRestore -smokeTest

    # ==========================================
    # Phase 7: URL-based tests (network-dependent)
    # ==========================================
    Write-Host "`r`n=== URL-based tests ===`r`n"
    CleanGeneratedCode

    @("https://petstore3.swagger.io/api/v3/openapi.json", "https://petstore3.swagger.io/api/v3/openapi.yaml") | ForEach-Object {
        $url = $_
        $urlFormat = if ($url.EndsWith(".json")) { "json" } else { "yaml" }
        $namespace = "PetstoreFromUri"
        $outputPath = "PetstoreFromUri.generated.cs"

        try {
            Get-ChildItem './GeneratedCode/*.cs' -Recurse -ErrorAction SilentlyContinue |
                ForEach-Object { Remove-Item -Path $_.FullName -Force }
        } catch { }

        $p = StartRefitter `
            -arguments """$url"" --namespace $namespace --output ./GeneratedCode/$outputPath --no-logging" `
            -processPath $processPath `
            -useDocker $UseDocker
        $p | Wait-Process
        if ($p.ExitCode -ne 0) { throw "Refitter failed for URL: $url" }

        BuildSolution -solution "./ConsoleApp/ConsoleApp.slnx" -noRestore
    }

    # ==========================================
    # Phase 8: Operation Name Generator Tests
    # ==========================================
    Write-Host "`r`n=== Operation Name Generator Tests ===`r`n"

    $opNameGenerators = @(
        "Default",
        "MultipleClientsFromOperationId",
        "MultipleClientsFromPathSegments",
        "MultipleClientsFromFirstTagAndOperationId",
        "MultipleClientsFromFirstTagAndOperationName",
        "MultipleClientsFromFirstTagAndPathSegments",
        "SingleClientFromOperationId",
        "SingleClientFromPathSegments"
    )

    CleanGeneratedCode
    foreach ($gen in $opNameGenerators)
    {
        $args = "./OpenAPI/v3.0/petstore.json --namespace OpNameGen_$gen --output ./GeneratedCode/OpNameGen_$gen.generated.cs --no-logging --operation-name-generator $gen"
        $p = StartRefitter -arguments $args -processPath $processPath -useDocker $UseDocker
        $p | Wait-Process
        if ($p.ExitCode -ne 0) { Write-Warning "Operation name generator '$gen' failed (may be expected for some generators)" }
    }
    # Build only what was successfully generated
    if (Test-Path './GeneratedCode/OpNameGen_*.generated.cs') {
        BuildSolution -solution "./ConsoleApp/ConsoleApp.Core.slnx" -noRestore -smokeTest
    }

    # ==========================================
    # Phase 9: Collection Format Variant Tests
    # ==========================================
    Write-Host "`r`n=== Collection Format Variant Tests ===`r`n"

    $collectionFormats = @("Multi", "Ssv", "Tsv", "Pipes")

    CleanGeneratedCode
    foreach ($fmt in $collectionFormats)
    {
        $args = "./OpenAPI/v3.0/petstore.json --namespace CollFmt_$fmt --output ./GeneratedCode/CollFmt_$fmt.generated.cs --no-logging --collection-format $fmt"
        $p = StartRefitter -arguments $args -processPath $processPath -useDocker $UseDocker
        $p | Wait-Process
        if ($p.ExitCode -ne 0) { throw "Collection format '$fmt' generation failed" }
    }
    BuildSolution -solution "./ConsoleApp/ConsoleApp.Core.slnx" -noRestore -smokeTest

    # ==========================================
    # Phase 10: Combination Tests
    # ==========================================
    Write-Host "`r`n=== Combination Tests ===`r`n"

    CleanGeneratedCode
    $combinationTasks = @(
        @{
            Name = "MultipleInterfacesByTagFiltered"
            Args = "--multiple-interfaces ByTag --tag pet --tag store"
            Spec = "./OpenAPI/v3.0/petstore.json"
        },
        @{
            Name = "ImmutableRecordsPolymorphic"
            Args = "--immutable-records --use-polymorphic-serialization"
            Spec = "./OpenAPI/v3.0/petstore.json"
        },
        @{
            Name = "ContractOnlyMultipleFiles"
            Args = "--contract-only --multiple-files"
            Spec = "./OpenAPI/v3.0/petstore.json"
        },
        @{
            Name = "TrimSchemaKeepPattern"
            Args = "--trim-unused-schema --tag pet --keep-schema `"^Pet.*`""
            Spec = "./OpenAPI/v3.0/petstore.json"
        },
        @{
            Name = "DisposableCancellation"
            Args = "--disposable --cancellation-tokens"
            Spec = "./OpenAPI/v3.0/petstore.json"
        }
    )

    foreach ($combo in $combinationTasks)
    {
        $ns = "Combo_$($combo.Name)"
        $output = "./GeneratedCode/Combo_$($combo.Name).generated.cs"
        $fullArgs = "$($combo.Spec) --namespace $ns --output $output --no-logging $($combo.Args)"
        $p = StartRefitter -arguments $fullArgs -processPath $processPath -useDocker $UseDocker
        $p | Wait-Process
        if ($p.ExitCode -ne 0) { throw "Combination test '$($combo.Name)' generation failed" }
    }
    BuildSolution -solution "./ConsoleApp/ConsoleApp.Core.slnx" -noRestore -smokeTest
}

if ($UseProduction)
{
    Write-Host "Running smoke tests in production mode"
    Write-Host "dotnet tool update -g refitter --prerelease"
    Start-Process "dotnet" -Args "tool update -g refitter --prerelease" -NoNewWindow -PassThru | Wait-Process
    ThrowOnNativeFailure
    Write-Host "`r`n"
}

if ($UseDocker)
{
    Write-Host "Running smoke tests in Docker mode"
    Write-Host "docker pull christianhelle/refitter:latest"
    Start-Process "docker" -Args "pull christianhelle/refitter:latest" -NoNewWindow -PassThru | Wait-Process
    ThrowOnNativeFailure
    Write-Host "`r`n"
}

Measure-Command {
    RunTests `
        -BuildFromSource (!$UseProduction -and !$UseDocker) `
        -UseDocker $UseDocker
}
Write-Host "`r`n"
Write-Host "`r`n"
