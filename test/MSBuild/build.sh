#!/bin/bash
set -e

rm -rf bin
rm -rf obj
rm -rf Generated
rm -rf GeneratedOutput
dotnet build-server shutdown
rm -f Refitter.MSBuild.*.nupkg
rm -f Petstore.cs PetstorePreserveOriginal.cs Output.cs

dotnet restore ../../src/Refitter.slnx
dotnet clean -c Release ../../src/Refitter.slnx
dotnet build -c Release ../../src/Refitter/Refitter.csproj
dotnet build -c Release ../../src/Refitter.MSBuild/Refitter.MSBuild.csproj
dotnet pack -c Release ../../src/Refitter.MSBuild/Refitter.MSBuild.csproj -o .
dotnet add package Refitter.MSBuild --version 1.0.0 --no-restore
dotnet restore --configfile nuget.local.config
dotnet run --no-restore -v d -filelogger -c Release
dotnet remove package Refitter.MSBuild
rm -f Refitter.MSBuild.*.nupkg
