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

    $runtime = [System.Runtime.InteropServices.RuntimeInformation]::RuntimeIdentifier
    $process = Start-Process "dotnet" `
        -Args "publish ../src/Refitter/Refitter.csproj -r $runtime -c Release --self-contained" `
        -NoNewWindow `
        -PassThru
    $process | Wait-Process
    if ($process.ExitCode -ne 0) {
        throw "Building refitter failed"
    }

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

                    Write-Host "../src/Refitter/bin/Release/net6.0/win-x64/refitter.exe ./openapi.$format --namespace $namespace --output ./GeneratedCode/$outputPath --no-logging"
                    $process = Start-Process "../src/Refitter/bin/Release/net6.0/win-x64/refitter.exe" `
                        -Args "./openapi.$format --namespace $namespace --output ./GeneratedCode/$outputPath --no-logging" `
                        -NoNewWindow `
                        -PassThru
                    $process | Wait-Process
                    if ($process.ExitCode -ne 0) {
                        throw "Refitter failed"
                    }

                    Write-Host "../src/Refitter/bin/Release/net6.0/win-x64/refitter.exe ./openapi.$format --namespace $namespace.Cancellation --output ./GeneratedCode/WithCancellation$outputPath --cancellation-tokens --no-logging"
                    $process = Start-Process "../src/Refitter/bin/Release/net6.0/win-x64/refitter.exe" `
                        -Args "./openapi.$format --namespace $namespace.Cancellation --output ./GeneratedCode/WithCancellation$outputPath --cancellation-tokens --no-logging" `
                        -NoNewWindow `
                        -PassThru
                    $process | Wait-Process
                    if ($process.ExitCode -ne 0) {
                        throw "Refitter failed"
                    }

                    Write-Host "../src/Refitter/bin/Release/net6.0/win-x64/refitter.exe ./openapi.$format --namespace $namespace.Internal --output ./GeneratedCode/Internal$outputPath --internal --no-logging"
                    $process = Start-Process "../src/Refitter/bin/Release/net6.0/win-x64/refitter.exe" `
                        -Args "./openapi.$format --namespace $namespace.Internal --output ./GeneratedCode/Internal$outputPath --internal --no-logging" `
                        -NoNewWindow `
                        -PassThru
                    $process | Wait-Process
                    if ($process.ExitCode -ne 0) {
                        throw "Refitter failed"
                    }

                    Write-Host "../src/Refitter/bin/Release/net6.0/win-x64/refitter.exe ./openapi.$format --namespace $namespace.Interface --output ./GeneratedCode/I$outputPath --interface-only --no-logging"
                    $process = Start-Process "../src/Refitter/bin/Release/net6.0/win-x64/refitter.exe" `
                        -Args "./openapi.$format --namespace $namespace.Interface --output ./GeneratedCode/I$outputPath --interface-only --no-logging" `
                        -NoNewWindow `
                        -PassThru
                    $process | Wait-Process
                    if ($process.ExitCode -ne 0) {
                        throw "Refitter failed"
                    }

                    Write-Host "../src/Refitter/bin/Release/net6.0/win-x64/refitter.exe ./openapi.$format --namespace $namespace.UsingApiResponse --output ./GeneratedCode/IApi$outputPath --use-api-response --interface-only --no-logging"
                    $process = Start-Process "../src/Refitter/bin/Release/net6.0/win-x64/refitter.exe" `
                        -Args "./openapi.$format --namespace $namespace.UsingApiResponse --output ./GeneratedCode/IApi$outputPath --use-api-response --interface-only --no-logging" `
                        -NoNewWindow `
                        -PassThru
                    $process | Wait-Process
                    if ($process.ExitCode -ne 0) {
                        throw "Refitter failed"
                    }

                    Write-Host "../src/Refitter/bin/Release/net6.0/win-x64/refitter.exe ./openapi.$format --namespace $namespace.UsingIsoDateFormat --output ./GeneratedCode/UsingIsoDateFormat$outputPath --use-iso-date-format --no-logging"
                    $process = Start-Process "../src/Refitter/bin/Release/net6.0/win-x64/refitter.exe" `
                        -Args "./openapi.$format --namespace $namespace.UsingIsoDateFormat --output ./GeneratedCode/UsingIsoDateFormat$outputPath --use-iso-date-format --no-logging" `
                        -NoNewWindow `
                        -PassThru
                    $process | Wait-Process
                    if ($process.ExitCode -ne 0) {
                        throw "Refitter failed"
                    }

                    Copy-Item "./GeneratedCode/$outputPath" "./$version-$_-$format.cs"
                    Copy-Item "./GeneratedCode/$outputPath" "./ConsoleApp/Net7/" -Force
                    Copy-Item "./GeneratedCode/$outputPath" "./ConsoleApp/Net6/" -Force
                    Copy-Item "./GeneratedCode/$outputPath" "./ConsoleApp/Net48/" -Force
                    Copy-Item "./GeneratedCode/$outputPath" "./ConsoleApp/Net481/" -Force
                    Copy-Item "./GeneratedCode/$outputPath" "./ConsoleApp/Net472/" -Force
                    Copy-Item "./GeneratedCode/$outputPath" "./ConsoleApp/Net462/" -Force
                    Copy-Item "./GeneratedCode/$outputPath" "./ConsoleApp/NetStandard20/" -Force
                    Copy-Item "./GeneratedCode/$outputPath" "./ConsoleApp/NetStandard21/" -Force
                    Copy-Item "./GeneratedCode/$outputPath" "./MinimalApi/" -Force
                    Copy-Item "./GeneratedCode/I$outputPath" "./ConsoleApp/Net7/" -Force
                    Copy-Item "./GeneratedCode/I$outputPath" "./ConsoleApp/Net6/" -Force
                    Copy-Item "./GeneratedCode/I$outputPath" "./ConsoleApp/Net48/" -Force
                    Copy-Item "./GeneratedCode/I$outputPath" "./ConsoleApp/Net481/" -Force
                    Copy-Item "./GeneratedCode/I$outputPath" "./ConsoleApp/Net472/" -Force
                    Copy-Item "./GeneratedCode/I$outputPath" "./ConsoleApp/Net462/" -Force
                    Copy-Item "./GeneratedCode/I$outputPath" "./ConsoleApp/NetStandard20/" -Force
                    Copy-Item "./GeneratedCode/I$outputPath" "./ConsoleApp/NetStandard21/" -Force
                    Copy-Item "./GeneratedCode/I$outputPath" "./MinimalApi/" -Force
                    Copy-Item "./GeneratedCode/IApi$outputPath" "./ConsoleApp/Net7/" -Force
                    Copy-Item "./GeneratedCode/IApi$outputPath" "./ConsoleApp/Net6/" -Force
                    Copy-Item "./GeneratedCode/IApi$outputPath" "./ConsoleApp/Net48/" -Force
                    Copy-Item "./GeneratedCode/IApi$outputPath" "./ConsoleApp/Net481/" -Force
                    Copy-Item "./GeneratedCode/IApi$outputPath" "./ConsoleApp/Net472/" -Force
                    Copy-Item "./GeneratedCode/IApi$outputPath" "./ConsoleApp/Net462/" -Force
                    Copy-Item "./GeneratedCode/IApi$outputPath" "./ConsoleApp/NetStandard20/" -Force
                    Copy-Item "./GeneratedCode/IApi$outputPath" "./ConsoleApp/NetStandard21/" -Force
                    Copy-Item "./GeneratedCode/IApi$outputPath" "./MinimalApi/" -Force
                    Copy-Item "./GeneratedCode/Internal$outputPath" "./ConsoleApp/Net7/" -Force
                    Copy-Item "./GeneratedCode/Internal$outputPath" "./ConsoleApp/Net6/" -Force
                    Copy-Item "./GeneratedCode/Internal$outputPath" "./ConsoleApp/Net48/" -Force
                    Copy-Item "./GeneratedCode/Internal$outputPath" "./ConsoleApp/Net481/" -Force
                    Copy-Item "./GeneratedCode/Internal$outputPath" "./ConsoleApp/Net472/" -Force
                    Copy-Item "./GeneratedCode/Internal$outputPath" "./ConsoleApp/Net462/" -Force
                    Copy-Item "./GeneratedCode/Internal$outputPath" "./ConsoleApp/NetStandard20/" -Force
                    Copy-Item "./GeneratedCode/Internal$outputPath" "./ConsoleApp/NetStandard21/" -Force
                    Copy-Item "./GeneratedCode/Internal$outputPath" "./MinimalApi/" -Force
                    Copy-Item "./GeneratedCode/WithCancellation$outputPath" "./ConsoleApp/Net7/" -Force
                    Copy-Item "./GeneratedCode/WithCancellation$outputPath" "./ConsoleApp/Net6/" -Force
                    Copy-Item "./GeneratedCode/WithCancellation$outputPath" "./ConsoleApp/Net48/" -Force
                    Copy-Item "./GeneratedCode/WithCancellation$outputPath" "./ConsoleApp/Net481/" -Force
                    Copy-Item "./GeneratedCode/WithCancellation$outputPath" "./ConsoleApp/Net472/" -Force
                    Copy-Item "./GeneratedCode/WithCancellation$outputPath" "./ConsoleApp/Net462/" -Force
                    Copy-Item "./GeneratedCode/WithCancellation$outputPath" "./ConsoleApp/NetStandard20/" -Force
                    Copy-Item "./GeneratedCode/WithCancellation$outputPath" "./ConsoleApp/NetStandard21/" -Force
                    Copy-Item "./GeneratedCode/WithCancellation$outputPath" "./MinimalApi/" -Force
                    Copy-Item "./GeneratedCode/UsingIsoDateFormat$outputPath" "./ConsoleApp/Net7/" -Force
                    Copy-Item "./GeneratedCode/UsingIsoDateFormat$outputPath" "./ConsoleApp/Net6/" -Force
                    Copy-Item "./GeneratedCode/UsingIsoDateFormat$outputPath" "./ConsoleApp/Net48/" -Force
                    Copy-Item "./GeneratedCode/UsingIsoDateFormat$outputPath" "./ConsoleApp/Net481/" -Force
                    Copy-Item "./GeneratedCode/UsingIsoDateFormat$outputPath" "./ConsoleApp/Net472/" -Force
                    Copy-Item "./GeneratedCode/UsingIsoDateFormat$outputPath" "./ConsoleApp/Net462/" -Force
                    Copy-Item "./GeneratedCode/UsingIsoDateFormat$outputPath" "./ConsoleApp/NetStandard20/" -Force
                    Copy-Item "./GeneratedCode/UsingIsoDateFormat$outputPath" "./ConsoleApp/NetStandard21/" -Force
                    Copy-Item "./GeneratedCode/UsingIsoDateFormat$outputPath" "./MinimalApi/" -Force
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
    }        
    
    "https://petstore3.swagger.io/api/v3/openapi.json", "https://petstore3.swagger.io/api/v3/openapi.yaml" | ForEach-Object {
        $namespace = "PetstoreFromUri"
        $outputPath = "PetstoreFromUri.cs"

        Write-Host "../src/Refitter/bin/Release/net6.0/win-x64/refitter.exe ""$_"" --namespace $namespace --output ./GeneratedCode/$outputPath"
        $process = Start-Process "../src/Refitter/bin/Release/net6.0/win-x64/refitter.exe" `
            -Args """$_"" --namespace $namespace --output ./GeneratedCode/$outputPath" `
            -NoNewWindow `
            -PassThru
        $process | Wait-Process
        if ($process.ExitCode -ne 0) {
            throw "Refitter failed"
        }

        Write-Host "../src/Refitter/bin/Release/net6.0/win-x64/refitter.exe ""$_"" --namespace $namespace.Internal --output ./GeneratedCode/Internal$outputPath --internal --no-logging"
        $process = Start-Process "../src/Refitter/bin/Release/net6.0/win-x64/refitter.exe" `
            -Args """$_"" --namespace $namespace.Internal --output ./GeneratedCode/Internal$outputPath --internal --no-logging" `
            -NoNewWindow `
            -PassThru
        $process | Wait-Process
        if ($process.ExitCode -ne 0) {
            throw "Refitter failed"
        }

        Write-Host "../src/Refitter/bin/Release/net6.0/win-x64/refitter.exe """$_""" --namespace $namespace.Interface --output ./GeneratedCode/I$outputPath --interface-only --no-logging"
        $process = Start-Process "../src/Refitter/bin/Release/net6.0/win-x64/refitter.exe" `
            -Args """$_"" --namespace $namespace.Interface --output ./GeneratedCode/I$outputPath --interface-only --no-logging" `
            -NoNewWindow `
            -PassThru
        $process | Wait-Process
        if ($process.ExitCode -ne 0) {
            throw "Refitter failed"
        }

        Write-Host "../src/Refitter/bin/Release/net6.0/win-x64/refitter.exe ""$_"" --namespace $namespace.UsingApiResponse --output ./GeneratedCode/I$outputPath --use-api-response --interface-only --no-logging"
        $process = Start-Process "../src/Refitter/bin/Release/net6.0/win-x64/refitter.exe" `
            -Args """$_"" --namespace $namespace.UsingApiResponse --output ./GeneratedCode/IApi$outputPath --use-api-response --interface-only --no-logging" `
            -NoNewWindow `
            -PassThru
        $process | Wait-Process
        if ($process.ExitCode -ne 0) {
            throw "Refitter failed"
        }

        Copy-Item "./GeneratedCode/$outputPath" "./ConsoleApp/Net7/" -Force
        Copy-Item "./GeneratedCode/$outputPath" "./ConsoleApp/Net6/" -Force
        Copy-Item "./GeneratedCode/$outputPath" "./ConsoleApp/Net48/" -Force
        Copy-Item "./GeneratedCode/$outputPath" "./ConsoleApp/Net481/" -Force
        Copy-Item "./GeneratedCode/$outputPath" "./ConsoleApp/Net472/" -Force
        Copy-Item "./GeneratedCode/$outputPath" "./ConsoleApp/Net462/" -Force
        Copy-Item "./GeneratedCode/$outputPath" "./ConsoleApp/NetStandard20/" -Force
        Copy-Item "./GeneratedCode/$outputPath" "./ConsoleApp/NetStandard21/" -Force
        Copy-Item "./GeneratedCode/$outputPath" "./MinimalApi/" -Force
        Copy-Item "./GeneratedCode/I$outputPath" "./ConsoleApp/Net7/" -Force
        Copy-Item "./GeneratedCode/I$outputPath" "./ConsoleApp/Net6/" -Force
        Copy-Item "./GeneratedCode/I$outputPath" "./ConsoleApp/Net48/" -Force
        Copy-Item "./GeneratedCode/I$outputPath" "./ConsoleApp/Net481/" -Force
        Copy-Item "./GeneratedCode/I$outputPath" "./ConsoleApp/Net472/" -Force
        Copy-Item "./GeneratedCode/I$outputPath" "./ConsoleApp/Net462/" -Force
        Copy-Item "./GeneratedCode/I$outputPath" "./ConsoleApp/NetStandard20/" -Force
        Copy-Item "./GeneratedCode/I$outputPath" "./ConsoleApp/NetStandard21/" -Force
        Copy-Item "./GeneratedCode/I$outputPath" "./MinimalApi/" -Force
        Copy-Item "./GeneratedCode/IApi$outputPath" "./ConsoleApp/Net7/" -Force
        Copy-Item "./GeneratedCode/IApi$outputPath" "./ConsoleApp/Net6/" -Force
        Copy-Item "./GeneratedCode/IApi$outputPath" "./ConsoleApp/Net48/" -Force
        Copy-Item "./GeneratedCode/IApi$outputPath" "./ConsoleApp/Net481/" -Force
        Copy-Item "./GeneratedCode/IApi$outputPath" "./ConsoleApp/Net472/" -Force
        Copy-Item "./GeneratedCode/IApi$outputPath" "./ConsoleApp/Net462/" -Force
        Copy-Item "./GeneratedCode/IApi$outputPath" "./ConsoleApp/NetStandard20/" -Force
        Copy-Item "./GeneratedCode/IApi$outputPath" "./ConsoleApp/NetStandard21/" -Force
        Copy-Item "./GeneratedCode/IApi$outputPath" "./MinimalApi/" -Force
        Copy-Item "./GeneratedCode/Internal$outputPath" "./ConsoleApp/Net7/" -Force
        Copy-Item "./GeneratedCode/Internal$outputPath" "./ConsoleApp/Net6/" -Force
        Copy-Item "./GeneratedCode/Internal$outputPath" "./ConsoleApp/Net48/" -Force
        Copy-Item "./GeneratedCode/Internal$outputPath" "./ConsoleApp/Net481/" -Force
        Copy-Item "./GeneratedCode/Internal$outputPath" "./ConsoleApp/Net472/" -Force
        Copy-Item "./GeneratedCode/Internal$outputPath" "./ConsoleApp/Net462/" -Force
        Copy-Item "./GeneratedCode/Internal$outputPath" "./ConsoleApp/NetStandard20/" -Force
        Copy-Item "./GeneratedCode/Internal$outputPath" "./ConsoleApp/NetStandard21/" -Force
        Copy-Item "./GeneratedCode/Internal$outputPath" "./MinimalApi/" -Force
        
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
}

Remove-Item * -Include *.cs -Recurse -Force -Exclude *Program.cs
Measure-Command { RunTests -Method "dotnet-run" -Parallel $Parallel }
Write-Host "`r`n"