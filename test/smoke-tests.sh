#!/bin/bash

# Parse command line arguments
PARALLEL=true
USE_PRODUCTION=false
USE_DOCKER=false

while [[ $# -gt 0 ]]; do
    case $1 in
        -Parallel)
            PARALLEL="$2"
            shift 2
            ;;
        -UseProduction)
            USE_PRODUCTION=true
            shift
            ;;
        -UseDocker)
            USE_DOCKER=true
            shift
            ;;
        *)
            shift
            ;;
    esac
done

# Detect PowerShell command (pwsh on Linux/macOS, PowerShell on Windows with Git Bash)
if command -v pwsh &> /dev/null; then
    PWSH_CMD="pwsh"
elif command -v powershell &> /dev/null; then
    PWSH_CMD="powershell"
else
    echo "Error: PowerShell not found. Please install PowerShell Core (pwsh) or use Windows PowerShell."
    exit 1
fi

# Pass arguments to PowerShell
if [ "$USE_DOCKER" = true ]; then
    $PWSH_CMD smoke-tests.ps1 -Parallel:$$PARALLEL -UseDocker
elif [ "$USE_PRODUCTION" = true ]; then
    $PWSH_CMD smoke-tests.ps1 -Parallel:$$PARALLEL -UseProduction
else
    $PWSH_CMD smoke-tests.ps1 -Parallel:$$PARALLEL
fi