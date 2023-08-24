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
        
        [Parameter(Mandatory=$true)]
        [string]
        $outputPath,
        
        [Parameter(Mandatory=$false)]
        [string]
        $args
    )
    
    Get-ChildItem '*.generated.cs' -Recurse | foreach { Remove-Item -Path $_.FullName }

    Write-Host "dotnet run --no-build --project ../src/Refitter/Refitter.csproj ./openapi.$format --namespace $namespace --output ./GeneratedCode/$outputPath --no-logging $args"
    $process = Start-Process "dotnet" `
        -Args "run --no-build --project ../src/Refitter/Refitter.csproj ./openapi.$format --namespace $namespace --output ./GeneratedCode/$outputPath --no-logging $args" `
        -NoNewWindow `
        -PassThru
    $process | Wait-Process
    if ($process.ExitCode -ne 0) {
        throw "Refitter failed"
    }

    Write-Host "`r`nBuilding ConsoleApp`r`n"
    $process = Start-Process "dotnet" `
        -Args "build -p:TreatWarningsAsErrors=true ./ConsoleApp/ConsoleApp.sln" `
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
        "petstore-expanded",
        "petstore-minimal",
        "petstore-simple",
        "petstore-with-external-docs",
        "petstore",
        "ingram-micro",
        "api-with-examples",
        "callback-example",
        "link-example",
        "uber",
        "uspto",
        "hubspot-events",
        "hubspot-webhooks"
    )
    
    Write-Host "dotnet build --project ../src/Refitter/Refitter.csproj"
    Start-Process "dotnet" -Args "build -p:TreatWarningsAsErrors=true ../src/Refitter/Refitter.csproj" -NoNewWindow -PassThru | Wait-Process

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

                    GenerateAndBuild -format $format -namespace $namespace -outputPath $outputPath -args "--settings-file ./SourceGenerator/swagger-petstore-direct.refitter"
                    GenerateAndBuild -format $format -namespace "$namespace.Cancellation" -outputPath "WithCancellation$outputPath" "--cancellation-tokens"
                    GenerateAndBuild -format $format -namespace "$namespace.Internal" -outputPath "Internal$outputPath" -args "--internal"
                    GenerateAndBuild -format $format -namespace "$namespace.UsingApiResponse" -outputPath "IApi$outputPath" -args "--use-api-response"
                    GenerateAndBuild -format $format -namespace "$namespace.UsingIsoDateFormat" -outputPath "UsingIsoDateFormat$outputPath" -args "--use-iso-date-format"
                    GenerateAndBuild -format $format -namespace "$namespace.MultipleInterfaces" -outputPath "MultipleInterfaces$outputPath" -args "--multiple-interfaces ByEndpoint"
                }
            }
        }
    }        
    
    "https://petstore3.swagger.io/api/v3/openapi.json", "https://petstore3.swagger.io/api/v3/openapi.yaml" | ForEach-Object {
        $namespace = "PetstoreFromUri"
        $outputPath = "PetstoreFromUri.generated.cs"
        
        Get-ChildItem '*.generated.cs' -Recurse | foreach { Remove-Item -Path $_.FullName }

        Write-Host "dotnet run --no-build --project ../src/Refitter/Refitter.csproj ""$_"" --namespace $namespace --output ./GeneratedCode/$outputPath"
        $process = Start-Process "dotnet" `
            -Args "run --no-build --project ../src/Refitter/Refitter.csproj ""$_"" --namespace $namespace --output ./GeneratedCode/$outputPath" `
            -NoNewWindow `
            -PassThru
        $process | Wait-Process
        if ($process.ExitCode -ne 0) {
            throw "Refitter failed"
        }
        
        Write-Host "`r`nBuilding ConsoleApp`r`n"
        $process = Start-Process "dotnet" `
            -Args "build -p:TreatWarningsAsErrors=true ./ConsoleApp/ConsoleApp.sln" `
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