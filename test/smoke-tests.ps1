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

    $filenames = @(
        # "api-with-examples",
        "callback-example",
        "link-example",
        "uber",
        "uspto",
        # "petstore-expanded",
        # "petstore-minimal",
        # "petstore-simple",
        # "petstore-with-external-docs",
        "petstore"
    )
    "v2.0", "v3.0" | ForEach-Object {
        $version = $_
        "json", "yaml" | ForEach-Object {            
            $format = $_
            $filenames | ForEach-Object {
                $filename = "./OpenAPI/$version/$_.$format"
                $exists = Test-Path -Path $filename -PathType Leaf
                if ($exists -eq $true) {
                    Copy-Item $filename ./openapi.$format
                    dotnet run `
                        --project ../src/Refitter/Refitter.csproj `
                        ./openapi.$format `
                        --namespace GeneratedCode

                    Copy-Item "./Output.cs" "./$version-$_-$format.cs"
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
    }
}

Remove-Item ./v2.0-*.cs -Force
Remove-Item ./v3.0-*.cs -Force
Remove-Item ./v3.1-*.cs -Force
Remove-Item ./**/*Output.cs -Force
Measure-Command { RunTests -Method "dotnet-run" -Parallel $Parallel }
Write-Host "`r`n"