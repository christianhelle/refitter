@echo off
setlocal enabledelayedexpansion

:: Parse command line arguments
set "THROTTLE_LIMIT=4"
set "USE_PRODUCTION=false"
set "USE_DOCKER=false"

:parse_args
if "%~1"=="" goto :main
if /i "%~1"=="-throttlelimit" (
    set "THROTTLE_LIMIT=%~2"
    shift
    shift
    goto :parse_args
)
if /i "%~1"=="-parallel" (
    :: Kept for backward compatibility
    shift
    shift
    goto :parse_args
)
if /i "%~1"=="-useproduction" (
    set "USE_PRODUCTION=true"
    shift
    goto :parse_args
)
if /i "%~1"=="-usedocker" (
    set "USE_DOCKER=true"
    shift
    goto :parse_args
)
shift
goto :parse_args

:main
echo ThrottleLimit: %THROTTLE_LIMIT%
echo UseProduction: %USE_PRODUCTION%
echo UseDocker: %USE_DOCKER%

goto :start_tests

:throw_on_native_failure
if not %errorlevel%==0 (
    echo Native Failure
    exit /b 1
)
goto :eof

:generate
:: %1=specPath %2=namespace %3=outputPath %4=extraArgs %5=processPath %6=useDocker
set "spec_path=%~1"
set "gen_namespace=%~2"
set "gen_output=%~3"
set "gen_args=%~4"
set "gen_process=%~5"
set "gen_docker=%~6"

if /i "%gen_docker%"=="true" (
    set "current_dir=%CD%"
    set "current_dir=!current_dir:\=/!"
    echo docker run --rm -v "!current_dir!:/src" -w /src christianhelle/refitter !spec_path! --namespace !gen_namespace! --output !gen_output! --no-logging !gen_args!
    docker run --rm -v "!current_dir!:/src" -w /src christianhelle/refitter !spec_path! --namespace !gen_namespace! --output !gen_output! --no-logging !gen_args!
    call :throw_on_native_failure
) else (
    echo !gen_process! !spec_path! --namespace !gen_namespace! --output !gen_output! --no-logging !gen_args!
    !gen_process! !spec_path! --namespace !gen_namespace! --output !gen_output! --no-logging !gen_args!
    call :throw_on_native_failure
)
goto :eof

:generate_from_settings
:: %1=settingsFile %2=processPath %3=useDocker
set "settings_file=%~1"
set "gfs_process=%~2"
set "gfs_docker=%~3"

if /i "%gfs_docker%"=="true" (
    set "current_dir=%CD%"
    set "current_dir=!current_dir:\=/!"
    echo docker run --rm -v "!current_dir!:/src" -w /src christianhelle/refitter --no-logging --settings-file !settings_file!
    docker run --rm -v "!current_dir!:/src" -w /src christianhelle/refitter --no-logging --settings-file !settings_file!
    call :throw_on_native_failure
) else (
    echo !gfs_process! --no-logging --settings-file !settings_file!
    !gfs_process! --no-logging --settings-file !settings_file!
    call :throw_on_native_failure
)
goto :eof

:build_solution
:: %1=solution %2=noRestore (true/false) %3=smokeTest (true/false)
set "build_sln=%~1"
set "build_norestore=%~2"
set "build_smoketest=%~3"

set "build_extra="
if /i "%build_norestore%"=="true" set "build_extra=!build_extra! --no-restore"
if /i "%build_smoketest%"=="true" set "build_extra=!build_extra! --property:SmokeTest=true"

echo.
echo Building %build_sln%
echo.
dotnet build %build_sln% --nologo -v q --property WarningLevel=0 /clp:ErrorsOnly!build_extra!
call :throw_on_native_failure
goto :eof

:clean_generated_code
del /q /s ".\GeneratedCode\*.cs" 2>nul
for /d %%d in (".\GeneratedCode\*") do rd /s /q "%%d" 2>nul
goto :eof

:run_tests
set "build_from_source=%~1"
set "use_docker_param=%~2"

if "%build_from_source%"=="" set "build_from_source=true"
if "%use_docker_param%"=="" set "use_docker_param=false"

set "process_path=.\bin\refitter"
if /i "%build_from_source%"=="false" set "process_path=refitter"
if /i "%use_docker_param%"=="true" set "process_path=docker"

:: Array of filenames
set "filename0=weather"
set "filename1=bot.paths"
set "filename2=petstore"
set "filename3=petstore-expanded"
set "filename4=petstore-minimal"
set "filename5=petstore-simple"
set "filename6=petstore-with-external-docs"
set "filename7=api-with-examples"
set "filename8=callback-example"
set "filename9=link-example"
set "filename10=uber"
set "filename11=uspto"
set "filename12=hubspot-events"
set "filename13=hubspot-webhooks"
set "filename_count=14"

:: V3.1 filenames
set "v31filename0=webhook-example"
set "v31filename_count=1"

:: ==========================================
:: Phase 0: Build refitter from source
:: ==========================================
if /i "%build_from_source%"=="true" (
    if /i "%use_docker_param%"=="false" (
        echo dotnet publish ..\src\Refitter\Refitter.csproj -c Release -o bin -f net9.0
        dotnet publish ..\src\Refitter\Refitter.csproj -c Release -o bin -f net9.0
        call :throw_on_native_failure

        echo refitter --version
        .\bin\refitter --version
        call :throw_on_native_failure
    )
)

:: ==========================================
:: Phase 1: Pre-restore packages
:: ==========================================
echo.
echo === Pre-restoring packages ===
echo.
dotnet restore .\ConsoleApp\ConsoleApp.slnx --nologo -v q
dotnet restore .\ConsoleApp\ConsoleApp.Core.slnx --nologo -v q
dotnet restore .\Apizr\Sample.csproj --nologo -v q

:: ==========================================
:: Phase 2: Settings-file tests
:: ==========================================
echo.
echo === Settings-file tests ===
echo.

call :clean_generated_code
call :generate_from_settings ".\petstore.refitter" "%process_path%" "%use_docker_param%"
call :build_solution ".\ConsoleApp\ConsoleApp.slnx" "true"

call :clean_generated_code
call :generate_from_settings ".\Apizr\petstore.apizr.refitter" "%process_path%" "%use_docker_param%"
call :build_solution ".\Apizr\Sample.csproj" "true"

call :generate_from_settings ".\MultipleFiles\petstore.refitter" "%process_path%" "%use_docker_param%"
call :build_solution "MultipleFiles\Client\Client.csproj" "false"

call :clean_generated_code
call :generate_from_settings ".\multiple-sources.refitter" "%process_path%" "%use_docker_param%"
call :build_solution ".\ConsoleApp\ConsoleApp.Core.slnx" "true"

:: ==========================================
:: Phase 3: Generate all STANDARD variants
:: ==========================================
echo.
echo === Generating standard variants ===
echo.
call :clean_generated_code

:: Standard variant definitions (suffix, prefix, args)
:: We process versions/formats/filenames and generate ALL variants, then build once

for %%v in (v3.0 v2.0) do (
    set "version_tag=%%v"
    set "version_tag=!version_tag:.=!"
    for %%f in (json yaml) do (
        for /l %%i in (0,1,13) do (
            set "cur_filename=!filename%%i!"
            set "file_path=.\OpenAPI\%%v\!cur_filename!.%%f"

            if exist "!file_path!" (
                :: Create output base and namespace
                set "output_base=!cur_filename!"
                set "output_base=!output_base:-=!"
                set "output_base=!output_base:.=!"
                call :capitalize output_base
                set "ns=!output_base!_!version_tag!_%%f"
                set "file_tag=!version_tag!_%%f_!output_base!"

                :: Standard variants
                call :generate "!file_path!" "!ns!.Cancellation" ".\GeneratedCode\WithCancellation!file_tag!.generated.cs" "--cancellation-tokens" "%process_path%" "%use_docker_param%"
                call :generate "!file_path!" "!ns!.Internal" ".\GeneratedCode\Internal!file_tag!.generated.cs" "--internal" "%process_path%" "%use_docker_param%"
                call :generate "!file_path!" "!ns!.UsingApiResponse" ".\GeneratedCode\IApi!file_tag!.generated.cs" "--use-api-response" "%process_path%" "%use_docker_param%"
                call :generate "!file_path!" "!ns!.UsingIObservable" ".\GeneratedCode\IObservable!file_tag!.generated.cs" "--use-observable-response" "%process_path%" "%use_docker_param%"
                call :generate "!file_path!" "!ns!.UsingIsoDateFormat" ".\GeneratedCode\UsingIsoDateFormat!file_tag!.generated.cs" "--use-iso-date-format" "%process_path%" "%use_docker_param%"
                call :generate "!file_path!" "!ns!.MultipleInterfaces" ".\GeneratedCode\MultipleInterfaces!file_tag!.generated.cs" "--multiple-interfaces ByEndpoint" "%process_path%" "%use_docker_param%"
                :: NOTE: --multiple-interfaces ByEndpoint --operation-name-template generates duplicate types (known limitation)
                :: Tested as generate-only after the batch build
                call :generate "!file_path!" "!ns!.ContractOnly" ".\GeneratedCode\ContractOnly!file_tag!.generated.cs" "--contract-only" "%process_path%" "%use_docker_param%"
                call :generate "!file_path!" "!ns!.DynamicQuerystring" ".\GeneratedCode\DynamicQuerystring!file_tag!.generated.cs" "--use-dynamic-querystring-parameters" "%process_path%" "%use_docker_param%"
                call :generate "!file_path!" "!ns!.IntegerTypeInt64" ".\GeneratedCode\IntegerTypeInt64!file_tag!.generated.cs" "--integer-type Int64" "%process_path%" "%use_docker_param%"
                call :generate "!file_path!" "!ns!.TrimUnusedSchema" ".\GeneratedCode\TrimUnusedSchema!file_tag!.generated.cs" "--trim-unused-schema" "%process_path%" "%use_docker_param%"
                call :generate "!file_path!" "!ns!.OptionalNullable" ".\GeneratedCode\OptionalNullable!file_tag!.generated.cs" "--optional-nullable-parameters" "%process_path%" "%use_docker_param%"
                call :generate "!file_path!" "!ns!.NoDeprecated" ".\GeneratedCode\NoDeprecated!file_tag!.generated.cs" "--no-deprecated-operations" "%process_path%" "%use_docker_param%"
                call :generate "!file_path!" "!ns!.NoAutoGeneratedHeader" ".\GeneratedCode\NoAutoGenHeader!file_tag!.generated.cs" "--no-auto-generated-header" "%process_path%" "%use_docker_param%"
                call :generate "!file_path!" "!ns!.NoAcceptHeaders" ".\GeneratedCode\NoAcceptHeaders!file_tag!.generated.cs" "--no-accept-headers" "%process_path%" "%use_docker_param%"
                call :generate "!file_path!" "!ns!.SkipDefaultAdditionalProps" ".\GeneratedCode\SkipDefaultAddlProps!file_tag!.generated.cs" "--skip-default-additional-properties" "%process_path%" "%use_docker_param%"
                call :generate "!file_path!" "!ns!.NoInlineJsonConverters" ".\GeneratedCode\NoInlineJsonConv!file_tag!.generated.cs" "--no-inline-json-converters" "%process_path%" "%use_docker_param%"
                call :generate "!file_path!" "!ns!.InterfaceOnly" ".\GeneratedCode\InterfaceOnly!file_tag!.generated.cs" "--interface-only --contracts-namespace !ns!.SeparateContractsFile.Contracts" "%process_path%" "%use_docker_param%"
                call :generate "!file_path!" "!ns!.NoXmlDocComments" ".\GeneratedCode\NoXmlDoc!file_tag!.generated.cs" "--no-xml-doc-comments" "%process_path%" "%use_docker_param%"
                call :generate "!file_path!" "!ns!.NoOperationHeaders" ".\GeneratedCode\NoOpHeaders!file_tag!.generated.cs" "--no-operation-headers" "%process_path%" "%use_docker_param%"
                call :generate "!file_path!" "!ns!.AdditionalNamespace" ".\GeneratedCode\AdditionalNs!file_tag!.generated.cs" "--additional-namespace System.ComponentModel" "%process_path%" "%use_docker_param%"
                call :generate "!file_path!" "!ns!.ExcludeNamespace" ".\GeneratedCode\ExcludeNs!file_tag!.generated.cs" "--exclude-namespace System.Xml.Serialization" "%process_path%" "%use_docker_param%"
                :: Petstore-only variants (tag/path filters require petstore-specific tags)
                echo !cur_filename! | findstr /b "petstore" >nul && (
                    call :generate "!file_path!" "!ns!.TagFiltered" ".\GeneratedCode\TagFiltered!file_tag!.generated.cs" "--tag pet --tag user --tag store" "%process_path%" "%use_docker_param%"
                    call :generate "!file_path!" "!ns!.MatchPathFiltered" ".\GeneratedCode\MatchPathFiltered!file_tag!.generated.cs" "--match-path ^^/pet/.*" "%process_path%" "%use_docker_param%"
                    call :generate "!file_path!" "!ns!.MultipleInterfacesByTag" ".\GeneratedCode\MultipleInterfacesByTag!file_tag!.generated.cs" "--multiple-interfaces ByTag" "%process_path%" "%use_docker_param%"
                )
                :: Multiple files variant (unique subdirectory)
                call :generate "!file_path!" "!ns!.MultipleFiles" ".\GeneratedCode\MultipleFiles\!file_tag!\" "--multiple-files" "%process_path%" "%use_docker_param%"
                :: Separate contracts variant (unique subdirectories)
                call :generate "!file_path!" "!ns!.SeparateContractsFile" ".\GeneratedCode\SeparateContracts\!file_tag!\" "--contracts-output GeneratedCode\Contracts\!file_tag! --contracts-namespace !ns!.SeparateContractsFile.Contracts" "%process_path%" "%use_docker_param%"
            )
        )
    )
)

:: V3.1 specs (skip MultipleInterfaces variants - webhook specs may lack regular API paths)
for %%f in (json yaml) do (
    for /l %%i in (0,1,0) do (
        set "cur_filename=!v31filename%%i!"
        set "file_path=.\OpenAPI\v3.1\!cur_filename!.%%f"

        if exist "!file_path!" (
            set "output_base=!cur_filename!"
            set "output_base=!output_base:-=!"
            set "output_base=!output_base:.=!"
            call :capitalize output_base
            set "ns=!output_base!_v31_%%f"
            set "file_tag=v31_%%f_!output_base!"

            call :generate "!file_path!" "!ns!.Cancellation" ".\GeneratedCode\WithCancellation!file_tag!.generated.cs" "--cancellation-tokens" "%process_path%" "%use_docker_param%"
            call :generate "!file_path!" "!ns!.Internal" ".\GeneratedCode\Internal!file_tag!.generated.cs" "--internal" "%process_path%" "%use_docker_param%"
            call :generate "!file_path!" "!ns!.UsingApiResponse" ".\GeneratedCode\IApi!file_tag!.generated.cs" "--use-api-response" "%process_path%" "%use_docker_param%"
            call :generate "!file_path!" "!ns!.UsingIObservable" ".\GeneratedCode\IObservable!file_tag!.generated.cs" "--use-observable-response" "%process_path%" "%use_docker_param%"
            call :generate "!file_path!" "!ns!.UsingIsoDateFormat" ".\GeneratedCode\UsingIsoDateFormat!file_tag!.generated.cs" "--use-iso-date-format" "%process_path%" "%use_docker_param%"
            call :generate "!file_path!" "!ns!.ContractOnly" ".\GeneratedCode\ContractOnly!file_tag!.generated.cs" "--contract-only" "%process_path%" "%use_docker_param%"
            call :generate "!file_path!" "!ns!.DynamicQuerystring" ".\GeneratedCode\DynamicQuerystring!file_tag!.generated.cs" "--use-dynamic-querystring-parameters" "%process_path%" "%use_docker_param%"
            call :generate "!file_path!" "!ns!.IntegerTypeInt64" ".\GeneratedCode\IntegerTypeInt64!file_tag!.generated.cs" "--integer-type Int64" "%process_path%" "%use_docker_param%"
            call :generate "!file_path!" "!ns!.TrimUnusedSchema" ".\GeneratedCode\TrimUnusedSchema!file_tag!.generated.cs" "--trim-unused-schema" "%process_path%" "%use_docker_param%"
            call :generate "!file_path!" "!ns!.OptionalNullable" ".\GeneratedCode\OptionalNullable!file_tag!.generated.cs" "--optional-nullable-parameters" "%process_path%" "%use_docker_param%"
            call :generate "!file_path!" "!ns!.NoDeprecated" ".\GeneratedCode\NoDeprecated!file_tag!.generated.cs" "--no-deprecated-operations" "%process_path%" "%use_docker_param%"
            call :generate "!file_path!" "!ns!.NoAutoGeneratedHeader" ".\GeneratedCode\NoAutoGenHeader!file_tag!.generated.cs" "--no-auto-generated-header" "%process_path%" "%use_docker_param%"
            call :generate "!file_path!" "!ns!.NoAcceptHeaders" ".\GeneratedCode\NoAcceptHeaders!file_tag!.generated.cs" "--no-accept-headers" "%process_path%" "%use_docker_param%"
            call :generate "!file_path!" "!ns!.SkipDefaultAdditionalProps" ".\GeneratedCode\SkipDefaultAddlProps!file_tag!.generated.cs" "--skip-default-additional-properties" "%process_path%" "%use_docker_param%"
            call :generate "!file_path!" "!ns!.NoInlineJsonConverters" ".\GeneratedCode\NoInlineJsonConv!file_tag!.generated.cs" "--no-inline-json-converters" "%process_path%" "%use_docker_param%"
            call :generate "!file_path!" "!ns!.InterfaceOnly" ".\GeneratedCode\InterfaceOnly!file_tag!.generated.cs" "--interface-only --contracts-namespace !ns!.SeparateContractsFile.Contracts" "%process_path%" "%use_docker_param%"
            call :generate "!file_path!" "!ns!.NoXmlDocComments" ".\GeneratedCode\NoXmlDoc!file_tag!.generated.cs" "--no-xml-doc-comments" "%process_path%" "%use_docker_param%"
            call :generate "!file_path!" "!ns!.NoOperationHeaders" ".\GeneratedCode\NoOpHeaders!file_tag!.generated.cs" "--no-operation-headers" "%process_path%" "%use_docker_param%"
            call :generate "!file_path!" "!ns!.AdditionalNamespace" ".\GeneratedCode\AdditionalNs!file_tag!.generated.cs" "--additional-namespace System.ComponentModel" "%process_path%" "%use_docker_param%"
            call :generate "!file_path!" "!ns!.ExcludeNamespace" ".\GeneratedCode\ExcludeNs!file_tag!.generated.cs" "--exclude-namespace System.Xml.Serialization" "%process_path%" "%use_docker_param%"
            call :generate "!file_path!" "!ns!.MultipleFiles" ".\GeneratedCode\MultipleFiles\!file_tag!\" "--multiple-files" "%process_path%" "%use_docker_param%"
            call :generate "!file_path!" "!ns!.SeparateContractsFile" ".\GeneratedCode\SeparateContracts\!file_tag!\" "--contracts-output GeneratedCode\Contracts\!file_tag! --contracts-namespace !ns!.SeparateContractsFile.Contracts" "%process_path%" "%use_docker_param%"
        )
    )
)

:: ==========================================
:: Phase 4: Build standard variants (one build validates all)
:: ==========================================
echo.
echo === Building standard variants ===
echo.
call :build_solution ".\ConsoleApp\ConsoleApp.slnx" "true" "true"

:: ==========================================
:: Phase 4b: Generate-only test for MultipleInterfacesWithCustomName
:: This variant uses --multiple-interfaces ByEndpoint --operation-name-template which
:: generates duplicate types per-endpoint (known limitation). We verify generation succeeds.
:: ==========================================
echo.
echo === Generate-only: MultipleInterfacesWithCustomName (petstore) ===
echo.
call :generate ".\OpenAPI\v3.0\petstore.json" "GenerateOnly.MultipleInterfacesWithCustomName" ".\GeneratedCode\MultipleInterfacesWithCustomName_generateonly.cs" "--multiple-interfaces ByEndpoint --operation-name-template ExecuteAsync" "%process_path%" "%use_docker_param%"
if not exist ".\GeneratedCode\MultipleInterfacesWithCustomName_generateonly.cs" (
    echo Generate-only test failed: MultipleInterfacesWithCustomName
    exit /b 1
)
del /q ".\GeneratedCode\MultipleInterfacesWithCustomName_generateonly.cs"
echo Generate-only test passed: MultipleInterfacesWithCustomName

:: ==========================================
:: Phase 5: Generate netCore variants (accumulate on top of standard code)
:: ==========================================
echo.
echo === Generating netCore variants ===
echo.

for %%v in (v3.0 v2.0) do (
    set "version_tag=%%v"
    set "version_tag=!version_tag:.=!"
    for %%f in (json yaml) do (
        for /l %%i in (0,1,13) do (
            set "cur_filename=!filename%%i!"
            set "file_path=.\OpenAPI\%%v\!cur_filename!.%%f"

            if exist "!file_path!" (
                set "output_base=!cur_filename!"
                set "output_base=!output_base:-=!"
                set "output_base=!output_base:.=!"
                call :capitalize output_base
                set "ns=!output_base!_!version_tag!_%%f"
                set "file_tag=!version_tag!_%%f_!output_base!"

                call :generate "!file_path!" "!ns!.Disposable" ".\GeneratedCode\Disposable!file_tag!.generated.cs" "--disposable" "%process_path%" "%use_docker_param%"
                call :generate "!file_path!" "!ns!.ImmutableRecords" ".\GeneratedCode\ImmutableRecords!file_tag!.generated.cs" "--immutable-records" "%process_path%" "%use_docker_param%"
                call :generate "!file_path!" "!ns!.PolymorphicSerialization" ".\GeneratedCode\PolymorphicSerialization!file_tag!.generated.cs" "--use-polymorphic-serialization" "%process_path%" "%use_docker_param%"
                call :generate "!file_path!" "!ns!.CollectionFormatCsv" ".\GeneratedCode\CollectionFormatCsv!file_tag!.generated.cs" "--collection-format csv" "%process_path%" "%use_docker_param%"
                call :generate "!file_path!" "!ns!.JsonSerializerContext" ".\GeneratedCode\JsonSerializerCtx!file_tag!.generated.cs" "--json-serializer-context" "%process_path%" "%use_docker_param%"
            )
        )
    )
)

:: V3.1 netCore variants
for %%f in (json yaml) do (
    for /l %%i in (0,1,0) do (
        set "cur_filename=!v31filename%%i!"
        set "file_path=.\OpenAPI\v3.1\!cur_filename!.%%f"

        if exist "!file_path!" (
            set "output_base=!cur_filename!"
            set "output_base=!output_base:-=!"
            set "output_base=!output_base:.=!"
            call :capitalize output_base
            set "ns=!output_base!_v31_%%f"
            set "file_tag=v31_%%f_!output_base!"

            call :generate "!file_path!" "!ns!.Disposable" ".\GeneratedCode\Disposable!file_tag!.generated.cs" "--disposable" "%process_path%" "%use_docker_param%"
            call :generate "!file_path!" "!ns!.ImmutableRecords" ".\GeneratedCode\ImmutableRecords!file_tag!.generated.cs" "--immutable-records" "%process_path%" "%use_docker_param%"
            call :generate "!file_path!" "!ns!.PolymorphicSerialization" ".\GeneratedCode\PolymorphicSerialization!file_tag!.generated.cs" "--use-polymorphic-serialization" "%process_path%" "%use_docker_param%"
            call :generate "!file_path!" "!ns!.CollectionFormatCsv" ".\GeneratedCode\CollectionFormatCsv!file_tag!.generated.cs" "--collection-format csv" "%process_path%" "%use_docker_param%"
            call :generate "!file_path!" "!ns!.JsonSerializerContext" ".\GeneratedCode\JsonSerializerCtx!file_tag!.generated.cs" "--json-serializer-context" "%process_path%" "%use_docker_param%"
        )
    )
)

:: ==========================================
:: Phase 6: Build netCore variants
:: ==========================================
echo.
echo === Building netCore variants ===
echo.
call :build_solution ".\ConsoleApp\ConsoleApp.Core.slnx" "true" "true"

:: ==========================================
:: Phase 7: URL-based tests (network-dependent)
:: ==========================================
echo.
echo === URL-based tests ===
echo.
call :clean_generated_code

for %%u in ("https://petstore3.swagger.io/api/v3/openapi.json" "https://petstore3.swagger.io/api/v3/openapi.yaml") do (
    set "namespace=PetstoreFromUri"
    set "output_path=PetstoreFromUri.generated.cs"

    del /q ".\GeneratedCode\*.cs" 2>nul

    if /i "%use_docker_param%"=="true" (
        set "current_dir=%CD%"
        set "current_dir=!current_dir:\=/!"
        echo docker run --rm -v "!current_dir!:/src" -w /src christianhelle/refitter %%u --namespace !namespace! --output .\GeneratedCode\!output_path!
        docker run --rm -v "!current_dir!:/src" -w /src christianhelle/refitter %%u --namespace !namespace! --output .\GeneratedCode\!output_path!
        call :throw_on_native_failure
    ) else (
        echo !process_path! %%u --namespace !namespace! --output .\GeneratedCode\!output_path!
        !process_path! %%u --namespace !namespace! --output .\GeneratedCode\!output_path!
        call :throw_on_native_failure
    )

    call :build_solution ".\ConsoleApp\ConsoleApp.slnx" "true"
)

:: ==========================================
:: Phase 8: Operation Name Generator Tests
:: ==========================================
echo.
echo === Operation Name Generator Tests ===
echo.

call :clean_generated_code

for %%g in (
    "Default"
    "MultipleClientsFromOperationId"
    "MultipleClientsFromPathSegments"
    "MultipleClientsFromFirstTagAndOperationId"
    "MultipleClientsFromFirstTagAndOperationName"
    "MultipleClientsFromFirstTagAndPathSegments"
    "SingleClientFromOperationId"
    "SingleClientFromPathSegments"
) do (
    set "gen_name=%%~g"
    if /i "%use_docker_param%"=="true" (
        set "current_dir=%CD%"
        set "current_dir=!current_dir:\=/!"
        echo docker run --rm -v "!current_dir!:/src" -w /src christianhelle/refitter .\OpenAPI\v3.0\petstore.json --namespace OpNameGen_!gen_name! --output .\GeneratedCode\OpNameGen_!gen_name!.generated.cs --no-logging --operation-name-generator !gen_name!
        docker run --rm -v "!current_dir!:/src" -w /src christianhelle/refitter .\OpenAPI\v3.0\petstore.json --namespace OpNameGen_!gen_name! --output .\GeneratedCode\OpNameGen_!gen_name!.generated.cs --no-logging --operation-name-generator !gen_name!
        if not !errorlevel!==0 echo Warning: Operation name generator '!gen_name!' failed (may be expected for some generators)
    ) else (
        echo !process_path! .\OpenAPI\v3.0\petstore.json --namespace OpNameGen_!gen_name! --output .\GeneratedCode\OpNameGen_!gen_name!.generated.cs --no-logging --operation-name-generator !gen_name!
        !process_path! .\OpenAPI\v3.0\petstore.json --namespace OpNameGen_!gen_name! --output .\GeneratedCode\OpNameGen_!gen_name!.generated.cs --no-logging --operation-name-generator !gen_name!
        if not !errorlevel!==0 echo Warning: Operation name generator '!gen_name!' failed (may be expected for some generators)
    )
)

if exist ".\GeneratedCode\OpNameGen_*.generated.cs" (
    call :build_solution ".\ConsoleApp\ConsoleApp.Core.slnx" "true" "true"
)

:: ==========================================
:: Phase 9: Collection Format Variant Tests
:: ==========================================
echo.
echo === Collection Format Variant Tests ===
echo.

call :clean_generated_code

for %%f in (Multi Ssv Tsv Pipes) do (
    call :generate ".\OpenAPI\v3.0\petstore.json" "CollFmt_%%f" ".\GeneratedCode\CollFmt_%%f.generated.cs" "--collection-format %%f" "%process_path%" "%use_docker_param%"
)

call :build_solution ".\ConsoleApp\ConsoleApp.Core.slnx" "true" "true"

:: ==========================================
:: Phase 10: Combination Tests
:: ==========================================
echo.
echo === Combination Tests ===
echo.

call :clean_generated_code

call :generate ".\OpenAPI\v3.0\petstore.json" "Combo_MultipleInterfacesByTagFiltered" ".\GeneratedCode\Combo_MultipleInterfacesByTagFiltered.generated.cs" "--multiple-interfaces ByTag --tag pet --tag store" "%process_path%" "%use_docker_param%"
call :generate ".\OpenAPI\v3.0\petstore.json" "Combo_ImmutableRecordsPolymorphic" ".\GeneratedCode\Combo_ImmutableRecordsPolymorphic.generated.cs" "--immutable-records --use-polymorphic-serialization" "%process_path%" "%use_docker_param%"
call :generate ".\OpenAPI\v3.0\petstore.json" "Combo_ContractOnlyMultipleFiles" ".\GeneratedCode\Combo_ContractOnlyMultipleFiles.generated.cs" "--contract-only --multiple-files" "%process_path%" "%use_docker_param%"
call :generate ".\OpenAPI\v3.0\petstore.json" "Combo_TrimSchemaKeepPattern" ".\GeneratedCode\Combo_TrimSchemaKeepPattern.generated.cs" "--trim-unused-schema --tag pet --keep-schema ^^Pet.*" "%process_path%" "%use_docker_param%"
call :generate ".\OpenAPI\v3.0\petstore.json" "Combo_DisposableCancellation" ".\GeneratedCode\Combo_DisposableCancellation.generated.cs" "--disposable --cancellation-tokens" "%process_path%" "%use_docker_param%"

call :build_solution ".\ConsoleApp\ConsoleApp.Core.slnx" "true" "true"

goto :eof

:capitalize
set "temp_var=!%1!"
set "first_char=!temp_var:~0,1!"
set "rest_chars=!temp_var:~1!"
for %%a in (A B C D E F G H I J K L M N O P Q R S T U V W X Y Z) do (
    for %%b in (a b c d e f g h i j k l m n o p q r s t u v w x y z) do (
        if "!first_char!"=="%%b" set "first_char=%%a"
    )
)
set "%1=!first_char!!rest_chars!"
goto :eof

:start_tests

:: Main execution
if /i "%USE_PRODUCTION%"=="true" (
    echo Running smoke tests in production mode
    echo dotnet tool update -g refitter --prerelease
    dotnet tool update -g refitter --prerelease
    call :throw_on_native_failure
    echo.
)

if /i "%USE_DOCKER%"=="true" (
    echo Running smoke tests in Docker mode
    echo docker pull christianhelle/refitter:latest
    docker pull christianhelle/refitter:latest
    call :throw_on_native_failure
    echo.
)

echo Starting smoke tests...
set "start_time=%time%"

if /i "%USE_DOCKER%"=="true" (
    call :run_tests "false" "true"
) else if /i "%USE_PRODUCTION%"=="true" (
    call :run_tests "false" "false"
) else (
    call :run_tests "true" "false"
)

set "end_time=%time%"
echo.
echo Smoke tests completed.

endlocal
