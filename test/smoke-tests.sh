Parallel=true

function ThrowOnNativeFailure {
    if [ $? -ne 0 ]; then
        echo "Native Failure"
        exit 1
    fi
}

function RunTests {

    Method=""
    Parallel=false

    while [[ $# -gt 0 ]]; do
        key="$1"

        case $key in
            -m|--method)
            Method="$2"
            shift # past argument
            shift # past value
            ;;
            -p|--parallel)
            Parallel=true
            shift # past argument
            ;;
            *)    # unknown option
            shift # past argument
            ;;
        esac
    done

    filenames=(
        "petstore-expanded"
        "petstore-minimal"
        "petstore-simple"
        "petstore-with-external-docs"
        "petstore"
        "ingram-micro"
        "api-with-examples"
        "callback-example"
        "link-example"
        "uber"
        "uspto"
        "hubspot-events"
        "hubspot-webhooks"
    )

    for version in "v2.0" "v3.0"; do
        for format in "json" "yaml"; do
            for filename in "${filenames[@]}"; do
                filename="./OpenAPI/$version/$filename.$format"
                if [ -f "$filename" ]; then
                    cp "$filename" ./openapi.$format
                    output_path="$filename.cs"
                    output_path="$(tr '[:lower:]' '[:upper:]' <<< ${output_path:0:1})${output_path:1}"
                    namespace="${filename/-/}"
                    namespace="$(tr '[:lower:]' '[:upper:]' <<< ${namespace:0:1})${namespace:1}"
                    echo "dotnet run --project ../src/Refitter/Refitter.csproj ./openapi.$format --namespace $namespace --output ./GeneratedCode/$output_path --no-logging"
                    dotnet run --project ../src/Refitter/Refitter.csproj ./openapi.$format --namespace $namespace --output ./GeneratedCode/$output_path --no-logging
                    ThrowOnNativeFailure
                    echo "dotnet run --project ../src/Refitter/Refitter.csproj ./openapi.$format --namespace $namespace.Cancellation --output ./GeneratedCode/WithCancellation$output_path --cancellation-tokens --no-logging"
                    dotnet run --project ../src/Refitter/Refitter.csproj ./openapi.$format --namespace $namespace.Cancellation --output ./GeneratedCode/WithCancellation$output_path --cancellation-tokens --no-logging
                    ThrowOnNativeFailure
                    echo "dotnet run --project ../src/Refitter/Refitter.csproj ./openapi.$format --namespace $namespace.Internal --output ./GeneratedCode/Internal$output_path --internal --no-logging"
                    dotnet run --project ../src/Refitter/Refitter.csproj ./openapi.$format --namespace $namespace.Internal --output ./GeneratedCode/Internal$output_path --internal --no-logging
                    ThrowOnNativeFailure
                    echo "dotnet run --project ../src/Refitter/Refitter.csproj ./openapi.$format --namespace $namespace.Interface --output ./GeneratedCode/I$output_path --interface-only --no-logging"
                    dotnet run --project ../src/Refitter/Refitter.csproj ./openapi.$format --namespace $namespace.Interface --output ./GeneratedCode/I$output_path --interface-only --no-logging
                    ThrowOnNativeFailure
                    echo "dotnet run --project ../src/Refitter/Refitter.csproj ./openapi.$format --namespace $namespace.UsingApiResponse --output ./GeneratedCode/IApi$output_path --use-api-response --interface-only --no-logging"
                    dotnet run --project ../src/Refitter/Refitter.csproj ./openapi.$format --namespace $namespace.UsingApiResponse --output ./GeneratedCode/IApi$output_path --use-api-response --interface-only --no-logging
                    ThrowOnNativeFailure
                fi
            done
        done
    done

                    if ($process.ExitCode -ne 0) {
                        throw "Refitter failed"
                    }

                    Write-Host "dotnet run --project ../src/Refitter/Refitter.csproj ./openapi.$format --namespace $namespace.UsingIsoDateFormat --output ./GeneratedCode/UsingIsoDateFormat$outputPath --use-iso-date-format --no-logging"
                    $process = Start-Process "dotnet" `
                        -Args "run --project ../src/Refitter/Refitter.csproj ./openapi.$format --namespace $namespace.UsingIsoDateFormat --output ./GeneratedCode/UsingIsoDateFormat$outputPath --use-iso-date-format --no-logging" `
                        -NoNewWindow `
                        -PassThru
                    $process | Wait-Process
                    if ($process.ExitCode -ne 0) {
                        throw "Refitter failed"
                    }

                    Write-Host "dotnet run --project ../src/Refitter/Refitter.csproj ./openapi.$format --namespace $namespace.MultipleInterfaces --output ./GeneratedCode/MultipleInterfaces$outputPath --multiple-interfaces --no-logging"
                    $process = Start-Process "dotnet" `
                        -Args "run --project ../src/Refitter/Refitter.csproj ./openapi.$format --namespace $namespace.MultipleInterfaces --output ./GeneratedCode/MultipleInterfaces$outputPath --multiple-interfaces byEndpoint --no-logging" `
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
                    Copy-Item "./GeneratedCode/MultipleInterfaces$outputPath" "./ConsoleApp/Net7/" -Force
                    Copy-Item "./GeneratedCode/MultipleInterfaces$outputPath" "./ConsoleApp/Net6/" -Force
                    Copy-Item "./GeneratedCode/MultipleInterfaces$outputPath" "./ConsoleApp/Net48/" -Force
                    Copy-Item "./GeneratedCode/MultipleInterfaces$outputPath" "./ConsoleApp/Net481/" -Force
                    Copy-Item "./GeneratedCode/MultipleInterfaces$outputPath" "./ConsoleApp/Net472/" -Force
                    Copy-Item "./GeneratedCode/MultipleInterfaces$outputPath" "./ConsoleApp/Net462/" -Force
                    Copy-Item "./GeneratedCode/MultipleInterfaces$outputPath" "./ConsoleApp/NetStandard20/" -Force
                    Copy-Item "./GeneratedCode/MultipleInterfaces$outputPath" "./ConsoleApp/NetStandard21/" -Force
                    Copy-Item "./GeneratedCode/MultipleInterfaces$outputPath" "./MinimalApi/" -Force
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

        Write-Host "dotnet run --project ../src/Refitter/Refitter.csproj ""$_"" --namespace $namespace --output ./GeneratedCode/$outputPath"
        $process = Start-Process "dotnet" `
            -Args "run --project ../src/Refitter/Refitter.csproj ""$_"" --namespace $namespace --output ./GeneratedCode/$outputPath" `
            -NoNewWindow `
            -PassThru
        $process | Wait-Process
        if ($process.ExitCode -ne 0) {
            throw "Refitter failed"
        }

        Write-Host "dotnet run --project ../src/Refitter/Refitter.csproj ""$_"" --namespace $namespace.Internal --output ./GeneratedCode/Internal$outputPath --internal --no-logging"
        $process = Start-Process "dotnet" `
            -Args "run --project ../src/Refitter/Refitter.csproj ""$_"" --namespace $namespace.Internal --output ./GeneratedCode/Internal$outputPath --internal --no-logging" `
            -NoNewWindow `
            -PassThru
        $process | Wait-Process
        if ($process.ExitCode -ne 0) {
            throw "Refitter failed"
        }

        Write-Host "dotnet run --project ../src/Refitter/Refitter.csproj """$_""" --namespace $namespace.Interface --output ./GeneratedCode/I$outputPath --interface-only --no-logging"
        $process = Start-Process "dotnet" `
            -Args "run --project ../src/Refitter/Refitter.csproj ""$_"" --namespace $namespace.Interface --output ./GeneratedCode/I$outputPath --interface-only --no-logging" `
            -NoNewWindow `
            -PassThru
        $process | Wait-Process
        if ($process.ExitCode -ne 0) {
            throw "Refitter failed"
        }

        Write-Host "dotnet run --project ../src/Refitter/Refitter.csproj ""$_"" --namespace $namespace.UsingApiResponse --output ./GeneratedCode/I$outputPath --use-api-response --interface-only --no-logging"
        $process = Start-Process "dotnet" `
            -Args "run --project ../src/Refitter/Refitter.csproj ""$_"" --namespace $namespace.UsingApiResponse --output ./GeneratedCode/IApi$outputPath --use-api-response --interface-only --no-logging" `
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