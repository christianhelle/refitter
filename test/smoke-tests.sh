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
TASK_SPECS=()
TASK_NAMESPACES=()
TASK_OUTPUTS=()
TASK_ARGS=()

# ==========================================
# Helpers
# ==========================================

verbose_log() {
    if [[ "$VERBOSE_OUTPUT" == true ]]; then
        printf '%s\n' "$*" >&2
    fi
}

reset_tasks() {
    TASK_SPECS=()
    TASK_NAMESPACES=()
    TASK_OUTPUTS=()
    TASK_ARGS=()
}

add_task() {
    TASK_SPECS+=("$1")
    TASK_NAMESPACES+=("$2")
    TASK_OUTPUTS+=("$3")
    TASK_ARGS+=("$4")
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
    local i exit_code
    for ((i = 0; i < ${#TASK_SPECS[@]}; i++)); do
        local -a args=("${TASK_SPECS[i]}" --namespace "${TASK_NAMESPACES[i]}" --output "${TASK_OUTPUTS[i]}" --no-logging)
        if [[ -n "${TASK_ARGS[i]}" ]]; then
            local -a extra
            read -r -a extra <<<"${TASK_ARGS[i]}"
            args+=("${extra[@]}")
        fi

        start_refitter "${args[@]}"
        exit_code=$?
        if [[ $exit_code -ne 0 ]]; then
            printf 'Refitter generation failed for: %s (%s)\n' "${TASK_SPECS[i]}" "${TASK_NAMESPACES[i]}" >&2
            exit 1
        fi
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
