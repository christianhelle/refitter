#!/bin/bash

function ThrowOnNativeFailure {
    if [ $? -ne 0 ]; then
        echo "Native Failure"
        exit 1
    fi
}

function GenerateAndBuild {
    format=$1
    namespace=$2
    outputPath=$3

    find . -name '*.generated.cs' -delete

    echo "dotnet run --no-build --project ../src/Refitter/Refitter.csproj ./openapi.$format --namespace $namespace --output ./GeneratedCode/$outputPath --no-logging"
    dotnet run --no-build --project ../src/Refitter/Refitter.csproj ./openapi.$format --namespace $namespace --output ./GeneratedCode/$outputPath --no-logging
    ThrowOnNativeFailure

    echo -e "\nBuilding ConsoleApp\n"
    dotnet build -p:TreatWarningsAsErrors=true ./ConsoleApp/ConsoleApp.sln
    ThrowOnNativeFailure
}

function RunTests {
    method=$1
    parallel=$2

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

    echo "dotnet build --project ../src/Refitter/Refitter.csproj"
    dotnet build -p:TreatWarningsAsErrors=true ../src/Refitter/Refitter.csproj
    ThrowOnNativeFailure

    for version in "v3.0" "v2.0"; do
        for format in "json" "yaml"; do
            for filename in "${filenames[@]}"; do
                filepath="./OpenAPI/$version/$filename.$format"
                if [ -f "$filepath" ]; then
                    cp "$filepath" ./openapi.$format
                    outputPath="$(tr '[:lower:]' '[:upper:]' <<< ${filename:0:1})${filename:1}.generated.cs"
                    namespace="$(tr '-' '' <<< ${filename})"
                    namespace="$(tr '[:lower:]' '[:upper:]' <<< ${namespace:0:1})${namespace:1}"
                    GenerateAndBuild $format $namespace $outputPath
                    GenerateAndBuild $format "${namespace}.Cancellation" "WithCancellation$outputPath"
                    GenerateAndBuild $format "${namespace}.Internal" "Internal$outputPath"
                    GenerateAndBuild $format "${namespace}.Interface" "I$outputPath"
                fi
            done
        done
    done
}

RunTests "dotnet-run" false