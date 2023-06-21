Write-Host "Cleaning up"
Get-ChildItem -Path Refitter/bin -Recurse | Remove-Item -Force -Recurse
Get-ChildItem -Path Refitter/obj -Recurse | Remove-Item -Force -Recurse
Get-ChildItem -Path Refitter.Core/bin -Recurse | Remove-Item -Force -Recurse
Get-ChildItem -Path Refitter.Core/obj -Recurse | Remove-Item -Force -Recurse
Get-ChildItem -Path *.nupkg -Recurse | Remove-Item -Force -Recurse
Remove-Item -Force -Recurse Refitter/bin -ErrorAction SilentlyContinue
Remove-Item -Force -Recurse Refitter/obj -ErrorAction SilentlyContinue
Remove-Item -Force -Recurse Refitter.Core/bin -ErrorAction SilentlyContinue
Remove-Item -Force -Recurse Refitter.Core/obj -ErrorAction SilentlyContinue

Write-Host "Building Refitter"
dotnet build -c Release

Write-Host "Publishing Refitter"
"win-x64","win-x86","win-arm64","win-arm","osx-x64","osx-arm64","linux-x64","linux-arm64","linux-arm" | ForEach-Object { 
    dotnet publish ./Refitter/Refitter.csproj -c Release -r $_ --self-contained
}

Write-Host "Package Refitter"
dotnet pack -c Release --no-build -o .