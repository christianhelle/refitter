param (
    [Parameter(Mandatory=$false)]
    [bool]
    $Parallel = $true
)

function ThrowOnNativeFailure {
    if (-not $?)
    {
        throw "Native Failure"
    }
}

function GenerateAndBuild {
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
        [bool]
        $netCore = $false,

        [Parameter(Mandatory=$false)]
        [string]
        $csproj
    )
    
    Get-ChildItem './GeneratedCode/*.cs' -Recurse | ForEach-Object { Remove-Item -Path $_.FullName -Force }

    if ($args.Contains("settings-file")) {        
        Write-Host "refitter --no-logging $args"
        $process = Start-Process "./bin/refitter" `
            -Args "--no-logging $args" `
            -NoNewWindow `
            -PassThru
    } else {        
        Write-Host "refitter ./openapi.$format --namespace $namespace --output ./GeneratedCode/$outputPath --no-logging $args"
        $process = Start-Process "./bin/refitter" `
            -Args "./openapi.$format --namespace $namespace --output ./GeneratedCode/$outputPath --no-logging $args" `
            -NoNewWindow `
            -PassThru
    }

    $process | Wait-Process
    if ($process.ExitCode -ne 0) {
        throw "Refitter failed"
    }

    if ($csproj -ne '') {
      Write-Host "`r`nBuilding $csproj file`r`n"
      $solution = $csproj
    } else {
      Write-Host "`r`nBuilding ConsoleApp`r`n"
      $solution = "./ConsoleApp/ConsoleApp.sln"
      if ($netCore) {
          $solution = "./ConsoleApp/ConsoleApp.Core.sln"
      }
    }

    $process = Start-Process "dotnet" `
        -Args "build $solution" `
        -NoNewWindow `
        -PassThru
    $process | Wait-Process
    if ($process.ExitCode -ne 0) {
        throw "Build Failed!"
    }
}

function RunTests {
    param (
        [Parameter(Mandatory=$true)]
        [ValidateSet("dotnet-run", "refitter")]
        [string]
        $Method,
        
        [Parameter(Mandatory=$false)]
        [bool]
        $Parallel = $false
    )

    $filenames = @(        
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
    
    Write-Host "dotnet publish ../src/Refitter/Refitter.csproj -p:PublishReadyToRun=true -o bin -f net8.0"
    Start-Process "dotnet" -Args "publish ../src/Refitter/Refitter.csproj -p:PublishReadyToRun=true -o bin -f net8.0" -NoNewWindow -PassThru | Wait-Process
    
    GenerateAndBuild -format " " -namespace " " -outputPath "SwaggerPetstoreDirect.generated.cs" -args "--settings-file ./petstore.refitter"
    GenerateAndBuild -format " " -namespace " " -args "--settings-file ./Apizr/petstore.apizr.refitter" -csproj "./Apizr/Sample.csproj"

    "v3.0", "v2.0" | ForEach-Object {
        $version = $_
        "json", "yaml" | ForEach-Object {            
            $format = $_
            $filenames | ForEach-Object {
                $filename = "./OpenAPI/$version/$_.$format"
                $exists = Test-Path -Path $filename -PathType Leaf
                if ($exists -eq $true) {
                    Copy-Item $filename ./openapi.$format
                    $outputPath = "$_.generated.cs"
                    $outputPath = $outputPath.Substring(0, 1).ToUpperInvariant() + $outputPath.Substring(1, $outputPath.Length - 1)
                    $namespace = $_.Replace("-", "")
                    $namespace = $namespace.Substring(0, 1).ToUpperInvariant() + $namespace.Substring(1, $namespace.Length - 1)

                    GenerateAndBuild -format $format -namespace "$namespace.MultipleFiles" -args "--multiple-files"
                    GenerateAndBuild -format $format -namespace "$namespace.Cancellation" -outputPath "WithCancellation$outputPath" "--cancellation-tokens"
                    GenerateAndBuild -format $format -namespace "$namespace.Internal" -outputPath "Internal$outputPath" -args "--internal"
                    GenerateAndBuild -format $format -namespace "$namespace.UsingApiResponse" -outputPath "IApi$outputPath" -args "--use-api-response"
                    GenerateAndBuild -format $format -namespace "$namespace.UsingIObservable" -outputPath "IObservable$outputPath" -args "--use-observable-response"
                    GenerateAndBuild -format $format -namespace "$namespace.UsingIsoDateFormat" -outputPath "UsingIsoDateFormat$outputPath" -args "--use-iso-date-format"
                    GenerateAndBuild -format $format -namespace "$namespace.MultipleInterfaces" -outputPath "MultipleInterfaces$outputPath" -args "--multiple-interfaces ByEndpoint"
                    GenerateAndBuild -format $format -namespace "$namespace.MultipleInterfaces" -outputPath "MultipleInterfacesWithCustomName$outputPath" -args "--multiple-interfaces ByEndpoint --operation-name-template ExecuteAsync"
                    GenerateAndBuild -format $format -namespace "$namespace.TagFiltered" -outputPath "TagFiltered$outputPath" -args "--tag pet --tag user --tag store"
                    GenerateAndBuild -format $format -namespace "$namespace.MatchPathFiltered" -outputPath "MatchPathFiltered$outputPath" -args "--match-path ^/pet/.*"
                    GenerateAndBuild -format $format -namespace "$namespace.ContractOnly" -outputPath "ContractOnly$outputPath" -args "--contract-only"
                    GenerateAndBuild -format $format -namespace "$namespace.ImmutableRecords" -outputPath "ImmutableRecords$outputPath" -args "--immutable-records" -netCore $true
                }
            }
        }
    }        
    
    "https://petstore3.swagger.io/api/v3/openapi.json", "https://petstore3.swagger.io/api/v3/openapi.yaml" | ForEach-Object {
        $namespace = "PetstoreFromUri"
        $outputPath = "PetstoreFromUri.generated.cs"
        
        Get-ChildItem '*.generated.cs' -Recurse | foreach { Remove-Item -Path $_.FullName }

        Write-Host "refitter ./openapi.$format --namespace $namespace --output ./GeneratedCode/$outputPath --no-logging $args"
        $process = Start-Process "./bin/refitter" `
            -Args """$_"" --namespace $namespace --output ./GeneratedCode/$outputPath" `
            -NoNewWindow `
            -PassThru
        $process | Wait-Process
        if ($process.ExitCode -ne 0) {
            throw "Refitter failed"
        }
        
        Write-Host "`r`nBuilding ConsoleApp`r`n"
        $process = Start-Process "dotnet" `
            -Args "build ./ConsoleApp/ConsoleApp.sln" `
            -NoNewWindow `
            -PassThru
        $process | Wait-Process
        if ($process.ExitCode -ne 0) {
            throw "Build Failed!"
        }
    }
}

Measure-Command { RunTests -Method "dotnet-run" -Parallel $Parallel }
Write-Host "`r`n"
