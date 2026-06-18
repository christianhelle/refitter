#!/bin/bash

# Find a dotnet that has SDK 10 (required by global.json)
# On WSL, the Windows dotnet might be needed if the Linux one lacks SDK 10
DOTNET="$(command -v dotnet.exe 2>/dev/null || echo '')"
if [ -z "$DOTNET" ]; then
    if dotnet --list-sdks 2>/dev/null | grep -q "10\."; then
        DOTNET="dotnet"
    elif [ -x "/mnt/c/Program Files/dotnet/dotnet.exe" ]; then
        DOTNET="/mnt/c/Program Files/dotnet/dotnet.exe"
    else
        echo "ERROR: .NET SDK 10.0.x is required but not found."
        echo "  Install .NET 10 SDK or update global.json to match an installed SDK."
        echo "  Installed SDKs:"
        dotnet --list-sdks 2>/dev/null || echo "  (none detected)"
        exit 1
    fi
fi

rm -rf bin
rm -rf obj
rm -rf Generated
rm -rf GeneratedOutput
"$DOTNET" build-server shutdown
rm -f Refitter.MSBuild.*.nupkg
rm -f Petstore.cs PetstorePreserveOriginal.cs Output.cs

"$DOTNET" restore ../../src/Refitter.slnx
"$DOTNET" clean -c Release ../../src/Refitter.slnx
"$DOTNET" build -c Release ../../src/Refitter.MSBuild/Refitter.MSBuild.csproj
"$DOTNET" pack -c Release ../../src/Refitter.MSBuild/Refitter.MSBuild.csproj -o .
"$DOTNET" add package Refitter.MSBuild --source .
"$DOTNET" restore
"$DOTNET" run -v d -filelogger -c Release
"$DOTNET" remove package Refitter.MSBuild
rm -f Refitter.MSBuild.*.nupkg