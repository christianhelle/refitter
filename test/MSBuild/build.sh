#!/bin/bash

rm -rf bin
rm -rf obj
dotnet build-server shutdown
dotnet nuget locals global-packages --clear
rm -f Refitter.MSBuild.*.nupkg
rm -f Petstore.cs
dotnet restore ../../src/Refitter.slnx
dotnet clean -c release ../../src/Refitter.slnx
dotnet build -c release ../../src/Refitter/Refitter.csproj
dotnet build -c release ../../src/Refitter.MSBuild/Refitter.MSBuild.csproj
dotnet pack -c release ../../src/Refitter.MSBuild/Refitter.MSBuild.csproj -o .

# Find the nupkg file and add it as a package
find . -name "Refitter.MSBuild.*.nupkg" -exec dotnet add package {} --source . \;

dotnet restore
dotnet add package Refitter.MSBuild --source .
dotnet run -v d -filelogger
dotnet remove package Refitter.MSBuild
rm -f Refitter.MSBuild.*.nupkg