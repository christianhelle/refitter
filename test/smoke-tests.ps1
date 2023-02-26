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
        # "api-with-examples",
        "callback-example",
        "link-example",
        "uber",
        "uspto",
        # "petstore-expanded",
        # "petstore-minimal",
        # "petstore-simple",
        # "petstore-with-external-docs",
        "petstore"
    )
    "v2.0", "v3.0" | ForEach-Object {
        $version = $_
        "json", "yaml" | ForEach-Object {            
            $format = $_
            $filenames | ForEach-Object {
                $filename = "./OpenAPI/$version/$_.$format"
                $exists = Test-Path -Path $filename -PathType Leaf
                if ($exists -eq $true) {
                    Copy-Item $filename ./openapi.$format
                    $outputPath = "$_.cs"
                    $outputPath = $outputPath.Substring(0, 1).ToUpperInvariant() + $outputPath.Substring(1, $outputPath.Length - 1)
                    $namespace = $_.Replace("-", "")
                    $namespace = $namespace.Substring(0, 1).ToUpperInvariant() + $namespace.Substring(1, $namespace.Length - 1)

                    $process = Start-Process "dotnet" `
                        -Args "run --project ../src/Refitter/Refitter.csproj ./openapi.$format --namespace $namespace --output $outputPath" `
                        -NoNewWindow `
                        -PassThru
                    $process | Wait-Process
                    if ($process.ExitCode -ne 0) {
                        throw "Refitter failed"
                    }

                    Copy-Item $outputPath "./$version-$_-$format.cs"
                    Copy-Item $outputPath "./ConsoleApp/Net7/" -Force
                    Copy-Item $outputPath "./ConsoleApp/Net6/" -Force
                    Copy-Item $outputPath "./ConsoleApp/Net48/" -Force
                    Copy-Item $outputPath "./ConsoleApp/Net481/" -Force
                    Copy-Item $outputPath "./ConsoleApp/Net472/" -Force
                    Copy-Item $outputPath "./ConsoleApp/Net462/" -Force
                    Copy-Item $outputPath "./ConsoleApp/NetStandard20/" -Force
                    Copy-Item $outputPath "./ConsoleApp/NetStandard21/" -Force
                    Copy-Item $outputPath "./MinimalApi/" -Force
                    Remove-Item $outputPath -Force
                }
            }
        }
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

    Write-Host "`r`nBuilding MinimalApi`r`n"
    $process = Start-Process "dotnet" `
        -Args "build ./MinimalApi/MinimalApi.csproj" `
        -NoNewWindow `
        -PassThru
    $process | Wait-Process
    if ($process.ExitCode -ne 0) {
        throw "Build Failed!"
    }
}

Remove-Item ./v2.0-*.cs -Force
Remove-Item ./v3.0-*.cs -Force
Remove-Item ./v3.1-*.cs -Force
Remove-Item ./**/*Output.cs -Force
Measure-Command { RunTests -Method "dotnet-run" -Parallel $Parallel }
Write-Host "`r`n"