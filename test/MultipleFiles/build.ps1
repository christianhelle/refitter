rm ./**/*.cs
dotnet run --project ..\..\src\Refitter\Refitter.csproj -- --settings-file .\petstore.refitter
dotnet build
