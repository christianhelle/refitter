# Verifies that each inner build of a multi-targeted project generates into its own
# intermediate output folder, and that stale copies in outputFolder are not compiled.

$ErrorActionPreference = "Stop"
$frameworks = @("net8.0", "net10.0")

function Assert-Build([string] $description) {
    dotnet build -nr:false
    if ($LASTEXITCODE -ne 0) {
        Write-Host "ERROR: Build failed ($description)" -ForegroundColor Red
        exit 1
    }
    Write-Host "PASS: Build succeeded ($description)" -ForegroundColor Green
}

function Assert-PerTargetFrameworkOutput {
    foreach ($framework in $frameworks) {
        $path = "obj/Debug/$framework/Refitter/petstore/Generated/Petstore.cs"
        if (-not (Test-Path $path)) {
            Write-Host "ERROR: Expected $path not found" -ForegroundColor Red
            exit 1
        }
        Write-Host "PASS: $path exists" -ForegroundColor Green
    }
}

Remove-Item bin, obj, Generated -Force -Recurse -ErrorAction SilentlyContinue
Remove-Item Refitter.MSBuild.*.nupkg -Force -ErrorAction SilentlyContinue
dotnet build-server shutdown

# Clear stale NuGet cache (version 1.0.0 never changes, cache may hold stale layout)
Remove-Item "$env:NUGET_PACKAGES\refitter.msbuild" -Force -Recurse -ErrorAction SilentlyContinue
Remove-Item "~/.nuget/packages/refitter.msbuild" -Force -Recurse -ErrorAction SilentlyContinue

dotnet build -c release ../../src/Refitter/Refitter.csproj
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}
dotnet pack -c release ../../src/Refitter.MSBuild/Refitter.MSBuild.csproj -o .
# nuget.config adds this folder as a package source next to nuget.org
dotnet add package Refitter.MSBuild --version 1.0.0
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

Write-Host ""
Write-Host "=== Multi-targeting Checks ===" -ForegroundColor Cyan

Assert-Build "fresh"
Assert-PerTargetFrameworkOutput

if (Test-Path "Generated") {
    Write-Host "ERROR: Unexpected Generated folder in project directory" -ForegroundColor Red
    exit 1
}
Write-Host "PASS: Nothing generated into outputFolder" -ForegroundColor Green

# A copy left in outputFolder by an older Refitter.MSBuild must not cause duplicate types
New-Item -ItemType Directory Generated | Out-Null
Copy-Item "obj/Debug/net8.0/Refitter/petstore/Generated/Petstore.cs" "Generated/Petstore.cs"
Assert-Build "stale copy in outputFolder"

Write-Host "=== All Multi-targeting Checks Complete ===" -ForegroundColor Cyan
Write-Host ""

dotnet remove package Refitter.MSBuild
Remove-Item Generated -Force -Recurse -ErrorAction SilentlyContinue
Remove-Item Refitter.MSBuild.*.nupkg -Force
