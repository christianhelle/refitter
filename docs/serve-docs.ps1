#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Builds and serves the Refitter DocFX documentation site locally.

.PARAMETER Port
    Port to serve the site on. Defaults to 8080.

.PARAMETER SkipInstall
    Skip installing or updating the docfx global tool.

.EXAMPLE
    ./docs/serve-docs.ps1
    ./docs/serve-docs.ps1 -Port 8089 -SkipInstall
#>
param(
    [int]$Port = 8080,
    [switch]$SkipInstall
)

$ErrorActionPreference = 'Stop'

$docfxJson = Join-Path $PSScriptRoot 'docfx_project/docfx.json'

if (-not $SkipInstall) {
    dotnet tool update -g docfx
    if ($LASTEXITCODE -ne 0) { exit $LASTEXITCODE }
}

docfx $docfxJson --serve --port $Port
exit $LASTEXITCODE
