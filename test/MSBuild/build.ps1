Remove-Item bin -Force -Recurse
Remove-Item obj -Force -Recurse
dotnet build-server shutdown
dotnet nuget locals global-packages --clear
Remove-Item Refitter.MSBuild.*.nupkg -Force
Remove-Item Petstore.cs
dotnet restore ../../src/Refitter.slnx
dotnet clean -c release ../../src/Refitter.slnx
dotnet build -c release ../../src/Refitter/Refitter.csproj
dotnet build -c release ../../src/Refitter.MSBuild/Refitter.MSBuild.csproj
dotnet pack -c release ../../src/Refitter.MSBuild/Refitter.MSBuild.csproj -o .
dotnet add package .\Refitter.MSBuild.1.0.0.nupkg --source .
dotnet restore
dotnet add package Refitter.MSBuild --source .
dotnet run -v d -filelogger -c Release
dotnet remove package Refitter.MSBuild
Remove-Item Refitter.MSBuild.*.nupkg -Force
