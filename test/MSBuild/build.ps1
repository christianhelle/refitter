Remove-Item bin -Force -Recurse -ErrorAction SilentlyContinue
Remove-Item obj -Force -Recurse -ErrorAction SilentlyContinue
Remove-Item GeneratedOutput -Force -Recurse -ErrorAction SilentlyContinue
Remove-Item Generated -Force -Recurse -ErrorAction SilentlyContinue
dotnet build-server shutdown
Remove-Item Refitter.MSBuild.*.nupkg -Force -ErrorAction SilentlyContinue
Remove-Item Petstore.cs -ErrorAction SilentlyContinue
Remove-Item PetstorePreserveOriginal.cs -ErrorAction SilentlyContinue
Remove-Item Output.cs -ErrorAction SilentlyContinue # Issue #998: Should not exist

dotnet restore ../../src/Refitter.slnx
dotnet clean -c release ../../src/Refitter.slnx
dotnet build -c release ../../src/Refitter/Refitter.csproj
dotnet build -c release ../../src/Refitter.MSBuild/Refitter.MSBuild.csproj
dotnet pack -c release ../../src/Refitter.MSBuild/Refitter.MSBuild.csproj -o .
dotnet add package .\Refitter.MSBuild.1.0.0.nupkg --source .
dotnet restore
dotnet add package Refitter.MSBuild --source .
dotnet run -v d -filelogger -c Release

# Issue #998 regression assertions
Write-Host ""
Write-Host "=== Issue #998 Regression Checks ===" -ForegroundColor Cyan

# Check that expected files exist under Generated (default output folder)
if (!(Test-Path "Generated\Petstore.cs")) {
    Write-Host "ERROR: Expected Generated\Petstore.cs not found (from petstore.refitter with output filename)" -ForegroundColor Red
    exit 1
}
Write-Host "PASS: Generated\Petstore.cs exists (respects output filename + default outputFolder)" -ForegroundColor Green

if (!(Test-Path "Generated\PetstorePreserveOriginal.cs")) {
    Write-Host "ERROR: Expected Generated\PetstorePreserveOriginal.cs not found (from petstore-preserve-original.refitter)" -ForegroundColor Red
    exit 1
}
Write-Host "PASS: Generated\PetstorePreserveOriginal.cs exists (respects output filename + default outputFolder)" -ForegroundColor Green

# Ensure files are not placed in root
if (Test-Path "Petstore.cs") {
    Write-Host "ERROR: Unexpected Petstore.cs found in root (outputFolder default ignored)" -ForegroundColor Red
    exit 1
}
if (Test-Path "PetstorePreserveOriginal.cs") {
    Write-Host "ERROR: Unexpected PetstorePreserveOriginal.cs found in root (outputFolder default ignored)" -ForegroundColor Red
    exit 1
}
Write-Host "PASS: No stray Petstore*.cs in root directory" -ForegroundColor Green

# Check that stray Output.cs does NOT exist (the bug symptom)
if (Test-Path "Output.cs") {
    Write-Host "ERROR: Unexpected Output.cs found in root (Issue #998 symptom)" -ForegroundColor Red
    Write-Host "       This indicates outputFolder or outputFilename settings were ignored" -ForegroundColor Red
    exit 1
}
Write-Host "PASS: No stray Output.cs in root directory" -ForegroundColor Green

# Check interface naming (smoke test for naming.interfaceName)
$petstoreContent = Get-Content "Generated\Petstore.cs" -Raw
if ($petstoreContent -notmatch "interface ISwaggerPetstore") {
    Write-Host "ERROR: Expected interface ISwaggerPetstore not found in Petstore.cs" -ForegroundColor Red
    Write-Host "       naming.interfaceName setting may have been ignored" -ForegroundColor Red
    exit 1
} else {
    Write-Host "PASS: Interface ISwaggerPetstore found (respects naming.interfaceName)" -ForegroundColor Green
}

# Check outputFolder when explicitly specified
if (!(Test-Path "GeneratedOutput\PetstoreWithFolder.cs")) {
    Write-Host "ERROR: Expected GeneratedOutput\PetstoreWithFolder.cs not found (from petstore-with-outputfolder.refitter)" -ForegroundColor Red
    exit 1
}
Write-Host "PASS: GeneratedOutput\PetstoreWithFolder.cs exists (respects explicit outputFolder setting)" -ForegroundColor Green

$petstoreWithFolderContent = Get-Content "GeneratedOutput\PetstoreWithFolder.cs" -Raw
if ($petstoreWithFolderContent -notmatch "interface ISwaggerPetstoreWithFolder") {
    Write-Host "ERROR: Expected interface ISwaggerPetstoreWithFolder not found in GeneratedOutput\PetstoreWithFolder.cs" -ForegroundColor Red
    Write-Host "       naming.interfaceName setting may have been ignored" -ForegroundColor Red
    exit 1
} else {
    Write-Host "PASS: Interface ISwaggerPetstoreWithFolder found (respects naming.interfaceName with outputFolder)" -ForegroundColor Green
}

Write-Host "=== All Issue #998 Checks Complete ===" -ForegroundColor Cyan
Write-Host ""

dotnet remove package Refitter.MSBuild
Remove-Item Refitter.MSBuild.*.nupkg -Force
