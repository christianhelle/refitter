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

            Write-Host "`r`nBuilding $_`r`n"
            dotnet build ./GeneratedCode/Generated.sln ThrowOnNativeFailure
        }
    }
}

Measure-Command { RunTests -Method "dotnet-run" -Parallel $Parallel }
Write-Host "`r`n"