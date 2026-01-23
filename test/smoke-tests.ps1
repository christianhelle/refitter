param (
    [Parameter(Mandatory=$false)]
    [bool]
    $Parallel = $true,

    [Parameter(Mandatory=$false)]
    [switch]
    $UseProduction = $false,

    [Parameter(Mandatory=$false)]
    [switch]
    $UseDocker = $false
)

function ThrowOnNativeFailure
{
    if (-not $?)
    {
        throw "Native Failure"
    }
}

function GenerateAndBuild
{
    param (
        [Parameter(Mandatory=$true)]
        [string]
        $format,

        [Parameter(Mandatory=$true)]
        [string]
        $namespace,

        [Parameter(Mandatory=$false)]
        [string]
        $outputPath,

        [Parameter(Mandatory=$false)]
        [string]
        $args,

        [Parameter(Mandatory=$false)]
        [switch]
        $netCore = $false,

        [Parameter(Mandatory=$false)]
        [string]
        $csproj,

        [Parameter(Mandatory=$false)]
        [bool]
        $buildFromSource = $true,

        [Parameter(Mandatory=$false)]
        [bool]
        $useDocker = $false
    )

    try
    {
        Get-ChildItem './GeneratedCode/*.cs' -Recurse | ForEach-Object { Remove-Item -Path $_.FullName -Force }
    } catch
    {
        # Do nothing
    }

    $processPath = "./bin/refitter"
    if ($buildFromSource -eq $false)
    {
        $processPath = "refitter"
    }
    if ($useDocker)
    {
        $processPath = "docker"
    }

    if ($useDocker)
    {
        $currentDir = (Get-Location).Path.Replace('\', '/')
        if ($args.Contains("settings-file"))
        {
            Write-Host "docker run --rm -v ""${currentDir}:/src"" -w /src christianhelle/refitter --no-logging $args"
            $process = Start-Process $processPath `
                -Args "run --rm -v ""${currentDir}:/src"" -w /src christianhelle/refitter --no-logging $args" `
                -NoNewWindow `
                -PassThru
        } else
        {
            Write-Host "docker run --rm -v ""${currentDir}:/src"" -w /src christianhelle/refitter ./openapi.$format --namespace $namespace --output ./GeneratedCode/$outputPath --no-logging $args"
            $process = Start-Process $processPath `
                -Args "run --rm -v ""${currentDir}:/src"" -w /src christianhelle/refitter ./openapi.$format --namespace $namespace --output ./GeneratedCode/$outputPath --no-logging $args" `
                -NoNewWindow `
                -PassThru
        }
    }
    elseif ($args.Contains("settings-file"))
    {
        Write-Host "refitter --no-logging $args"
        $process = Start-Process $processPath `
            -Args "--no-logging $args" `
            -NoNewWindow `
            -PassThru
    } else
    {
        Write-Host "refitter ./openapi.$format --namespace $namespace --output ./GeneratedCode/$outputPath --no-logging $args"
        $process = Start-Process  $processPath `
            -Args "./openapi.$format --namespace $namespace --output ./GeneratedCode/$outputPath --no-logging $args" `
            -NoNewWindow `
            -PassThru
    }

    $process | Wait-Process
    if ($process.ExitCode -ne 0)
    {
        throw "Refitter failed"
    }

    if ($csproj -ne '')
    {
        Write-Host "`r`nBuilding $csproj file`r`n"
        $solution = $csproj
    } else
    {
        Write-Host "`r`nBuilding ConsoleApp`r`n"
        $solution = "./ConsoleApp/ConsoleApp.slnx"
        if ($netCore)
        {
            $solution = "./ConsoleApp/ConsoleApp.Core.slnx"
        }
    }

    $process = Start-Process "dotnet" `
        -Args "build $solution --nologo -v q --property WarningLevel=0 /clp:ErrorsOnly" `
        -NoNewWindow `
        -PassThru
    $process | Wait-Process
    if ($process.ExitCode -ne 0)
    {
        throw "Build Failed!"
    }
}

function RunTests
{
    param (
        [Parameter(Mandatory=$true)]
        [ValidateSet("dotnet-run", "refitter")]
        [string]
        $Method,

        [Parameter(Mandatory=$false)]
        [bool]
        $Parallel = $false,

        [Parameter(Mandatory=$false)]
        [bool]
        $BuildFromSource = $true,

        [Parameter(Mandatory=$false)]
        [bool]
        $UseDocker = $false
    )

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

    if ($BuildFromSource -and -not $UseDocker)
    {
        Write-Host "dotnet publish ../src/Refitter/Refitter.csproj -c Release -o bin -f net10.0"
        Start-Process "dotnet" -Args "publish ../src/Refitter/Refitter.csproj -c Release -o bin -f net10.0" -NoNewWindow -PassThru | Wait-Process

        Write-Host "refitter --version"
        $process = Start-Process "./bin/refitter" `
            -Args " --version" `
            -NoNewWindow `
            -PassThru
        $process | Wait-Process
        if ($process.ExitCode -ne 0)
        {
            throw "Show version failed!"
        }
    }

    GenerateAndBuild -format " " -namespace " " -outputPath "SwaggerPetstoreDirect.generated.cs" -args "--settings-file ./petstore.refitter" -buildFromSource $buildFromSource -useDocker $UseDocker
    GenerateAndBuild -format " " -namespace " " -args "--settings-file ./Apizr/petstore.apizr.refitter" -csproj "./Apizr/Sample.csproj" -buildFromSource $buildFromSource -useDocker $UseDocker
    GenerateAndBuild -format " " -namespace " " -args "--settings-file ./MultipleFiles/petstore.refitter" -csproj "MultipleFiles/Client/Client.csproj" -buildFromSource $buildFromSource -useDocker $UseDocker

    "v3.0", "v2.0" | ForEach-Object {
        $version = $_
        "json", "yaml" | ForEach-Object {
            $format = $_
            $filenames | ForEach-Object {
                $filename = "./OpenAPI/$version/$_.$format"
                $exists = Test-Path -Path $filename -PathType Leaf
                if ($exists -eq $true)
                {
                    Copy-Item $filename ./openapi.$format
                    $outputPath = "$_.generated.cs"
                    $outputPath = $outputPath.Substring(0, 1).ToUpperInvariant() + $outputPath.Substring(1, $outputPath.Length - 1)
                    $namespace = $_.Replace("-", "")
                    $namespace = $namespace.Substring(0, 1).ToUpperInvariant() + $namespace.Substring(1, $namespace.Length - 1)

                    GenerateAndBuild -format $format -namespace "$namespace.Disposable" -outputPath "Disposable$outputPath" -args "--disposable" -buildFromSource $buildFromSource -netCore -useDocker $UseDocker
                    GenerateAndBuild -format $format -namespace "$namespace.MultipleFiles" -args "--multiple-files" -buildFromSource $buildFromSource -useDocker $UseDocker
                    GenerateAndBuild -format $format -namespace "$namespace.SeparateContractsFile" -args "--contracts-output GeneratedCode/Contracts --contracts-namespace $namespace.SeparateContractsFile.Contracts" -buildFromSource $buildFromSource -useDocker $UseDocker
                    GenerateAndBuild -format $format -namespace "$namespace.Cancellation" -outputPath "WithCancellation$outputPath" "--cancellation-tokens" -buildFromSource $buildFromSource -useDocker $UseDocker
                    GenerateAndBuild -format $format -namespace "$namespace.Internal" -outputPath "Internal$outputPath" -args "--internal" -buildFromSource $buildFromSource -useDocker $UseDocker
                    GenerateAndBuild -format $format -namespace "$namespace.UsingApiResponse" -outputPath "IApi$outputPath" -args "--use-api-response" -buildFromSource $buildFromSource -useDocker $UseDocker
                    GenerateAndBuild -format $format -namespace "$namespace.UsingIObservable" -outputPath "IObservable$outputPath" -args "--use-observable-response" -buildFromSource $buildFromSource -useDocker $UseDocker
                    GenerateAndBuild -format $format -namespace "$namespace.UsingIsoDateFormat" -outputPath "UsingIsoDateFormat$outputPath" -args "--use-iso-date-format" -buildFromSource $buildFromSource -useDocker $UseDocker
                    GenerateAndBuild -format $format -namespace "$namespace.MultipleInterfaces" -outputPath "MultipleInterfaces$outputPath" -args "--multiple-interfaces ByEndpoint" -buildFromSource $buildFromSource -useDocker $UseDocker
                    GenerateAndBuild -format $format -namespace "$namespace.MultipleInterfaces" -outputPath "MultipleInterfacesWithCustomName$outputPath" -args "--multiple-interfaces ByEndpoint --operation-name-template ExecuteAsync" -buildFromSource $buildFromSource -useDocker $UseDocker
                    GenerateAndBuild -format $format -namespace "$namespace.TagFiltered" -outputPath "TagFiltered$outputPath" -args "--tag pet --tag user --tag store" -buildFromSource $buildFromSource -useDocker $UseDocker
                    GenerateAndBuild -format $format -namespace "$namespace.MatchPathFiltered" -outputPath "MatchPathFiltered$outputPath" -args "--match-path ^/pet/.*" -buildFromSource $buildFromSource -useDocker $UseDocker
                    GenerateAndBuild -format $format -namespace "$namespace.ContractOnly" -outputPath "ContractOnly$outputPath" -args "--contract-only" -buildFromSource $buildFromSource -useDocker $UseDocker
                    GenerateAndBuild -format $format -namespace "$namespace.ImmutableRecords" -outputPath "ImmutableRecords$outputPath" -args "--immutable-records" -buildFromSource $buildFromSource -netCore -useDocker $UseDocker
                    GenerateAndBuild -format $format -namespace "$namespace.PolymorphicSerialization" -outputPath "PolymorphicSerialization$outputPath" -args "--use-polymorphic-serialization" -buildFromSource $buildFromSource -netCore -useDocker $UseDocker
                    GenerateAndBuild -format $format -namespace "$namespace.CollectionFormatCsv" -outputPath "CollectionFormatCsv$outputPath" -args "--collection-format csv" -buildFromSource $buildFromSource -netCore -useDocker $UseDocker
                }
            }
        }
    }

    "https://petstore3.swagger.io/api/v3/openapi.json", "https://petstore3.swagger.io/api/v3/openapi.yaml" | ForEach-Object {
        $namespace = "PetstoreFromUri"
        $outputPath = "PetstoreFromUri.generated.cs"

        Get-ChildItem '*.generated.cs' -Recurse | foreach { Remove-Item -Path $_.FullName }

        $processPath = "./bin/refitter"
        if ($buildFromSource -eq $false)
        {
            $processPath = "refitter"
        }
        if ($UseDocker)
        {
            $processPath = "docker"
        }

        if ($UseDocker)
        {
            $currentDir = (Get-Location).Path.Replace('\', '/')
            Write-Host "docker run --rm -v ""${currentDir}:/src"" -w /src christianhelle/refitter ""$_"" --namespace $namespace --output ./GeneratedCode/$outputPath"
            $process = Start-Process $processPath `
                -Args "run --rm -v ""${currentDir}:/src"" -w /src christianhelle/refitter ""$_"" --namespace $namespace --output ./GeneratedCode/$outputPath" `
                -NoNewWindow `
                -PassThru
        }
        else
        {
            Write-Host "refitter ./openapi.$format --namespace $namespace --output ./GeneratedCode/$outputPath --no-logging $args"
            $process = Start-Process $processPath `
                -Args """$_"" --namespace $namespace --output ./GeneratedCode/$outputPath" `
                -NoNewWindow `
                -PassThru
        }
        $process | Wait-Process
        if ($process.ExitCode -ne 0)
        {
            throw "Refitter failed"
        }

        Write-Host "`r`nBuilding ConsoleApp`r`n"
        $process = Start-Process "dotnet" `
            -Args "build ./ConsoleApp/ConsoleApp.slnx" `
            -NoNewWindow `
            -PassThru
        $process | Wait-Process
        if ($process.ExitCode -ne 0)
        {
            throw "Build Failed!"
        }
    }
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

Measure-Command { RunTests -Method "dotnet-run" -Parallel $Parallel -BuildFromSource (!$UseProduction -and !$UseDocker) -UseDocker $UseDocker }
Write-Host "`r`n"
Write-Host "`r`n"
