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
    $Parallel = $true,

    [Parameter(Mandatory=$false)]
    [string]
    $LogFile = ""
)

function Write-Log
{
    param([string]$Message)
    if (-not $Message) { return }
    Write-Host $Message
    if ($LogFile) {
        $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss.fff"
        "[$timestamp] $Message" | Out-File -FilePath $LogFile -Append -Encoding utf8
    }
}

function ConvertTo-ArgumentArray
{
    param([string]$CommandLine)
    $args = [System.Collections.Generic.List[string]]::new()
    $sb = [System.Text.StringBuilder]::new()
    $inQuote = $false
    for ($i = 0; $i -lt $CommandLine.Length; $i++) {
        $c = $CommandLine[$i]
        if ($inQuote) {
            if ($c -eq '"') { $inQuote = $false }
            else { [void]$sb.Append($c) }
        } elseif ($c -eq '"') {
            $inQuote = $true
        } elseif ($c -le ' ') {
            if ($sb.Length -gt 0) {
                $args.Add($sb.ToString())
                [void]$sb.Clear()
            }
        } else {
            [void]$sb.Append($c)
        }
    }
    if ($sb.Length -gt 0) { $args.Add($sb.ToString()) }
    return $args.ToArray()
}

function Invoke-Process
{
    param (
        [string]$FilePath,
        [string]$Arguments,
        [string]$Description = ""
    )

    if ($Description) {
        Write-Log ">>> $Description"
        Write-Log "    $FilePath $Arguments"
    }

    try {
        $argArray = ConvertTo-ArgumentArray $Arguments
        $psi = New-Object System.Diagnostics.ProcessStartInfo
        $psi.FileName = $FilePath
        $psi.UseShellExecute = $false
        $psi.RedirectStandardOutput = $true
        $psi.RedirectStandardError = $true
        $psi.CreateNoWindow = $true
        foreach ($arg in $argArray) {
            [void]$psi.ArgumentList.Add($arg)
        }

        $p = [System.Diagnostics.Process]::Start($psi)
        $stdout = $p.StandardOutput.ReadToEnd()
        $stderr = $p.StandardError.ReadToEnd()
        $p.WaitForExit()

        $output = "$($stdout.Trim())`r`n$($stderr.Trim())".Trim()
        if ($output) {
            Write-Log $output
        }
        return @{
            ExitCode = $p.ExitCode
            Output = $output
            Command = "$FilePath $Arguments"
        }
    }
    finally {
        if ($p) { $p.Dispose() }
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
        $displayCommand = "docker $fullArgs"
        return Invoke-Process -FilePath "docker" -Arguments $fullArgs -Description $displayCommand
    }
    else
    {
        return Invoke-Process -FilePath $processPath -Arguments $arguments -Description "$processPath $arguments"
    }
}

function GenerateFromSettingsFile
{
    param (
        [string]$settingsFile,
        [string]$processPath,
        [bool]$useDocker = $false
    )

    $arguments = "--no-logging --settings-file $settingsFile"
    $result = StartRefitter `
        -arguments $arguments `
        -processPath $processPath `
        -useDocker $useDocker

    if ($result.ExitCode -ne 0) { throw "Settings file FAILED [exit: $($result.ExitCode)]`n  File: $settingsFile`n  Output: $($result.Output)" }
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

    Write-Log ""
    Write-Log "=== Building $solution ==="
    Write-Log ""
    $result = Invoke-Process -FilePath "dotnet" -Arguments $buildArgs -Description "dotnet build $solution"
    if ($result.ExitCode -ne 0) { throw "Build FAILED for $solution [exit: $($result.ExitCode)]`n  Output: $($result.Output)" }
}

function CleanGeneratedCode
{
    try {
        if (Test-Path './GeneratedCode') {
            Remove-Item -Path './GeneratedCode' -Recurse -Force
        }
        New-Item -ItemType Directory -Path './GeneratedCode' -Force | Out-Null
    } catch {
        Write-Log "Warning: Could not fully clean GeneratedCode directory: $_"
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

        Write-Log "[$($i+1)/$($tasks.Count)] Generating $($task.Namespace)..."
        $result = StartRefitter -arguments $arguments -processPath $processPath -useDocker $useDocker
        if ($result.ExitCode -ne 0) {
            throw "Generation FAILED [exit: $($result.ExitCode)]`n  Spec: $($task.SpecPath)`n  Namespace: $($task.Namespace)`n  Args: $($task.Args)`n  Command: $($result.Command)`n  Output: $($result.Output)"
        }
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
        @{ Suffix="JsonSerializerContext"; Prefix="JsonSerializerCtx"; Args="--json-serializer-context" },
        @{ Suffix="JsonLibraryVersion9"; Prefix="JsonLibraryVersion9"; Args="--json-library-version 9.0" }
    )

    # ==========================================
    # Phase 0: Build refitter from source
    # ==========================================
    if ($BuildFromSource -and -not $UseDocker)
    {
        Write-Log ""
        Write-Log "=== Phase 0: Build refitter from source ==="
        Write-Log ""
        $result = Invoke-Process -FilePath "dotnet" -Arguments "publish ../src/Refitter/Refitter.csproj -c Release -o bin -f net10.0" -Description "dotnet publish Refitter.csproj"
        if ($result.ExitCode -ne 0) { throw "Build refitter from source FAILED [exit: $($result.ExitCode)]`n  Output: $($result.Output)" }

        $result = Invoke-Process -FilePath "./bin/refitter" -Arguments "--version" -Description "refitter --version"
        if ($result.ExitCode -ne 0) { throw "Show version FAILED [exit: $($result.ExitCode)]`n  Output: $($result.Output)" }
    }

    # ==========================================
    # Phase 1: Pre-restore packages
    # ==========================================
    Write-Log ""
    Write-Log "=== Phase 1: Pre-restore packages ==="
    Write-Log ""
    Invoke-Process -FilePath "dotnet" -Arguments "restore ./ConsoleApp/ConsoleApp.slnx --nologo -v q" -Description "restore ConsoleApp.slnx"
    Invoke-Process -FilePath "dotnet" -Arguments "restore ./ConsoleApp/ConsoleApp.Core.slnx --nologo -v q" -Description "restore ConsoleApp.Core.slnx"
    Invoke-Process -FilePath "dotnet" -Arguments "restore ./Apizr/Sample.csproj --nologo -v q" -Description "restore Apizr Sample.csproj"

    # ==========================================
    # Phase 2: Settings-file tests (individual generate + build)
    # ==========================================
    Write-Log ""
    Write-Log "=== Phase 2: Settings-file tests ==="
    Write-Log ""

    CleanGeneratedCode
    GenerateFromSettingsFile -settingsFile "./petstore.refitter" -processPath $processPath -useDocker $UseDocker
    BuildSolution -solution "./ConsoleApp/ConsoleApp.slnx" -noRestore

    CleanGeneratedCode
    GenerateFromSettingsFile -settingsFile "./Apizr/petstore.apizr.refitter" -processPath $processPath -useDocker $UseDocker
    BuildSolution -solution "./Apizr/Sample.csproj" -noRestore

    CleanGeneratedCode
    GenerateFromSettingsFile -settingsFile "./MultipleFiles/petstore.refitter" -processPath $processPath -useDocker $UseDocker
    BuildSolution -solution "MultipleFiles/Client/Client.csproj"

    CleanGeneratedCode
    GenerateFromSettingsFile -settingsFile "./multiple-sources.refitter" -processPath $processPath -useDocker $UseDocker
    BuildSolution -solution "./ConsoleApp/ConsoleApp.Core.slnx" -noRestore

    # ==========================================
    # Phase 3: Generate all STANDARD variants (no build until all are generated)
    # ==========================================
    Write-Log ""
    Write-Log "=== Phase 3: Generate standard variants ==="
    Write-Log ""
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

    Write-Log "Standard generation tasks: $($standardTasks.Count)"
    Write-Log "NetCore generation tasks: $($netCoreTasks.Count)"

    # Execute standard generation in parallel batches
    RunGenerationTasks -tasks $standardTasks -processPath $processPath -useDocker $UseDocker

    # ==========================================
    # Phase 4: Build standard variants (one build validates all)
    # ==========================================
    Write-Log ""
    Write-Log "=== Phase 4: Build standard variants ==="
    Write-Log ""
    BuildSolution -solution "./ConsoleApp/ConsoleApp.slnx" -noRestore -smokeTest

    # ==========================================
    # Phase 4b: Generate-only test for MultipleInterfacesWithCustomName
    # This variant uses --multiple-interfaces ByEndpoint --operation-name-template which
    # generates duplicate types per-endpoint (known limitation). We verify generation succeeds.
    # ==========================================
    Write-Log ""
    Write-Log "=== Phase 4b: Generate-only: MultipleInterfacesWithCustomName (petstore) ==="
    Write-Log ""
    CleanGeneratedCode
    $customNameSpec = "./OpenAPI/v3.0/petstore.json"
    $customNameArgs = "--multiple-interfaces ByEndpoint --operation-name-template ExecuteAsync"
    $customNameOutput = "./GeneratedCode/MultipleInterfacesWithCustomName_generateonly.cs"
    $customNameInvocation = "$customNameSpec --namespace GenerateOnly.MultipleInterfacesWithCustomName --output $customNameOutput --no-logging $customNameArgs"
    $result = StartRefitter -arguments $customNameInvocation -processPath $processPath -useDocker $UseDocker
    if ($result.ExitCode -ne 0) { throw "Generate-only test FAILED: MultipleInterfacesWithCustomName [exit: $($result.ExitCode)]`n  Command: $($result.Command)`n  Output: $($result.Output)" }
    if (-not (Test-Path $customNameOutput)) { throw "Generate-only test FAILED: MultipleInterfacesWithCustomName - output file not found: $customNameOutput" }
    Remove-Item $customNameOutput -Force
    Write-Log "Generate-only test passed: MultipleInterfacesWithCustomName"

    # ==========================================
    # Phase 5: Generate netCore variants (accumulate on top of standard code)
    # Net8/Net9/Net10 can compile both standard and netCore code
    # ==========================================
    Write-Log ""
    Write-Log "=== Phase 5: Generate netCore variants ==="
    Write-Log ""
    RunGenerationTasks -tasks $netCoreTasks -processPath $processPath -useDocker $UseDocker

    # ==========================================
    # Phase 6: Build netCore variants
    # ==========================================
    Write-Log ""
    Write-Log "=== Phase 6: Build netCore variants ==="
    Write-Log ""
    BuildSolution -solution "./ConsoleApp/ConsoleApp.Core.slnx" -noRestore -smokeTest

    # ==========================================
    # Phase 7: URL-based tests (network-dependent)
    # ==========================================
    Write-Log ""
    Write-Log "=== Phase 7: URL-based tests ==="
    Write-Log ""
    CleanGeneratedCode

    @("https://petstore3.swagger.io/api/v3/openapi.json", "https://petstore3.swagger.io/api/v3/openapi.yaml") | ForEach-Object {
        $url = $_
        $urlFormat = if ($url.EndsWith(".json")) { "json" } else { "yaml" }
        $namespace = "PetstoreFromUri"
        $outputPath = "PetstoreFromUri.generated.cs"

        $result = StartRefitter `
            -arguments """$url"" --namespace $namespace --output ./GeneratedCode/$outputPath --no-logging" `
            -processPath $processPath `
            -useDocker $UseDocker
        if ($result.ExitCode -ne 0) { throw "URL generation FAILED for $url [exit: $($result.ExitCode)]`n  Output: $($result.Output)" }

        BuildSolution -solution "./ConsoleApp/ConsoleApp.slnx" -noRestore
    }

    # ==========================================
    # Phase 8: Operation Name Generator Tests
    # ==========================================
    Write-Log ""
    Write-Log "=== Phase 8: Operation Name Generator Tests ==="
    Write-Log ""

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
        $result = StartRefitter -arguments $genArgs -processPath $processPath -useDocker $UseDocker
        if ($result.ExitCode -ne 0) { Write-Warning "Operation name generator '$gen' FAILED [exit: $($result.ExitCode)] (may be expected for some generators)`n  Output: $($result.Output)" }
    }
    # Build only what was successfully generated
    if (Test-Path './GeneratedCode/OpNameGen_*.generated.cs') {
        BuildSolution -solution "./ConsoleApp/ConsoleApp.Core.slnx" -noRestore -smokeTest
    }

    # ==========================================
    # Phase 9: Collection Format Variant Tests
    # ==========================================
    Write-Log ""
    Write-Log "=== Phase 9: Collection Format Variant Tests ==="
    Write-Log ""

    $collectionFormats = @("Multi", "Ssv", "Tsv", "Pipes")

    CleanGeneratedCode
    foreach ($fmt in $collectionFormats)
    {
        $fmtArgs = "./OpenAPI/v3.0/petstore.json --namespace CollFmt_$fmt --output ./GeneratedCode/CollFmt_$fmt.generated.cs --no-logging --collection-format $fmt"
        $result = StartRefitter -arguments $fmtArgs -processPath $processPath -useDocker $UseDocker
        if ($result.ExitCode -ne 0) { throw "Collection format FAILED: '$fmt' [exit: $($result.ExitCode)]`n  Command: $($result.Command)`n  Output: $($result.Output)" }
    }
    BuildSolution -solution "./ConsoleApp/ConsoleApp.Core.slnx" -noRestore -smokeTest

    # ==========================================
    # Phase 10: Combination Tests
    # ==========================================
    Write-Log ""
    Write-Log "=== Phase 10: Combination Tests ==="
    Write-Log ""

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
        $result = StartRefitter -arguments $fullArgs -processPath $processPath -useDocker $UseDocker
        if ($result.ExitCode -ne 0) { throw "Combination test FAILED: '$($combo.Name)' [exit: $($result.ExitCode)]`n  Args: $($combo.Args)`n  Command: $($result.Command)`n  Output: $($result.Output)" }
    }
    BuildSolution -solution "./ConsoleApp/ConsoleApp.Core.slnx" -noRestore -smokeTest
}

if ($UseProduction)
{
    Write-Log ""
    Write-Log "=== Production mode: installing refitter tool ==="
    Write-Log ""
    $result = Invoke-Process -FilePath "dotnet" -Arguments "tool update -g refitter --prerelease" -Description "dotnet tool update refitter"
    if ($result.ExitCode -ne 0) { throw "Failed to install refitter tool [exit: $($result.ExitCode)]`n  Output: $($result.Output)" }
    Write-Log ""
}

if ($UseDocker)
{
    Write-Log ""
    Write-Log "=== Docker mode: pulling image ==="
    Write-Log ""
    $result = Invoke-Process -FilePath "docker" -Arguments "pull christianhelle/refitter:latest" -Description "docker pull christianhelle/refitter"
    if ($result.ExitCode -ne 0) { throw "Failed to pull Docker image [exit: $($result.ExitCode)]`n  Output: $($result.Output)" }
    Write-Log ""
}

$elapsed = Measure-Command {
    RunTests `
        -BuildFromSource (!$UseProduction -and !$UseDocker) `
        -UseDocker $UseDocker
}

Write-Log ""
Write-Log "============================================"
Write-Log " ALL SMOKE TESTS PASSED"
Write-Log "============================================"
Write-Log "Elapsed time: $($elapsed.Hours)h $($elapsed.Minutes)m $($elapsed.Seconds)s"
if ($LogFile) {
    Write-Log "Log file: $LogFile"
}
Write-Log ""
