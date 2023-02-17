param (
    [Parameter(Mandatory=$false)]
    [bool]
    $Parallel = $true
)

function ThrowOnNativeFailure {
    if (-not $?)
    {
        throw "Native Failure"
    }
}

function Prepare-SwaggerPetstore {
    param (
        [Parameter(Mandatory=$true)]
        [ValidateSet("V2", "V3")]
        [string]
        $Version,

        [Parameter(Mandatory=$true)]
        [ValidateSet("json", "yaml")]
        [string]
        $Format,

        [Parameter(Mandatory=$false)]
        [switch]
        $Download
    )

    if ($Download) {
        Write-Host "`r`nDownload Swagger Petstore $Version spec ($Format)`r`n"

        if ($Version -eq "V2") {
            Invoke-WebRequest -Uri https://petstore.swagger.io/v2/swagger.$Format -OutFile Swagger.$Format
        }

        if ($Version -eq "V3") {
            Invoke-WebRequest -Uri https://petstore3.swagger.io/api/v3/openapi.$Format -OutFile Swagger.$Format
        }
    } else {
        Copy-Item ./OpenAPI/$Version/Swagger.$Format ./Swagger.$Format
    }
}

function Build-GeneratedCode {    
    param (
        [Parameter(Mandatory=$false)]
        [bool]
        $Parallel = $true
    )

    if ($Parallel) {
        $argumentsList = @(
            "build ./GeneratedCode/NetStandard20/NetStandard20.csproj",
            "build ./GeneratedCode/NetStandard21/NetStandard21.csproj",
            "build ./GeneratedCode/Net6/Net6.csproj",
            "build ./GeneratedCode/Net7/Net7.csproj",
            "build ./GeneratedCode/Net48/Net48.csproj",
            "build ./GeneratedCode/Net481/Net481.csproj",
            "build ./GeneratedCode/Net472/Net472.csproj",
            "build ./GeneratedCode/Net462/Net462.csproj"
        )
        
        $processes = ($argumentsList | ForEach-Object {
            Start-Process "dotnet" -Args $PSItem -NoNewWindow -PassThru
        })
        $processes | Wait-Process
        $processes | ForEach-Object {
            if ($_.ExitCode -ne 0) {
                throw "Build Failed!"
            }
        }
    } else {        
        Write-Host "`r`nBuilding $_`r`n"
        dotnet build ./GeneratedCode/NetStandard20/NetStandard20.csproj; ThrowOnNativeFailure
        dotnet build ./GeneratedCode/NetStandard21/NetStandard21.csproj; ThrowOnNativeFailure
        dotnet build ./GeneratedCode/Net48/Net48.csproj; ThrowOnNativeFailure
        dotnet build ./GeneratedCode/Net481/Net481.csproj; ThrowOnNativeFailure
        dotnet build ./GeneratedCode/Net472/Net472.csproj; ThrowOnNativeFailure
        dotnet build ./GeneratedCode/Net462/Net462.csproj; ThrowOnNativeFailure
        dotnet build ./GeneratedCode/Net452/Net452.csproj; ThrowOnNativeFailure        
    }
}

function RunTests {
    param (
        [Parameter(Mandatory=$true)]
        [ValidateSet("dotnet-run", "rapicgen")]
        [string]
        $Method,
        
        [Parameter(Mandatory=$false)]
        [bool]
        $Parallel = $false
    )

    "V2", "V3" | ForEach-Object {
        $version = $_
        "json", "yaml" | ForEach-Object {
            $format = $_
            Remove-Item ./**/*Output.cs -Force
            Prepare-SwaggerPetstore -Version $version -Format $format

            dotnet run --project ../src/Refitter/Refitter.csproj ./OpenAPI/$version/Swagger.$format
            Copy-Item "./Output.cs" "./GeneratedCode/Net7/Output.cs" -Force
            Copy-Item "./Output.cs" "./GeneratedCode/Net7/Output.cs" -Force
            Copy-Item "./Output.cs" "./GeneratedCode/Net6/Output.cs" -Force
            Copy-Item "./Output.cs" "./GeneratedCode/Net48/Output.cs" -Force
            Copy-Item "./Output.cs" "./GeneratedCode/Net481/Output.cs" -Force
            Copy-Item "./Output.cs" "./GeneratedCode/Net472/Output.cs" -Force
            Copy-Item "./Output.cs" "./GeneratedCode/Net462/Output.cs" -Force
            Copy-Item "./Output.cs" "./GeneratedCode/NetStandard20/Output.cs" -Force
            Copy-Item "./Output.cs" "./GeneratedCode/NetStandard21/Output.cs" -Force
            Remove-Item "./Output.cs" -Force

            Build-GeneratedCode -Parallel $Parallel
        }
    }
}

Measure-Command { RunTests -Method "dotnet-run" -Parallel $Parallel }
Write-Host "`r`n"