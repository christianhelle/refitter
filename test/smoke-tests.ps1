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

function RunTests {
    param (
        [Parameter(Mandatory=$true)]
        [ValidateSet("dotnet-run", "refitter")]
        [string]
        $Method,
        
        [Parameter(Mandatory=$false)]
        [bool]
        $Parallel = $false
    )

    "v2.0", "v3.0" | ForEach-Object {
        $version = $_
        "json", "yaml" | ForEach-Object {
            $format = $_
            Remove-Item ./**/*Output.cs -Force
            Copy-Item ./OpenAPI/$version/petstore.$format ./openapi.$format

            dotnet run `
                --project ../src/Refitter/Refitter.csproj `
                ./openapi.$format `
                --namespace GeneratedCode

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
            $process = Start-Process "dotnet" `
                -Args "build ./GeneratedCode/Generated.sln" `
                -NoNewWindow `
                -PassThru
            $process | Wait-Process
            if ($process.ExitCode -ne 0) {
                throw "Build Failed!"
            }
        }
    }
}

Measure-Command { RunTests -Method "dotnet-run" -Parallel $Parallel }
Write-Host "`r`n"