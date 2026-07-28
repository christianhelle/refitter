#!/bin/bash

rm -rf bin
rm -rf obj
rm -rf Generated
rm -rf GeneratedOutput
dotnet build-server shutdown
rm -f Refitter.MSBuild.*.nupkg
rm -f Petstore.cs PetstorePreserveOriginal.cs Output.cs

dotnet restore ../../src/Refitter.slnx
dotnet clean -c Release ../../src/Refitter.slnx
dotnet build -c Release ../../src/Refitter.MSBuild/Refitter.MSBuild.csproj
dotnet pack -c Release ../../src/Refitter.MSBuild/Refitter.MSBuild.csproj -o .
dotnet add package Refitter.MSBuild --source .
dotnet restore
dotnet run -v d -filelogger -c Release
dotnet remove package Refitter.MSBuild
rm -f Refitter.MSBuild.*.nupkg
