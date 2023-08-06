#!/bin/bash

# Function to handle errors
function throw_on_failure() {
    if [ $? -ne 0 ]; then
        echo "Native Failure"
        exit 1
    fi
}

# Function to run tests
function run_tests() {
    format_arr=("json" "yaml")
    filenames=("petstore-expanded" "petstore-minimal" "petstore-simple" "petstore-with-external-docs" "petstore" "ingram-micro" "api-with-examples" "callback-example" "link-example" "uber" "uspto" "hubspot-events" "hubspot-webhooks")

    for version in "v2.0" "v3.0"; do
        for format in "${format_arr[@]}"; do
            for filename in "${filenames[@]}"; do
                filename="./OpenAPI/${version}/${filename}.${format}"
                if [[ -f $filename ]]; then
                    cp "$filename" "./openapi.${format}"
                    output_path="${filename%.${format}}.cs"
                    output_path=$(tr '[:lower:]' '[:upper:]' <<< ${output_path:0:1})${output_path:1}
                    namespace=$(echo "${filename%.${format}}" | sed 's/-//g')
                    namespace=$(tr '[:lower:]' '[:upper:]' <<< ${namespace:0:1})${namespace:1}

                    echo "dotnet run --project ../src/Refitter/Refitter.csproj ./openapi.${format} --namespace ${namespace} --output ./GeneratedCode/${output_path} --no-logging"
                    dotnet run --project ../src/Refitter/Refitter.csproj ./openapi.${format} --namespace ${namespace} --output ./GeneratedCode/${output_path} --no-logging
                    throw_on_failure

                    echo "dotnet run --project ../src/Refitter/Refitter.csproj ./openapi.${format} --namespace ${namespace}.Cancellation --output ./GeneratedCode/WithCancellation${output_path} --cancellation-tokens --no-logging"
                    dotnet run --project ../src/Refitter/Refitter.csproj ./openapi.${format} --namespace ${namespace}.Cancellation --output ./GeneratedCode/WithCancellation${output_path} --cancellation-tokens --no-logging
                    throw_on_failure

                    echo "dotnet run --project ../src/Refitter/Refitter.csproj ./openapi.${format} --namespace ${namespace}.Internal --output ./GeneratedCode/Internal${output_path} --internal --no-logging"
                    dotnet run --project ../src/Refitter/Refitter.csproj ./openapi.${format} --namespace ${namespace}.Internal --output ./GeneratedCode/Internal${output_path} --internal --no-logging
                    throw_on_failure

                    echo "dotnet run --project ../src/Refitter/Refitter.csproj ./openapi.${format} --namespace ${namespace}.Interface --output ./GeneratedCode/I${output_path} --interface-only --no-logging"
                    dotnet run --project ../src/Refitter/Refitter.csproj ./openapi.${format} --namespace ${namespace}.Interface --output ./GeneratedCode/I${output_path} --interface-only --no-logging
                    throw_on_failure

                    echo "dotnet run --project ../src/Refitter/Refitter.csproj ./openapi.${format} --namespace ${namespace}.UsingApiResponse --output ./GeneratedCode/IApi${output_path} --use-api-response --interface-only --no-logging"
                    dotnet run --project ../src/Refitter/Refitter.csproj ./openapi.${format} --namespace ${namespace}.UsingApiResponse --output ./GeneratedCode/IApi${output_path} --use-api-response --interface-only --no-logging
                    throw_on_failure

                    echo "dotnet run --project ../src/Refitter/Refitter.csproj ./openapi.${format} --namespace ${namespace}.UsingIsoDateFormat --output ./GeneratedCode/UsingIsoDateFormat${output_path} --use-iso-date-format --no-logging"
                    dotnet run --project ../src/Refitter/Refitter.csproj ./openapi.${format} --namespace ${namespace}.UsingIsoDateFormat --output ./GeneratedCode/UsingIsoDateFormat${output_path} --use-iso-date-format --no-logging
                    throw_on_failure

                    echo "dotnet run --project ../src/Refitter/Refitter.csproj ./openapi.${format} --namespace ${namespace}.MultipleInterfaces --output ./GeneratedCode/MultipleInterfaces${output_path} --multiple-interfaces --no-logging"
                    dotnet run --project ../src/Refitter/Refitter.csproj ./openapi.${format} --namespace ${namespace}.MultipleInterfaces --output ./GeneratedCode/MultipleInterfaces${output_path} --multiple-interfaces byEndpoint --no-logging
                    throw_on_failure

                    cp "./GeneratedCode/${output_path}" "./${version}-${filename}.${format}.cs"
                    cp "./GeneratedCode/${output_path}" "./ConsoleApp/Net7/" -f
                    cp "./GeneratedCode/${output_path}" "./ConsoleApp/Net6/" -f
                    cp "./GeneratedCode/${output_path}" "./ConsoleApp/Net48/" -f
                    cp "./GeneratedCode/${output_path}" "./ConsoleApp/Net481/" -f
                    cp "./GeneratedCode/${output_path}" "./ConsoleApp/Net472/" -f
                    cp "./GeneratedCode/${output_path}" "./ConsoleApp/Net462/" -f
                    cp "./GeneratedCode/${output_path}" "./ConsoleApp/NetStandard20/" -f
                    cp "./GeneratedCode/${output_path}" "./ConsoleApp/NetStandard21/" -f
                    cp "./GeneratedCode/${output_path}" "./MinimalApi/" -f
                    cp "./GeneratedCode/I${output_path}" "./ConsoleApp/Net7/" -f
                    cp "./GeneratedCode/I${output_path}" "./ConsoleApp/Net6/" -f
                    cp "./GeneratedCode/I${output_path}" "./ConsoleApp/Net48/" -f
                    cp "./GeneratedCode/I${output_path}" "./ConsoleApp/Net481/" -f
                    cp "./GeneratedCode/I${output_path}" "./ConsoleApp/Net472/" -f
                    cp "./GeneratedCode/I${output_path}" "./ConsoleApp/Net462/" -f
                    cp "./GeneratedCode/I${output_path}" "./ConsoleApp/NetStandard20/" -f
                    cp "./GeneratedCode/I${output_path}" "./ConsoleApp/NetStandard21/" -f
                    cp "./GeneratedCode/I${output_path}" "./MinimalApi/" -f
                    cp "./GeneratedCode/IApi${output_path}" "./ConsoleApp/Net7/" -f
                    cp "./GeneratedCode/IApi${output_path}" "./ConsoleApp/Net6/" -f
                    cp "./GeneratedCode/IApi${output_path}" "./ConsoleApp/Net48/" -f
                    cp "./GeneratedCode/IApi${output_path}" "./ConsoleApp/Net481/" -f
                    cp "./GeneratedCode/IApi${output_path}" "./ConsoleApp/Net472/" -f
                    cp "./GeneratedCode/IApi${output_path}" "./ConsoleApp/Net462/" -f
                    cp "./GeneratedCode/IApi${output_path}" "./ConsoleApp/NetStandard20/" -f
                    cp "./GeneratedCode/IApi${output_path}" "./ConsoleApp/NetStandard21/" -f
                    cp "./GeneratedCode/IApi${output_path}" "./MinimalApi/" -f
                    cp "./GeneratedCode/Internal${output_path}" "./ConsoleApp/Net7/" -f
                    cp "./GeneratedCode/Internal${output_path}" "./ConsoleApp/Net6/" -f
                    cp "./GeneratedCode/Internal${output_path}" "./ConsoleApp/Net48/" -f
                    cp "./GeneratedCode/Internal${output_path}" "./ConsoleApp/Net481/" -f
                    cp "./GeneratedCode/Internal${output_path}" "./ConsoleApp/Net472/" -f
                    cp "./GeneratedCode/Internal${output_path}" "./ConsoleApp/Net462/" -f
                    cp "./GeneratedCode/Internal${output_path}" "./ConsoleApp/NetStandard20/" -f
                    cp "./GeneratedCode/Internal${output_path}" "./ConsoleApp/NetStandard21/" -f
                    cp "./GeneratedCode/Internal${output_path}" "./MinimalApi/" -f

                    echo -e "\nBuilding ConsoleApp\n"
                    dotnet build ./ConsoleApp/ConsoleApp.sln
                    throw_on_failure

                    echo -e "\nBuilding MinimalApi\n"
                    dotnet build ./MinimalApi/MinimalApi.csproj
                    throw_on_failure
                fi
            done
        done
    done

    uri_arr=("https://petstore3.swagger.io/api/v3/openapi.json" "https://petstore3.swagger.io/api/v3/openapi.yaml")

    for uri in "${uri_arr[@]}"; do
        namespace="PetstoreFromUri"
        output_path="PetstoreFromUri.cs"

        echo "dotnet run --project ../src/Refitter/Refitter.csproj \"${uri}\" --namespace ${namespace} --output ./GeneratedCode/${output_path}"
        dotnet run --project ../src/Refitter/Refitter.csproj "${uri}" --namespace ${namespace} --output ./GeneratedCode/${output_path}
        throw_on_failure

        echo "dotnet run --project ../src/Refitter/Refitter.csproj \"${uri}\" --namespace ${namespace}.Internal --output ./GeneratedCode/Internal${output_path} --internal --no-logging"
        dotnet run --project ../src/Refitter/Refitter.csproj "${uri}" --namespace ${namespace}.Internal --output ./GeneratedCode/Internal${output_path} --internal --no-logging
        throw_on_failure

        echo "dotnet run --project ../src/Refitter/Refitter.csproj \"${uri}\" --namespace ${namespace}.Interface --output ./GeneratedCode/I${output_path} --interface-only --no-logging"
        dotnet run --project ../src/Refitter/Refitter.csproj "${uri}" --namespace ${namespace}.Interface --output ./GeneratedCode/I${output_path} --interface-only --no-logging
        throw_on_failure

        echo "dotnet run --project ../src/Refitter/Refitter.csproj \"${uri}\" --namespace ${namespace}.UsingApiResponse --output ./GeneratedCode/I${output_path} --use-api-response --interface-only --no-logging"
        dotnet run --project ../src/Refitter/Refitter.csproj "${uri}" --namespace ${namespace}.UsingApiResponse --output ./GeneratedCode/IApi${output_path} --use-api-response --interface-only --no-logging
        throw_on_failure

        cp "./GeneratedCode/${output_path}" "./ConsoleApp/Net7/" -f
        cp "./GeneratedCode/${output_path}" "./ConsoleApp/Net6/" -f
        cp "./GeneratedCode/${output_path}" "./ConsoleApp/Net48/" -f
        cp "./GeneratedCode/${output_path}" "./ConsoleApp/Net481/" -f
        cp "./GeneratedCode/${output_path}" "./ConsoleApp/Net472/" -f
        cp "./GeneratedCode/${output_path}" "./ConsoleApp/Net462/" -f
        cp "./GeneratedCode/${output_path}" "./ConsoleApp/NetStandard20/" -f
        cp "./GeneratedCode/${output_path}" "./ConsoleApp/NetStandard21/" -f
        cp "./GeneratedCode/${output_path}" "./MinimalApi/" -f
        cp "./GeneratedCode/I${output_path}" "./ConsoleApp/Net7/" -f
        cp "./GeneratedCode/I${output_path}" "./ConsoleApp/Net6/" -f
        cp "./GeneratedCode/I${output_path}" "./ConsoleApp/Net48/" -f
        cp "./GeneratedCode/I${output_path}" "./ConsoleApp/Net481/" -f
        cp "./GeneratedCode/I${output_path}" "./ConsoleApp/Net472/" -f
        cp "./GeneratedCode/I${output_path}" "./ConsoleApp/Net462/" -f
        cp "./GeneratedCode/I${output_path}" "./ConsoleApp/NetStandard20/" -f
        cp "./GeneratedCode/I${output_path}" "./ConsoleApp/NetStandard21/" -f
        cp "./GeneratedCode/I${output_path}" "./MinimalApi/" -f
        cp "./GeneratedCode/IApi${output_path}" "./ConsoleApp/Net7/" -f
        cp "./GeneratedCode/IApi${output_path}" "./ConsoleApp/Net6/" -f
        cp "./GeneratedCode/IApi${output_path}" "./ConsoleApp/Net48/" -f
        cp "./GeneratedCode/IApi${output_path}" "./ConsoleApp/Net481/" -f
        cp "./GeneratedCode/IApi${output_path}" "./ConsoleApp/Net472/" -f
        cp "./GeneratedCode/IApi${output_path}" "./ConsoleApp/Net462/" -f
        cp "./GeneratedCode/IApi${output_path}" "./ConsoleApp/NetStandard20/" -f
        cp "./GeneratedCode/IApi${output_path}" "./ConsoleApp/NetStandard21/" -f
        cp "./GeneratedCode/IApi${output_path}" "./MinimalApi/" -f
        cp "./GeneratedCode/Internal${output_path}" "./ConsoleApp/Net7/" -f
        cp "./GeneratedCode/Internal${output_path}" "./ConsoleApp/Net6/" -f
        cp "./GeneratedCode/Internal${output_path}" "./ConsoleApp/Net48/" -f
        cp "./GeneratedCode/Internal${output_path}" "./ConsoleApp/Net481/" -f
        cp "./GeneratedCode/Internal${output_path}" "./ConsoleApp/Net472/" -f
        cp "./GeneratedCode/Internal${output_path}" "./ConsoleApp/Net462/" -f
        cp "./GeneratedCode/Internal${output_path}" "./ConsoleApp/NetStandard20/" -f
        cp "./GeneratedCode/Internal${output_path}" "./ConsoleApp/NetStandard21/" -f
        cp "./GeneratedCode/Internal${output_path}" "./MinimalApi/" -f

        echo -e "\nBuilding ConsoleApp\n"
        dotnet build ./ConsoleApp/ConsoleApp.sln
        throw_on

        echo -e "\nBuilding ConsoleApp\n"
        dotnet build ./ConsoleApp/ConsoleApp.sln
        throw_on_failure

        echo -e "\nBuilding MinimalApi\n"
        dotnet build ./MinimalApi/MinimalApi.csproj
        throw_on_failure
    done
}
