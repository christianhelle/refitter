# MSBuild Task Exit Code Regression Test for Issue #1012
# This script tests that the MSBuild task properly fails when the CLI process fails

Write-Host "=== Issue #1012 MSBuild Task Exit Code Test ===" -ForegroundColor Cyan

# Clean up previous runs
Remove-Item bin -Force -Recurse -ErrorAction SilentlyContinue
Remove-Item obj -Force -Recurse -ErrorAction SilentlyContinue
Remove-Item Generated -Force -Recurse -ErrorAction SilentlyContinue
dotnet build-server shutdown
dotnet nuget locals global-packages --clear | Out-Null

# Build the Refitter.MSBuild package
Write-Host "Building Refitter.MSBuild package..." -ForegroundColor Yellow
dotnet clean -c Release ..\..\src\Refitter.MSBuild\Refitter.MSBuild.csproj
dotnet build -c Release ..\..\src\Refitter.MSBuild\Refitter.MSBuild.csproj
dotnet pack -c Release ..\..\src\Refitter.MSBuild\Refitter.MSBuild.csproj -o .
$package = Get-ChildItem -Filter "Refitter.MSBuild.*.nupkg" | Sort-Object LastWriteTime -Descending | Select-Object -First 1
if ($null -eq $package) {
    Write-Host "ERROR: Failed to locate packed Refitter.MSBuild package" -ForegroundColor Red
    exit 1
}

$packageVersion = $package.BaseName.Substring("Refitter.MSBuild.".Length)

# Create a test project with an invalid .refitter file
$testDir = "test-exit-code"
Remove-Item $testDir -Force -Recurse -ErrorAction SilentlyContinue
New-Item -ItemType Directory -Path $testDir | Out-Null

# Create a simple test project
$csprojContent = @"
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Refitter.MSBuild" Version="$packageVersion" />
    <AdditionalFiles Include="invalid.refitter" />
  </ItemGroup>
</Project>
"@
Set-Content "$testDir\TestProject.csproj" $csprojContent

# Create an invalid .refitter file (pointing to unreachable URL)
$invalidRefitterContent = @"
{
  "openApiPath": "https://invalid.example.com/nonexistent-api-spec.json",
  "namespace": "Test",
  "outputFilename": "Test.cs"
}
"@
Set-Content "$testDir\invalid.refitter" $invalidRefitterContent

# Copy the package to test directory
Copy-Item $package.FullName "$testDir\" -Force

# Create a nuget.config that uses local source
$nugetConfig = @"
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="local" value="." />
  </packageSources>
</configuration>
"@
Set-Content "$testDir\nuget.config" $nugetConfig

Push-Location $testDir
try {
    $env:NUGET_PACKAGES = Join-Path (Get-Location) ".nuget-packages"

    # Try to build the project - it should FAIL because the OpenAPI spec is unreachable
    Write-Host "Attempting build with invalid spec (should fail)..." -ForegroundColor Yellow
    dotnet restore
    
    # Run build and capture exit code
    $buildOutput = dotnet build --no-restore 2>&1
    $buildExitCode = $LASTEXITCODE
    
    if ($buildExitCode -eq 0) {
        Write-Host "ERROR: Build succeeded when it should have failed!" -ForegroundColor Red
        Write-Host "       MSBuild task is not reporting CLI failures correctly (Issue #1012)" -ForegroundColor Red
        Pop-Location
        Remove-Item ..\$testDir -Force -Recurse -ErrorAction SilentlyContinue
        exit 1
    }
    
    Write-Host "PASS: Build correctly failed with exit code $buildExitCode" -ForegroundColor Green

    $buildOutputText = $buildOutput | Out-String
    if ($buildOutputText -notmatch "Generation partially completed") {
        Write-Host "ERROR: Expected partial-failure summary message was not found in build output!" -ForegroundColor Red
        Write-Host "       RefitterGenerateTask summary is still misleading on partial failure." -ForegroundColor Red
        Pop-Location
        Remove-Item ..\$testDir -Force -Recurse -ErrorAction SilentlyContinue
        exit 1
    }

    Write-Host "PASS: Partial-failure summary message was emitted" -ForegroundColor Green
} finally {
    Remove-Item Env:NUGET_PACKAGES -ErrorAction SilentlyContinue
    Pop-Location
}

# Clean up
Remove-Item $testDir -Force -Recurse -ErrorAction SilentlyContinue

Write-Host "=== Issue #1012 Test Complete ===" -ForegroundColor Cyan
