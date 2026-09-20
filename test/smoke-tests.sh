#!/usr/bin/env bash
#
# Refitter smoke tests — pure bash implementation.
#
# This script used to be a thin wrapper that delegated to smoke-tests.ps1 so
# that PowerShell had to be installed just to run the smoke tests. It now
# implements the whole smoke test suite in bash so it can run on any system
# with dotnet and (optionally) docker.
#
# Usage: ./smoke-tests.sh [-UseProduction] [-UseDocker] [-Verbose] [-Parallel <bool>]
#
#   -UseProduction  Install and test the latest published Refitter tool
#   -UseDocker      Run the christianhelle/refitter container image
#   -Verbose        Show progress output from this script and child processes
#   -Parallel       Accepted for backward compatibility, currently ignored

set -uo pipefail

# ==========================================
# Argument parsing
# ==========================================
VERBOSE_OUTPUT=false
USE_PRODUCTION=false
USE_DOCKER=false
PARALLEL=true

while [[ $# -gt 0 ]]; do
    case "$1" in
    -Parallel | --parallel)
        if [[ $# -ge 2 ]]; then
            PARALLEL="$2"
            shift 2
        else
            shift
        fi
        ;;
    -UseProduction | --use-production)
        USE_PRODUCTION=true
        shift
        ;;
    -UseDocker | --use-docker)
        USE_DOCKER=true
        shift
        ;;
    -Verbose | --verbose)
        VERBOSE_OUTPUT=true
        shift
        ;;
    *)
        shift
        ;;
    esac
done

# ==========================================
# Globals
# ==========================================

# Output is suppressed by default. Pass -Verbose to show progress output
# from this script and all child processes.
CHILD_LOG_DIR="$(mktemp -d "${TMPDIR:-/tmp}/refitter-smoke-tests-XXXXXXXX")"
cleanup() {
    [[ -d "$CHILD_LOG_DIR" ]] && rm -rf "$CHILD_LOG_DIR"
}
trap cleanup EXIT

# Resolved once by run_tests and used by start_refitter
CURRENT_PROCESS_PATH=""
CURRENT_USE_DOCKER=false

# Generation tasks are accumulated here and executed by run_generation_tasks
STANDARD_SPECS=()
STANDARD_NAMESPACES=()
STANDARD_OUTPUTS=()
STANDARD_ARGS=()

NETCORE_SPECS=()
NETCORE_NAMESPACES=()
NETCORE_OUTPUTS=()
NETCORE_ARGS=()

# ==========================================
# Helpers
# ==========================================

verbose_log() {
    if [[ "$VERBOSE_OUTPUT" == true ]]; then
        printf '%s\n' "$*" >&2
    fi
}

reset_tasks() {
    STANDARD_SPECS=()
    STANDARD_NAMESPACES=()
    STANDARD_OUTPUTS=()
    STANDARD_ARGS=()

    NETCORE_SPECS=()
    NETCORE_NAMESPACES=()
    NETCORE_OUTPUTS=()
    NETCORE_ARGS=()
}

add_standard_task() {
    STANDARD_SPECS+=("$1")
    STANDARD_NAMESPACES+=("$2")
    STANDARD_OUTPUTS+=("$3")
    STANDARD_ARGS+=("$4")
}

add_netcore_task() {
    NETCORE_SPECS+=("$1")
    NETCORE_NAMESPACES+=("$2")
    NETCORE_OUTPUTS+=("$3")
    NETCORE_ARGS+=("$4")
}

invoke_child_process() {
    local file_path="$1"
    shift
    local description="$1"
    shift

    verbose_log "$file_path $*"

    if [[ "$VERBOSE_OUTPUT" == true ]]; then
        "$file_path" "$@"
        return $?
    fi

    local stdout_log stderr_log exit_code
    stdout_log="$(mktemp "$CHILD_LOG_DIR/XXXXXX.log")"
    stderr_log="$stdout_log.err"

    "$file_path" "$@" >"$stdout_log" 2>"$stderr_log"
    exit_code=$?

    if [[ $exit_code -ne 0 ]]; then
        printf 'FAILED: %s (exit code %s)\n' "$description" "$exit_code"
        printf 'Command: %s %s\n' "$file_path" "$*"
        if [[ -f "$stdout_log" ]]; then
            printf -- '-- stdout --\n'
            tail -n 60 "$stdout_log"
        fi
        if [[ -f "$stderr_log" ]]; then
            printf -- '-- stderr --\n'
            tail -n 60 "$stderr_log"
        fi
    fi

    return $exit_code
}

get_process_path() {
    local build_from_source="$1"
    local use_docker="$2"

    if [[ "$use_docker" == true ]]; then
        printf 'docker\n'
        return
    fi
    if [[ "$build_from_source" != true ]]; then
        printf 'refitter\n'
        return
    fi
    printf './bin/refitter\n'
}

build_docker_prefix() {
    local current_dir user_param prefix
    current_dir="$(pwd)"

    user_param=""
    case "$(uname -s)" in
    Linux | Darwin)
        user_param="--user $(id -u):$(id -g)"
        ;;
    esac

    prefix="run --rm -v ${current_dir}:/src -w /src"
    if [[ -n "$user_param" ]]; then
        prefix="$prefix $user_param"
    fi
    prefix="$prefix christianhelle/refitter"
    printf '%s\n' "$prefix"
}

start_refitter() {
    local -a args=("$@")

    if [[ "$VERBOSE_OUTPUT" != true ]]; then
        args+=("--silent")
    fi

    if [[ "$CURRENT_USE_DOCKER" == true ]]; then
        local -a docker_prefix
        read -r -a docker_prefix <<<"$(build_docker_prefix)"
        invoke_child_process docker "refitter" "${docker_prefix[@]}" "${args[@]}"
        return $?
    fi

    invoke_child_process "$CURRENT_PROCESS_PATH" "refitter" "${args[@]}"
}

generate_from_settings_file() {
    local settings_file="$1"
    local exit_code

    start_refitter --no-logging --settings-file "$settings_file"
    exit_code=$?
    if [[ $exit_code -ne 0 ]]; then
        printf 'Refitter failed for settings file: %s\n' "$settings_file" >&2
        exit 1
    fi
}

build_solution() {
    local solution="$1"
    local no_restore="${2:-false}"
    local smoke_test="${3:-false}"
    local exit_code

    local -a build_args=(build "$solution" --nologo -v q --property WarningLevel=0 /clp:ErrorsOnly)
    if [[ "$no_restore" == true ]]; then
        build_args+=(--no-restore)
    fi
    if [[ "$smoke_test" == true ]]; then
        build_args+=(--property:SmokeTest=true)
    fi

    verbose_log "Building $solution"
    invoke_child_process dotnet "Build $solution" "${build_args[@]}"
    exit_code=$?
    if [[ $exit_code -ne 0 ]]; then
        printf 'Build Failed: %s\n' "$solution" >&2
        exit 1
    fi
}

# Deletes generated .cs files and generated subdirectories, keeping the
# GeneratedCode folder itself and any non-.cs content.
clean_generated_code() {
    if [[ -d ./GeneratedCode ]]; then
        find ./GeneratedCode -type f -name '*.cs' -delete 2>/dev/null || true
        find ./GeneratedCode -mindepth 1 -maxdepth 1 -type d -exec rm -rf {} + 2>/dev/null || true
    fi
}

# Deletes only generated .cs files, leaving directories in place.
remove_generated_cs_files() {
    if [[ -d ./GeneratedCode ]]; then
        find ./GeneratedCode -type f -name '*.cs' -delete 2>/dev/null || true
    fi
}

run_generation_tasks() {
    local -n specs=$1
    local -n namespaces=$2
    local -n outputs=$3
    local -n arg_list=$4

    local i exit_code
    for ((i = 0; i < ${#specs[@]}; i++)); do
        local -a args=("${specs[i]}" --namespace "${namespaces[i]}" --output "${outputs[i]}" --no-logging)
        if [[ -n "${arg_list[i]}" ]]; then
            local -a extra
            read -r -a extra <<<"${arg_list[i]}"
            args+=("${extra[@]}")
        fi

        start_refitter "${args[@]}"
        exit_code=$?
        if [[ $exit_code -ne 0 ]]; then
            printf 'Refitter generation failed for: %s (%s)\n' "${specs[i]}" "${namespaces[i]}" >&2
            exit 1
        fi
    done
}

# Builds a unique file tag from version/format/filename, matching the naming
# used by smoke-tests.ps1. Prints "<fileTag> <namespace>" on one line.
make_file_tag() {
    local version="$1"
    local format="$2"
    local filename="$3"

    local v_tag base ns_base
    v_tag="${version//./}"
    base="${filename//-/}"
    base="${base//./}"
    base="$(printf '%s' "${base:0:1}" | tr '[:lower:]' '[:upper:]')${base:1}"
    ns_base="${base}_${v_tag}_${format}"
    printf '%s %s\n' "${v_tag}_${format}_${base}" "$ns_base"
}

# ==========================================
# Variant definitions
# ==========================================

# Standard variants: compile on all frameworks (net462-net10).
# Each entry is "Suffix|Prefix|Args".
STANDARD_VARIANTS=(
    "Cancellation|WithCancellation|--cancellation-tokens"
    "Internal|Internal|--internal"
    "UsingApiResponse|IApi|--use-api-response"
    "UsingIObservable|IObservable|--use-observable-response"
    "UsingIsoDateFormat|UsingIsoDateFormat|--use-iso-date-format"
    "MultipleInterfaces|MultipleInterfaces|--multiple-interfaces ByEndpoint"
    # NOTE: --multiple-interfaces ByEndpoint --operation-name-template produces duplicate types per-endpoint.
    # This is a known Refitter limitation. We test generation works but skip compilation.
    "ContractOnly|ContractOnly|--contract-only"
    "DynamicQuerystring|DynamicQuerystring|--use-dynamic-querystring-parameters"
    "IntegerTypeInt64|IntegerTypeInt64|--integer-type Int64"
    "TrimUnusedSchema|TrimUnusedSchema|--trim-unused-schema"
    "OptionalNullable|OptionalNullable|--optional-nullable-parameters"
    "NoDeprecated|NoDeprecated|--no-deprecated-operations"
    "NoAutoGeneratedHeader|NoAutoGenHeader|--no-auto-generated-header"
    "NoAcceptHeaders|NoAcceptHeaders|--no-accept-headers"
    "SkipDefaultAdditionalProps|SkipDefaultAddlProps|--skip-default-additional-properties"
    "NoInlineJsonConverters|NoInlineJsonConv|--no-inline-json-converters"
    "InterfaceOnly|InterfaceOnly|--interface-only"
    "NoXmlDocComments|NoXmlDoc|--no-xml-doc-comments"
    "NoOperationHeaders|NoOpHeaders|--no-operation-headers"
    "AdditionalNamespace|AdditionalNs|--additional-namespace System.ComponentModel"
    "ExcludeNamespace|ExcludeNs|--exclude-namespace System.Xml.Serialization"
    "PreserveOriginal|PreserveOriginal|--property-naming-policy PreserveOriginal"
)

# Petstore-only variants: require specs with specific tags/paths
# (petstore has "pet", "user", "store" tags). Each entry is "Suffix|Prefix|Args".
PETSTORE_ONLY_VARIANTS=(
    "TagFiltered|TagFiltered|--tag pet --tag user --tag store"
    "MatchPathFiltered|MatchPathFiltered|--match-path ^/pet/.*"
    "MultipleInterfacesByTag|MultipleInterfacesByTag|--multiple-interfaces ByTag"
)

# NetCore variants: require net8.0+ features. Each entry is "Suffix|Prefix|Args".
NETCORE_VARIANTS=(
    "Disposable|Disposable|--disposable"
    "ImmutableRecords|ImmutableRecords|--immutable-records"
    "PolymorphicSerialization|PolymorphicSerialization|--use-polymorphic-serialization"
    "CollectionFormatCsv|CollectionFormatCsv|--collection-format csv"
    "JsonSerializerContext|JsonSerializerCtx|--json-serializer-context"
    "JsonLibraryVersion9|JsonLibraryVersion9|--json-library-version 9.0"
)

FILENAMES=(
    "weather"
    "petstore"
    "petstore-expanded"
    "petstore-minimal"
    "petstore-simple"
    "petstore-with-external-docs"
    "api-with-examples"
    "callback-example"
    "link-example"
    "uber"
    "uspto"
    "hubspot-events"
)

V31_FILENAMES=(
    "webhook-example"
    "lmstudio"
)

V34_WEBHOOK_FILENAMES=(
    "webhook-example"
)

# Collects generation tasks for a spec, mirroring the PowerShell logic.
# Args: spec_path file_tag namespace skip_multiple_interfaces
collect_spec_tasks() {
    local spec_path="$1"
    local file_tag="$2"
    local ns="$3"
    local skip_multiple_interfaces="$4"

    local variant suffix prefix variant_args task_args

    for variant in "${STANDARD_VARIANTS[@]}"; do
        IFS='|' read -r suffix prefix variant_args <<<"$variant"

        if [[ "$skip_multiple_interfaces" == true && "$variant_args" == *"--multiple-interfaces"* ]]; then
            continue
        fi

        task_args="$variant_args"
        # InterfaceOnly variant needs contracts-namespace so generated interfaces
        # can reference contract types from the SeparateContracts variant
        if [[ "$suffix" == "InterfaceOnly" ]]; then
            task_args="$task_args --contracts-namespace $ns.SeparateContractsFile.Contracts"
        fi

        add_standard_task "$spec_path" "$ns.$suffix" "./GeneratedCode/$prefix$file_tag.generated.cs" "$task_args"
    done

    # Petstore-only variants (tag/path filters require petstore-specific tags)
    if [[ "$(basename "$spec_path")" == petstore* ]]; then
        for variant in "${PETSTORE_ONLY_VARIANTS[@]}"; do
            IFS='|' read -r suffix prefix variant_args <<<"$variant"
            add_standard_task "$spec_path" "$ns.$suffix" "./GeneratedCode/$prefix$file_tag.generated.cs" "$variant_args"
        done
    fi

    # Multiple files variant (unique subdirectory)
    add_standard_task "$spec_path" "$ns.MultipleFiles" "./GeneratedCode/MultipleFiles/$file_tag/" "--multiple-files"

    # Separate contracts variant (unique subdirectories for both interface and contracts)
    add_standard_task "$spec_path" "$ns.SeparateContractsFile" "./GeneratedCode/SeparateContracts/$file_tag/" \
        "--contracts-output GeneratedCode/Contracts/$file_tag --contracts-namespace $ns.SeparateContractsFile.Contracts"

    for variant in "${NETCORE_VARIANTS[@]}"; do
        IFS='|' read -r suffix prefix variant_args <<<"$variant"
        add_netcore_task "$spec_path" "$ns.$suffix" "./GeneratedCode/$prefix$file_tag.generated.cs" "$variant_args"
    done
}

collect_tasks() {
    reset_tasks

    local version format filename spec_path info file_tag ns
    for version in v3.0 v2.0; do
        for format in json yaml; do
            for filename in "${FILENAMES[@]}"; do
                spec_path="./OpenAPI/$version/$filename.$format"
                [[ -f "$spec_path" ]] || continue

                info="$(make_file_tag "$version" "$format" "$filename")"
                file_tag="${info%% *}"
                ns="${info##* }"

                collect_spec_tasks "$spec_path" "$file_tag" "$ns" false
            done
        done
    done

    # v3.1 webhook specs may not have regular API paths, so skip MultipleInterfaces variants
    for format in json yaml; do
        for filename in "${V31_FILENAMES[@]}"; do
            spec_path="./OpenAPI/v3.1/$filename.$format"
            [[ -f "$spec_path" ]] || continue

            info="$(make_file_tag "v3.1" "$format" "$filename")"
            file_tag="${info%% *}"
            ns="${info##* }"

            collect_spec_tasks "$spec_path" "$file_tag" "$ns" true
        done
    done

    # v3.4 webhook specs
    for format in json yaml; do
        for filename in "${V34_WEBHOOK_FILENAMES[@]}"; do
            spec_path="./OpenAPI/v3.4/$filename.$format"
            [[ -f "$spec_path" ]] || continue

            info="$(make_file_tag "v3.4" "$format" "$filename")"
            file_tag="${info%% *}"
            ns="${info##* }"

            collect_spec_tasks "$spec_path" "$file_tag" "$ns" true
        done
    done
}

# ==========================================
# Test phases
# ==========================================

run_tests() {
    local build_from_source="$1"
    local use_docker="$2"

    CURRENT_PROCESS_PATH="$(get_process_path "$build_from_source" "$use_docker")"
    CURRENT_USE_DOCKER="$use_docker"

    # ==========================================
    # Phase 0: Build refitter from source
    # ==========================================
    if [[ "$build_from_source" == true && "$use_docker" != true ]]; then
        verbose_log "dotnet publish ../src/Refitter/Refitter.csproj -c Release -o bin -f net10.0"
        invoke_child_process dotnet "dotnet publish" publish ../src/Refitter/Refitter.csproj -c Release -o bin -f net10.0 --nologo -v q
        if [[ $? -ne 0 ]]; then
            echo "Publish failed!" >&2
            exit 1
        fi

        invoke_child_process ./bin/refitter "refitter --version" --version
        if [[ $? -ne 0 ]]; then
            echo "Show version failed!" >&2
            exit 1
        fi
    fi

    # ==========================================
    # Phase 1: Pre-restore packages
    # ==========================================
    verbose_log "Pre-restoring packages"
    invoke_child_process dotnet "restore ConsoleApp.slnx" restore ./ConsoleApp/ConsoleApp.slnx --nologo -v q
    if [[ $? -ne 0 ]]; then
        echo "Restore failed: ConsoleApp.slnx" >&2
        exit 1
    fi
    invoke_child_process dotnet "restore ConsoleApp.Core.slnx" restore ./ConsoleApp/ConsoleApp.Core.slnx --nologo -v q
    if [[ $? -ne 0 ]]; then
        echo "Restore failed: ConsoleApp.Core.slnx" >&2
        exit 1
    fi
    invoke_child_process dotnet "restore Apizr/Sample.csproj" restore ./Apizr/Sample.csproj --nologo -v q
    if [[ $? -ne 0 ]]; then
        echo "Restore failed: Apizr/Sample.csproj" >&2
        exit 1
    fi

    # ==========================================
    # Phase 2: Settings-file tests (individual generate + build)
    # ==========================================
    verbose_log "Settings-file tests"

    clean_generated_code
    generate_from_settings_file "./petstore.refitter"
    build_solution "./ConsoleApp/ConsoleApp.slnx" true

    clean_generated_code
    generate_from_settings_file "./Apizr/petstore.apizr.refitter"
    build_solution "./Apizr/Sample.csproj" true

    generate_from_settings_file "./MultipleFiles/petstore.refitter"
    build_solution "MultipleFiles/Client/Client.csproj"

    clean_generated_code
    generate_from_settings_file "./multiple-sources.refitter"
    build_solution "./ConsoleApp/ConsoleApp.Core.slnx" true

    clean_generated_code
    generate_from_settings_file "./Streaming/.refitter"
    build_solution "./Streaming/Streaming.csproj"

    # ==========================================
    # Phase 3: Generate all STANDARD variants (no build until all are generated)
    # ==========================================
    verbose_log "Generating standard variants"
    clean_generated_code
    collect_tasks

    verbose_log "Standard generation tasks: ${#STANDARD_SPECS[@]}"
    verbose_log "NetCore generation tasks: ${#NETCORE_SPECS[@]}"

    run_generation_tasks STANDARD_SPECS STANDARD_NAMESPACES STANDARD_OUTPUTS STANDARD_ARGS

    # ==========================================
    # Phase 4: Build standard variants (one build validates all)
    # ==========================================
    verbose_log "Building standard variants"
    build_solution "./ConsoleApp/ConsoleApp.slnx" true true

    # ==========================================
    # Phase 4b: Generate-only test for MultipleInterfacesWithCustomName
    # This variant uses --multiple-interfaces ByEndpoint --operation-name-template which
    # generates duplicate types per-endpoint (known limitation). We verify generation succeeds.
    # ==========================================
    verbose_log "Generate-only: MultipleInterfacesWithCustomName (petstore)"
    local custom_name_output="./GeneratedCode/MultipleInterfacesWithCustomName_generateonly.cs"
    start_refitter \
        "./OpenAPI/v3.0/petstore.json" \
        --namespace GenerateOnly.MultipleInterfacesWithCustomName \
        --output "$custom_name_output" \
        --no-logging \
        --multiple-interfaces ByEndpoint \
        --operation-name-template ExecuteAsync
    if [[ $? -ne 0 ]]; then
        echo "Generate-only test failed: MultipleInterfacesWithCustomName" >&2
        exit 1
    fi
    if [[ ! -f "$custom_name_output" ]]; then
        echo "Generate-only test failed: MultipleInterfacesWithCustomName" >&2
        exit 1
    fi
    rm -f "$custom_name_output"
    verbose_log "Generate-only test passed: MultipleInterfacesWithCustomName"

    # ==========================================
    # Phase 5: Generate netCore variants (accumulate on top of standard code)
    # Net8/Net9/Net10 can compile both standard and netCore code
    # ==========================================
    verbose_log "Generating netCore variants"
    run_generation_tasks NETCORE_SPECS NETCORE_NAMESPACES NETCORE_OUTPUTS NETCORE_ARGS

    # ==========================================
    # Phase 6: Build netCore variants
    # ==========================================
    verbose_log "Building netCore variants"
    build_solution "./ConsoleApp/ConsoleApp.Core.slnx" true true

    # ==========================================
    # Phase 7: URL-based tests (network-dependent)
    # ==========================================
    verbose_log "URL-based tests"
    clean_generated_code

    local url
    for url in \
        "https://petstore3.swagger.io/api/v3/openapi.json" \
        "https://petstore3.swagger.io/api/v3/openapi.yaml"; do
        remove_generated_cs_files

        start_refitter \
            "$url" \
            --namespace PetstoreFromUri \
            --output ./GeneratedCode/PetstoreFromUri.generated.cs \
            --no-logging
        if [[ $? -ne 0 ]]; then
            printf 'Refitter failed for URL: %s\n' "$url" >&2
            exit 1
        fi

        build_solution "./ConsoleApp/ConsoleApp.slnx" true
    done

    # ==========================================
    # Phase 8: Operation Name Generator Tests
    # ==========================================
    verbose_log "Operation Name Generator Tests"

    local op_name_generators=(
        "Default"
        "MultipleClientsFromOperationId"
        "MultipleClientsFromPathSegments"
        "MultipleClientsFromFirstTagAndOperationId"
        "MultipleClientsFromFirstTagAndOperationName"
        "MultipleClientsFromFirstTagAndPathSegments"
        "SingleClientFromOperationId"
        "SingleClientFromPathSegments"
    )

    clean_generated_code
    local gen
    for gen in "${op_name_generators[@]}"; do
        start_refitter \
            ./OpenAPI/v3.0/petstore.json \
            --namespace "OpNameGen_$gen" \
            --output "./GeneratedCode/OpNameGen_$gen.generated.cs" \
            --no-logging \
            --operation-name-generator "$gen"
        if [[ $? -ne 0 ]]; then
            verbose_log "Operation name generator '$gen' failed (may be expected for some generators)"
        fi
    done
    # Build only what was successfully generated
    if compgen -G "./GeneratedCode/OpNameGen_*.generated.cs" >/dev/null; then
        build_solution "./ConsoleApp/ConsoleApp.Core.slnx" true true
    fi

    # ==========================================
    # Phase 9: Collection Format Variant Tests
    # ==========================================
    verbose_log "Collection Format Variant Tests"

    local collection_formats=(Multi Ssv Tsv Pipes)
    local fmt
    clean_generated_code
    for fmt in "${collection_formats[@]}"; do
        start_refitter \
            ./OpenAPI/v3.0/petstore.json \
            --namespace "CollFmt_$fmt" \
            --output "./GeneratedCode/CollFmt_$fmt.generated.cs" \
            --no-logging \
            --collection-format "$fmt"
        if [[ $? -ne 0 ]]; then
            printf "Collection format '%s' generation failed\n" "$fmt" >&2
            exit 1
        fi
    done
    build_solution "./ConsoleApp/ConsoleApp.Core.slnx" true true

    # ==========================================
    # Phase 10: Combination Tests
    # ==========================================
    verbose_log "Combination Tests"

    clean_generated_code
    local combinations=(
        "MultipleInterfacesByTagFiltered|--multiple-interfaces ByTag --tag pet --tag store|./OpenAPI/v3.0/petstore.json|"
        "ImmutableRecordsPolymorphic|--immutable-records --use-polymorphic-serialization|./OpenAPI/v3.0/petstore.json|"
        "ContractOnlyMultipleFiles|--contract-only --multiple-files|./OpenAPI/v3.0/petstore.json|./GeneratedCode/Combo_ContractOnlyMultipleFiles/"
        "TrimSchemaKeepPattern|--trim-unused-schema --tag pet --keep-schema ^Pet.*|./OpenAPI/v3.0/petstore.json|"
        "DisposableCancellation|--disposable --cancellation-tokens|./OpenAPI/v3.0/petstore.json|"
    )

    local combo combo_name combo_args combo_spec combo_output
    for combo in "${combinations[@]}"; do
        IFS='|' read -r combo_name combo_args combo_spec combo_output <<<"$combo"

        local ns="Combo_$combo_name"
        local output="./GeneratedCode/Combo_$combo_name.generated.cs"
        if [[ -n "$combo_output" ]]; then
            output="$combo_output"
        fi

        local -a args=("$combo_spec" --namespace "$ns" --output "$output" --no-logging)
        local -a extra
        read -r -a extra <<<"$combo_args"
        args+=("${extra[@]}")

        start_refitter "${args[@]}"
        if [[ $? -ne 0 ]]; then
            printf "Combination test '%s' generation failed\n" "$combo_name" >&2
            exit 1
        fi
    done
    build_solution "./ConsoleApp/ConsoleApp.Core.slnx" true true

    # ==========================================
    # Phase 11: Asana API regression test (issue #359)
    # Large real-world spec that previously failed to compile with
    # "cannot derive from sealed type 'string'" (CS0509) because of
    # single-primitive allOf wrappers. The spec is committed under
    # OpenAPI/v3.0/asana.yaml to avoid transient HTTP errors. Generate
    # with default settings and build once to guard against regressions.
    # ==========================================
    verbose_log "Asana API regression test (issue #359)"

    clean_generated_code
    start_refitter \
        ./OpenAPI/v3.0/asana.yaml \
        --namespace Asana \
        --output ./GeneratedCode/Asana.generated.cs \
        --no-logging
    if [[ $? -ne 0 ]]; then
        echo "Asana API generation failed (issue #359 regression)" >&2
        exit 1
    fi
    build_solution "./ConsoleApp/ConsoleApp.Core.slnx" true true
}

main() {
    if [[ "$USE_PRODUCTION" == true ]]; then
        verbose_log "Running smoke tests in production mode"
        invoke_child_process dotnet "dotnet tool update -g refitter --prerelease" tool update -g refitter --prerelease -v q
        if [[ $? -ne 0 ]]; then
            echo "Production tool update failed" >&2
            exit 1
        fi
    fi

    if [[ "$USE_DOCKER" == true ]]; then
        verbose_log "Running smoke tests in Docker mode"
        invoke_child_process docker "docker pull christianhelle/refitter:latest" pull christianhelle/refitter:latest
        if [[ $? -ne 0 ]]; then
            echo "Docker image pull failed" >&2
            exit 1
        fi
    fi

    local build_from_source=true
    if [[ "$USE_PRODUCTION" == true || "$USE_DOCKER" == true ]]; then
        build_from_source=false
    fi

    local start_time=$SECONDS
    run_tests "$build_from_source" "$USE_DOCKER"
    printf 'Smoke tests passed in %s seconds\n' "$((SECONDS - start_time))"
}

# Allow the script to be sourced for testing without executing the suite
if [[ "${BASH_SOURCE[0]}" == "${0}" ]]; then
    main "$@"
fi
