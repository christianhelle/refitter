#!/usr/bin/env bash
# Builds and serves the Refitter DocFX documentation site locally.
#
# Usage: ./docs/serve-docs.sh [--port <port>] [--skip-install]

set -euo pipefail

port=8080
skip_install=false

while [[ $# -gt 0 ]]; do
    case "$1" in
        --port)
            port="$2"
            shift 2
            ;;
        --skip-install)
            skip_install=true
            shift
            ;;
        -h|--help)
            echo "Usage: $0 [--port <port>] [--skip-install]"
            exit 0
            ;;
        *)
            echo "Unknown argument: $1" >&2
            exit 1
            ;;
    esac
done

script_dir="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

if [[ "$skip_install" != true ]]; then
    dotnet tool update -g docfx
fi

docfx "$script_dir/docfx_project/docfx.json" --serve --port "$port"
