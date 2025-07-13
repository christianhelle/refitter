@echo off
setlocal enabledelayedexpansion

:: Parse command line arguments
set "PARALLEL=true"
set "USE_PRODUCTION=false"

:parse_args
if "%~1"=="" goto :main
if /i "%~1"=="-parallel" (
    set "PARALLEL=%~2"
    shift
    shift
    goto :parse_args
)
if /i "%~1"=="-useproduction" (
    set "USE_PRODUCTION=true"
    shift
    goto :parse_args
)
shift
goto :parse_args

:main
echo Parallel: %PARALLEL%
echo UseProduction: %USE_PRODUCTION%

goto :run_tests

:throw_on_native_failure
if not %errorlevel%==0 (
    echo Native Failure
    exit /b 1
)
goto :eof

:generate_and_build
set "format=%~1"
set "namespace=%~2"
set "output_path=%~3"
set "args=%~4"
set "net_core=%~5"
set "csproj=%~6"
set "build_from_source=%~7"

if "%build_from_source%"=="" set "build_from_source=true"

:: Clean up generated files
del /q /s ".\GeneratedCode\*.cs" 2>nul

set "process_path=.\bin\refitter"
if /i "%build_from_source%"=="false" set "process_path=refitter"

:: Check if using settings file
echo %args% | findstr "settings-file" >nul
if %errorlevel%==0 (
    echo %process_path% --no-logging %args%
    %process_path% --no-logging %args%
    call :throw_on_native_failure
) else (
    echo %process_path% .\openapi.%format% --namespace %namespace% --output .\GeneratedCode\%output_path% --no-logging %args%
    %process_path% .\openapi.%format% --namespace %namespace% --output .\GeneratedCode\%output_path% --no-logging %args%
    call :throw_on_native_failure
)

:: Build the project
if not "%csproj%"=="" (
    echo.
    echo Building %csproj% file
    echo.
    set "solution=%csproj%"
) else (
    echo.
    echo Building ConsoleApp
    echo.
    set "solution=.\ConsoleApp\ConsoleApp.sln"
    if /i "%net_core%"=="true" set "solution=.\ConsoleApp\ConsoleApp.Core.sln"
)

dotnet build %solution%
call :throw_on_native_failure
goto :eof

:run_tests
set "method=%~1"
set "parallel_param=%~2"
set "build_from_source_param=%~3"

if "%build_from_source_param%"=="" set "build_from_source_param=true"

:: Array of filenames (simulated using variables)
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

if /i "%build_from_source_param%"=="true" (
    echo dotnet publish ..\src\Refitter\Refitter.csproj -p:PublishReadyToRun=true -o bin -f net8.0
    dotnet publish ..\src\Refitter\Refitter.csproj -p:PublishReadyToRun=true -o bin -f net8.0
    call :throw_on_native_failure

    echo refitter --version
    .\bin\refitter --version
    call :throw_on_native_failure
)

:: Generate with settings files
call :generate_and_build " " " " "SwaggerPetstoreDirect.generated.cs" "--settings-file .\petstore.refitter" "" "" "%build_from_source_param%"
call :generate_and_build " " " " "" "--settings-file .\Apizr\petstore.apizr.refitter" "" ".\Apizr\Sample.csproj" "%build_from_source_param%"
call :generate_and_build " " " " "" "--settings-file .\MultipleFiles\petstore.refitter" "" "MultipleFiles\Client\Client.csproj" "%build_from_source_param%"

:: Process versions and formats
for %%v in (v3.0 v2.0) do (
    for %%f in (json yaml) do (
        for /l %%i in (0,1,13) do (
            set "filename=!filename%%i!"
            set "file_path=.\OpenAPI\%%v\!filename!.%%f"
            
            if exist "!file_path!" (
                copy "!file_path!" ".\openapi.%%f" >nul
                
                :: Create output path and namespace
                set "output_path=!filename!.generated.cs"
                set "output_path=!output_path:~0,1!!output_path:~1!"
                call :capitalize output_path
                
                set "namespace=!filename!"
                set "namespace=!namespace:-=!"
                call :capitalize namespace
                
                :: Generate different variants
                call :generate_and_build "%%f" "!namespace!.Disposable" "Disposable!output_path!" "--disposable" "true" "" "%build_from_source_param%"
                call :generate_and_build "%%f" "!namespace!.MultipleFiles" "" "--multiple-files" "" "" "%build_from_source_param%"
                call :generate_and_build "%%f" "!namespace!.SeparateContractsFile" "" "--contracts-output GeneratedCode/Contracts --contracts-namespace !namespace!.SeparateContractsFile.Contracts" "" "" "%build_from_source_param%"
                call :generate_and_build "%%f" "!namespace!.Cancellation" "WithCancellation!output_path!" "--cancellation-tokens" "" "" "%build_from_source_param%"
                call :generate_and_build "%%f" "!namespace!.Internal" "Internal!output_path!" "--internal" "" "" "%build_from_source_param%"
                call :generate_and_build "%%f" "!namespace!.UsingApiResponse" "IApi!output_path!" "--use-api-response" "" "" "%build_from_source_param%"
                call :generate_and_build "%%f" "!namespace!.UsingIObservable" "IObservable!output_path!" "--use-observable-response" "" "" "%build_from_source_param%"
                call :generate_and_build "%%f" "!namespace!.UsingIsoDateFormat" "UsingIsoDateFormat!output_path!" "--use-iso-date-format" "" "" "%build_from_source_param%"
                call :generate_and_build "%%f" "!namespace!.MultipleInterfaces" "MultipleInterfaces!output_path!" "--multiple-interfaces ByEndpoint" "" "" "%build_from_source_param%"
                call :generate_and_build "%%f" "!namespace!.MultipleInterfaces" "MultipleInterfacesWithCustomName!output_path!" "--multiple-interfaces ByEndpoint --operation-name-template ExecuteAsync" "" "" "%build_from_source_param%"
                call :generate_and_build "%%f" "!namespace!.TagFiltered" "TagFiltered!output_path!" "--tag pet --tag user --tag store" "" "" "%build_from_source_param%"
                call :generate_and_build "%%f" "!namespace!.MatchPathFiltered" "MatchPathFiltered!output_path!" "--match-path ^^/pet/.*" "" "" "%build_from_source_param%"
                call :generate_and_build "%%f" "!namespace!.ContractOnly" "ContractOnly!output_path!" "--contract-only" "" "" "%build_from_source_param%"
                call :generate_and_build "%%f" "!namespace!.ImmutableRecords" "ImmutableRecords!output_path!" "--immutable-records" "true" "" "%build_from_source_param%"
                call :generate_and_build "%%f" "!namespace!.PolymorphicSerialization" "PolymorphicSerialization!output_path!" "--use-polymorphic-serialization" "true" "" "%build_from_source_param%"
                call :generate_and_build "%%f" "!namespace!.CollectionFormatCsv" "CollectionFormatCsv!output_path!" "--collection-format csv" "true" "" "%build_from_source_param%"
            )
        )
    )
)

:: Process URLs
for %%u in ("https://petstore3.swagger.io/api/v3/openapi.json" "https://petstore3.swagger.io/api/v3/openapi.yaml") do (
    set "namespace=PetstoreFromUri"
    set "output_path=PetstoreFromUri.generated.cs"
    
    del /q "*.generated.cs" 2>nul
    
    set "process_path=.\bin\refitter"
    if /i "%build_from_source_param%"=="false" set "process_path=refitter"
    
    echo !process_path! %%u --namespace !namespace! --output .\GeneratedCode\!output_path!
    !process_path! %%u --namespace !namespace! --output .\GeneratedCode\!output_path!
    call :throw_on_native_failure
    
    echo.
    echo Building ConsoleApp
    echo.
    dotnet build .\ConsoleApp\ConsoleApp.sln
    call :throw_on_native_failure
)

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

:: Main execution
if /i "%USE_PRODUCTION%"=="true" (
    echo Running smoke tests in production mode
    echo dotnet tool update -g refitter --prerelease
    dotnet tool update -g refitter --prerelease
    call :throw_on_native_failure
    echo.
)

echo Starting smoke tests...
set "start_time=%time%"
call :run_tests "dotnet-run" "%PARALLEL%" "!USE_PRODUCTION:true=false!"
set "end_time=%time%"
echo.
echo Smoke tests completed.

endlocal
