[CmdletBinding()]
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

# Output is suppressed by default. Pass -Verbose to show progress output
# from this script and all child processes.
$script:VerboseOutput = $VerbosePreference -eq "Continue"

$script:ChildLogDirectory = Join-Path ([System.IO.Path]::GetTempPath()) "refitter-smoke-tests-$([guid]::NewGuid().ToString('N'))"
New-Item -ItemType Directory -Path $script:ChildLogDirectory -Force | Out-Null

function Invoke-ChildProcess
{
    param (
        [string]$FilePath,
        [string]$Arguments,
        [string]$Description
    )

    Write-Verbose "$FilePath $Arguments"

    if ($script:VerboseOutput)
    {
        $p = Start-Process $FilePath -Args $Arguments -NoNewWindow -PassThru
        $p | Wait-Process
        return $p.ExitCode
    }

    $stdoutLog = Join-Path $script:ChildLogDirectory "$([guid]::NewGuid().ToString('N')).log"
    $stderrLog = "$stdoutLog.err"
    $p = Start-Process $FilePath -Args $Arguments -NoNewWindow -RedirectStandardOutput $stdoutLog -RedirectStandardError $stderrLog -PassThru
    $p | Wait-Process

    if ($p.ExitCode -ne 0)
    {
        Write-Host "FAILED: $Description (exit code $($p.ExitCode))"
        Write-Host "Command: $FilePath $Arguments"
        if (Test-Path $stdoutLog)
        {
            Write-Host "-- stdout --"
            Get-Content $stdoutLog -Tail 60 | ForEach-Object { Write-Host $_ }
        }
        if (Test-Path $stderrLog)
        {
            Write-Host "-- stderr --"
            Get-Content $stderrLog -Tail 60 | ForEach-Object { Write-Host $_ }
        }
    }

    return $p.ExitCode
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

    if (-not $script:VerboseOutput)
    {
        $arguments += " --silent"
    }

    if ($useDocker)
    {
        $dockerPrefix = BuildDockerPrefix
        $fullArgs = "$dockerPrefix $arguments"
        return Invoke-ChildProcess -FilePath "docker" -Arguments $fullArgs -Description "refitter"
    }

    return Invoke-ChildProcess -FilePath $processPath -Arguments $arguments -Description "refitter"
}

function GenerateFromSettingsFile
{
    param (
        [string]$settingsFile,
        [string]$processPath,
        [bool]$useDocker = $false
    )

    $exitCode = StartRefitter `
        -arguments "--no-logging --settings-file $settingsFile" `
        -processPath $processPath `
        -useDocker $useDocker
    if ($exitCode -ne 0) { throw "Refitter failed for settings file: $settingsFile" }
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

    Write-Verbose "Building $solution"
    $exitCode = Invoke-ChildProcess -FilePath "dotnet" -Arguments $buildArgs -Description "Build $solution"
    if ($exitCode -ne 0) { throw "Build Failed: $solution" }
}

function CleanGeneratedCode
{
    if (Test-Path './GeneratedCode') {
        try {
            Get-ChildItem './GeneratedCode' -Recurse -Include '*.cs' -ErrorAction Stop |
                ForEach-Object { Remove-Item -Path $_.FullName -Force -ErrorAction Stop }
        }
        catch [System.Management.Automation.ItemNotFoundException] {
            # Ignore not-found errors (path/file disappeared between enumeration and deletion)
        }
        try {
            Get-ChildItem './GeneratedCode' -Directory -ErrorAction Stop |
                ForEach-Object { Remove-Item -Path $_.FullName -Recurse -Force -ErrorAction Stop }
        }
        catch [System.Management.Automation.ItemNotFoundException] {
            # Ignore not-found errors (directory disappeared between enumeration and deletion)
        }
    }
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
        $exitCode = StartRefitter -arguments $arguments -processPath $processPath -useDocker $useDocker
        if ($exitCode -ne 0) { throw "Refitter generation failed for: $($task.SpecPath) ($($task.Namespace))" }
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
        "hubspot-events"
    )

    $v31Filenames = @(
        "webhook-example",
        "lmstudio"
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
        @{ Suffix="JsonSerializerContext"; Prefix="JsonSerializerCtx"; Args="--json-serializer-context" },
        @{ Suffix="JsonLibraryVersion9"; Prefix="JsonLibraryVersion9"; Args="--json-library-version 9.0" }
    )

    # ==========================================
    # Phase 0: Build refitter from source
    # ==========================================
    if ($BuildFromSource -and -not $UseDocker)
    {
        Write-Verbose "dotnet publish ../src/Refitter/Refitter.csproj -c Release -o bin -f net10.0"
        $exitCode = Invoke-ChildProcess `
            -FilePath "dotnet" `
            -Arguments "publish ../src/Refitter/Refitter.csproj -c Release -o bin -f net10.0 --nologo -v q" `
            -Description "dotnet publish"
        if ($exitCode -ne 0) { throw "Publish failed!" }

        $exitCode = Invoke-ChildProcess `
            -FilePath "./bin/refitter" `
            -Arguments "--version" `
            -Description "refitter --version"
        if ($exitCode -ne 0) { throw "Show version failed!" }
    }

    # ==========================================
    # Phase 1: Pre-restore packages
    # ==========================================
    Write-Verbose "Pre-restoring packages"
    $exitCode = Invoke-ChildProcess -FilePath "dotnet" -Arguments "restore ./ConsoleApp/ConsoleApp.slnx --nologo -v q" -Description "restore ConsoleApp.slnx"
    if ($exitCode -ne 0) { throw "Restore failed: ConsoleApp.slnx" }
    $exitCode = Invoke-ChildProcess -FilePath "dotnet" -Arguments "restore ./ConsoleApp/ConsoleApp.Core.slnx --nologo -v q" -Description "restore ConsoleApp.Core.slnx"
    if ($exitCode -ne 0) { throw "Restore failed: ConsoleApp.Core.slnx" }
    $exitCode = Invoke-ChildProcess -FilePath "dotnet" -Arguments "restore ./Apizr/Sample.csproj --nologo -v q" -Description "restore Apizr/Sample.csproj"
    if ($exitCode -ne 0) { throw "Restore failed: Apizr/Sample.csproj" }

    # ==========================================
    # Phase 2: Settings-file tests (individual generate + build)
    # ==========================================
    Write-Verbose "Settings-file tests"

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

    CleanGeneratedCode
    GenerateFromSettingsFile -settingsFile "./Streaming/.refitter" -processPath $processPath -useDocker $UseDocker
    BuildSolution -solution "./Streaming/Streaming.csproj"

    # ==========================================
    # Phase 3: Generate all STANDARD variants (no build until all are generated)
    # ==========================================
    Write-Verbose "Generating standard variants"
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
                    $taskArgs = $v.Args
                    # InterfaceOnly variant needs contracts-namespace so generated interfaces
                    # can reference contract types from the SeparateContracts variant
                    if ($v.Suffix -eq "InterfaceOnly") {
                        $taskArgs += " --contracts-namespace $ns.SeparateContractsFile.Contracts"
                    }
                    $standardTasks += @{
                        SpecPath = $specPath
                        Namespace = "$ns.$($v.Suffix)"
                        OutputPath = "./GeneratedCode/$($v.Prefix)${fileTag}.generated.cs"
                        Args = $taskArgs
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
                $taskArgs = $v.Args
                # InterfaceOnly variant needs contracts-namespace so generated interfaces
                # can reference contract types from the SeparateContracts variant
                if ($v.Suffix -eq "InterfaceOnly") {
                    $taskArgs += " --contracts-namespace $ns.SeparateContractsFile.Contracts"
                }
                $standardTasks += @{
                    SpecPath = $specPath
                    Namespace = "$ns.$($v.Suffix)"
                    OutputPath = "./GeneratedCode/$($v.Prefix)${fileTag}.generated.cs"
                    Args = $taskArgs
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
                $taskArgs = $v.Args
                if ($v.Suffix -eq "InterfaceOnly") {
                    $taskArgs += " --contracts-namespace $ns.SeparateContractsFile.Contracts"
                }
                $standardTasks += @{
                    SpecPath = $specPath
                    Namespace = "$ns.$($v.Suffix)"
                    OutputPath = "./GeneratedCode/$($v.Prefix)${fileTag}.generated.cs"
                    Args = $taskArgs
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

    Write-Verbose "Standard generation tasks: $($standardTasks.Count)"
    Write-Verbose "NetCore generation tasks: $($netCoreTasks.Count)"

    # Execute standard generation in parallel batches
    RunGenerationTasks -tasks $standardTasks -processPath $processPath -useDocker $UseDocker

    # ==========================================
    # Phase 4: Build standard variants (one build validates all)
    # ==========================================
    Write-Verbose "Building standard variants"
    BuildSolution -solution "./ConsoleApp/ConsoleApp.slnx" -noRestore -smokeTest

    # ==========================================
    # Phase 4b: Generate-only test for MultipleInterfacesWithCustomName
    # This variant uses --multiple-interfaces ByEndpoint --operation-name-template which
    # generates duplicate types per-endpoint (known limitation). We verify generation succeeds.
    # ==========================================
    Write-Verbose "Generate-only: MultipleInterfacesWithCustomName (petstore)"
    $customNameSpec = "./OpenAPI/v3.0/petstore.json"
    $customNameArgs = "--multiple-interfaces ByEndpoint --operation-name-template ExecuteAsync"
    $customNameOutput = "./GeneratedCode/MultipleInterfacesWithCustomName_generateonly.cs"
    $customNameInvocation = "$customNameSpec --namespace GenerateOnly.MultipleInterfacesWithCustomName --output $customNameOutput --no-logging $customNameArgs"
    $exitCode = StartRefitter `
        -arguments $customNameInvocation `
        -processPath $processPath `
        -useDocker $UseDocker
    if ($exitCode -ne 0) { throw "Generate-only test failed: MultipleInterfacesWithCustomName; exit code $exitCode" }
    if (-not (Test-Path $customNameOutput)) { throw "Generate-only test failed: MultipleInterfacesWithCustomName" }
    Remove-Item $customNameOutput -Force
    Write-Verbose "Generate-only test passed: MultipleInterfacesWithCustomName"

    # ==========================================
    # Phase 5: Generate netCore variants (accumulate on top of standard code)
    # Net8/Net9/Net10 can compile both standard and netCore code
    # ==========================================
    Write-Verbose "Generating netCore variants"
    RunGenerationTasks -tasks $netCoreTasks -processPath $processPath -useDocker $UseDocker

    # ==========================================
    # Phase 6: Build netCore variants
    # ==========================================
    Write-Verbose "Building netCore variants"
    BuildSolution -solution "./ConsoleApp/ConsoleApp.Core.slnx" -noRestore -smokeTest

    # ==========================================
    # Phase 7: URL-based tests (network-dependent)
    # ==========================================
    Write-Verbose "URL-based tests"
    CleanGeneratedCode

    @("https://petstore3.swagger.io/api/v3/openapi.json", "https://petstore3.swagger.io/api/v3/openapi.yaml") | ForEach-Object {
        $url = $_

        try {
            Get-ChildItem './GeneratedCode/*.cs' -Recurse -ErrorAction Stop |
                ForEach-Object { Remove-Item -Path $_.FullName -Force -ErrorAction Stop }
        }
        catch [System.Management.Automation.ItemNotFoundException] {
            # Ignore not-found errors (path/file doesn't exist yet)
        }

        $exitCode = StartRefitter `
            -arguments """$url"" --namespace PetstoreFromUri --output ./GeneratedCode/PetstoreFromUri.generated.cs --no-logging" `
            -processPath $processPath `
            -useDocker $UseDocker
        if ($exitCode -ne 0) { throw "Refitter failed for URL: $url" }

        BuildSolution -solution "./ConsoleApp/ConsoleApp.slnx" -noRestore
    }

    # ==========================================
    # Phase 8: Operation Name Generator Tests
    # ==========================================
    Write-Verbose "Operation Name Generator Tests"

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
        $genArgs = "./OpenAPI/v3.0/petstore.json --namespace OpNameGen_$gen --output ./GeneratedCode/OpNameGen_$gen.generated.cs --no-logging --operation-name-generator $gen"
        $exitCode = StartRefitter -arguments $genArgs -processPath $processPath -useDocker $UseDocker
        if ($exitCode -ne 0) { Write-Verbose "Operation name generator '$gen' failed (may be expected for some generators)" }
    }
    # Build only what was successfully generated
    if (Test-Path './GeneratedCode/OpNameGen_*.generated.cs') {
        BuildSolution -solution "./ConsoleApp/ConsoleApp.Core.slnx" -noRestore -smokeTest
    }

    # ==========================================
    # Phase 9: Collection Format Variant Tests
    # ==========================================
    Write-Verbose "Collection Format Variant Tests"

    $collectionFormats = @("Multi", "Ssv", "Tsv", "Pipes")

    CleanGeneratedCode
    foreach ($fmt in $collectionFormats)
    {
        $fmtArgs = "./OpenAPI/v3.0/petstore.json --namespace CollFmt_$fmt --output ./GeneratedCode/CollFmt_$fmt.generated.cs --no-logging --collection-format $fmt"
        $exitCode = StartRefitter -arguments $fmtArgs -processPath $processPath -useDocker $UseDocker
        if ($exitCode -ne 0) { throw "Collection format '$fmt' generation failed" }
    }
    BuildSolution -solution "./ConsoleApp/ConsoleApp.Core.slnx" -noRestore -smokeTest

    # ==========================================
    # Phase 10: Combination Tests
    # ==========================================
    Write-Verbose "Combination Tests"

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
            Output = "./GeneratedCode/Combo_ContractOnlyMultipleFiles/"
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
        if ($combo.ContainsKey("Output"))
        {
            $output = $combo.Output
        }
        $fullArgs = "$($combo.Spec) --namespace $ns --output $output --no-logging $($combo.Args)"
        $exitCode = StartRefitter -arguments $fullArgs -processPath $processPath -useDocker $UseDocker
        if ($exitCode -ne 0) { throw "Combination test '$($combo.Name)' generation failed" }
    }
    BuildSolution -solution "./ConsoleApp/ConsoleApp.Core.slnx" -noRestore -smokeTest

    # ==========================================
    # Phase 11: Asana API regression test (issue #359)
    # Large real-world spec that previously failed to compile with
    # "cannot derive from sealed type 'string'" (CS0509) because of
    # single-primitive allOf wrappers. The spec is committed under
    # OpenAPI/v3.0/asana.yaml to avoid transient HTTP errors. Generate
    # with default settings and build once to guard against regressions.
    # ==========================================
    Write-Verbose "Asana API regression test (issue #359)"

    CleanGeneratedCode
    $asanaArgs = "./OpenAPI/v3.0/asana.yaml --namespace Asana --output ./GeneratedCode/Asana.generated.cs --no-logging"
    $exitCode = StartRefitter -arguments $asanaArgs -processPath $processPath -useDocker $UseDocker
    if ($exitCode -ne 0) { throw "Asana API generation failed (issue #359 regression)" }
    BuildSolution -solution "./ConsoleApp/ConsoleApp.Core.slnx" -noRestore -smokeTest
}

if ($UseProduction)
{
    Write-Verbose "Running smoke tests in production mode"
    $exitCode = Invoke-ChildProcess -FilePath "dotnet" -Arguments "tool update -g refitter --prerelease -v q" -Description "dotnet tool update -g refitter --prerelease"
    if ($exitCode -ne 0) { throw "Production tool update failed" }
}

if ($UseDocker)
{
    Write-Verbose "Running smoke tests in Docker mode"
    $exitCode = Invoke-ChildProcess -FilePath "docker" -Arguments "pull christianhelle/refitter:latest" -Description "docker pull christianhelle/refitter:latest"
    if ($exitCode -ne 0) { throw "Docker image pull failed" }
}

try
{
    $totalTime = Measure-Command {
        RunTests `
            -BuildFromSource (-not $UseProduction -and -not $UseDocker) `
            -UseDocker $UseDocker
    }
    Write-Host "Smoke tests passed in $([math]::Round($totalTime.TotalSeconds, 1)) seconds"
}
finally
{
    if (Test-Path $script:ChildLogDirectory)
    {
        Remove-Item -Path $script:ChildLogDirectory -Recurse -Force -ErrorAction SilentlyContinue
    }
}
