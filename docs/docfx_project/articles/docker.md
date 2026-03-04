# Docker Usage

Refitter is a CLI tool for generating a C# REST API Client using the [Refit](https://github.com/reactiveui/refit) library. Refitter can generate the Refit interface from OpenAPI specifications. Refitter can format the generated Refit interface to be managed by [Apizr](https://www.apizr.net) (v6+) and generate some registration helpers too.

This guide covers how to use Refitter using Docker, which provides a consistent environment without requiring .NET installation on your machine.

## Table of Contents

- [Installation](#installation)
- [Understanding Docker Run Arguments](#understanding-docker-run-arguments)
- [Basic Usage](#basic-usage)
- [Volume Mounting Explained](#volume-mounting-explained)
- [Advanced Usage Examples](#advanced-usage-examples)
- [Creating Shell Aliases](#creating-shell-aliases)
- [All CLI Options](#all-cli-options)
- [Troubleshooting](#troubleshooting)

## Installation

### Pulling the Docker Image

To download the latest Refitter Docker image from Docker Hub:

```shell
docker pull christianhelle/refitter
```

To pull a specific version:

```shell
docker pull christianhelle/refitter:1.0.0
```

To verify the image is installed:

```shell
docker images | grep refitter
```

You don't need to pull the image explicitly - Docker will automatically download it the first time you run it. However, pulling it in advance can be useful to ensure you have the latest version or to avoid delays during first use.

## Understanding Docker Run Arguments

When running Refitter with Docker, you'll use the `docker run` command. Here's a breakdown of each Docker argument you'll encounter:

### Basic Docker Run Command Structure

```shell
docker run [DOCKER_OPTIONS] IMAGE_NAME [REFITTER_ARGUMENTS]
```

### Common Docker Options Explained

- **`docker run`** - The Docker command to create and start a new container from an image

- **`--rm`** - Automatically removes the container when it exits. This keeps your system clean by not leaving stopped containers behind. Highly recommended for CLI tools.

- **`-v` or `--volume`** - Mounts a directory or file from your host machine into the container
  - Format: `-v HOST_PATH:CONTAINER_PATH`
  - Example: `-v $(pwd):/src` mounts your current directory to `/src` in the container
  - **Windows (PowerShell)**: Use `${PWD}` instead of `$(pwd)`
  - **Windows (CMD)**: Use `%cd%` instead of `$(pwd)`

- **`-w` or `--workdir`** - Sets the working directory inside the container where commands will execute
  - Example: `-w /src` sets `/src` as the working directory

- **`-e` or `--env`** - Sets environment variables in the container
  - Example: `-e "MY_VAR=value"`

- **`-u` or `--user`** - Runs the container as a specific user (useful for file permissions)
  - Example: `-u $(id -u):$(id -g)` runs as your current user on Linux/macOS

- **`-i` or `--interactive`** - Keeps STDIN open (useful for interactive applications)

- **`-t` or `--tty`** - Allocates a pseudo-TTY (terminal)

- **`--name`** - Assigns a name to the container for easy reference

- **`--network`** - Connects the container to a specific network
  - Example: `--network host` uses the host network (useful for accessing local services)

### Image Name

- **`christianhelle/refitter`** - The Docker image name (repository/image)
- **`christianhelle/refitter:latest`** - Specifies the latest version tag
- **`christianhelle/refitter:1.0.0`** - Specifies a specific version tag

### Refitter Arguments

Everything after the image name is passed to the Refitter CLI tool itself. These are the same arguments you would use if running Refitter directly on your machine.

## Basic Usage

### Minimal Example

Generate code from a local OpenAPI specification file:

```shell
docker run --rm -v $(pwd):/src christianhelle/refitter ./openapi.json
```

**What this does:**
- `docker run` - Creates and runs a new container
- `--rm` - Removes the container after it finishes
- `-v $(pwd):/src` - Mounts your current directory to `/src` in the container
- `christianhelle/refitter` - The Docker image to use
- `./openapi.json` - The OpenAPI spec file path (relative to the mounted volume)

This will generate `Output.cs` in your current directory.

### Specify Output File

```shell
docker run --rm -v $(pwd):/src christianhelle/refitter ./openapi.json --output ./GeneratedClient.cs
```

### Specify Namespace

```shell
docker run --rm -v $(pwd):/src christianhelle/refitter ./openapi.json --namespace "MyCompany.Api.Client" --output ./ApiClient.cs
```

### Generate from URL

```shell
docker run --rm christianhelle/refitter https://petstore3.swagger.io/api/v3/openapi.yaml --output ./PetStoreClient.cs
```

**Note:** When generating from a URL, you still need to mount a volume if you want the output file saved to your host machine:

```shell
docker run --rm -v $(pwd):/src christianhelle/refitter https://petstore3.swagger.io/api/v3/openapi.yaml --output /src/PetStoreClient.cs
```

### Using a Settings File

```shell
docker run --rm -v $(pwd):/src christianhelle/refitter ./openapi.json --settings-file ./petstore.refitter
```

## Volume Mounting Explained

Volume mounting (`-v` or `--volume`) is crucial for Docker-based CLI tools because containers are isolated from your file system by default.

### Why Volume Mounting is Necessary

1. **Reading Input Files**: The container needs access to your OpenAPI specification files
2. **Writing Output Files**: Generated code needs to be saved to your host machine, not just inside the container
3. **Accessing Settings Files**: Configuration files (`.refitter` files) need to be accessible

### Volume Mounting Patterns

#### Mount Current Directory (Most Common)

```shell
# Linux/macOS
docker run --rm -v $(pwd):/src christianhelle/refitter ./openapi.json

# Windows PowerShell
docker run --rm -v ${PWD}:/src christianhelle/refitter ./openapi.json

# Windows CMD
docker run --rm -v %cd%:/src christianhelle/refitter ./openapi.json
```

#### Mount Specific Directory

```shell
docker run --rm -v /path/to/your/project:/src christianhelle/refitter /src/specs/openapi.json --output /src/generated/Client.cs
```

#### Mount Multiple Directories

```shell
docker run --rm \
  -v /path/to/specs:/specs \
  -v /path/to/output:/output \
  christianhelle/refitter /specs/openapi.json --output /output/Client.cs
```

#### Read-Only Mounts

For security, you can mount directories as read-only:

```shell
docker run --rm \
  -v $(pwd)/specs:/specs:ro \
  -v $(pwd)/output:/output \
  christianhelle/refitter /specs/openapi.json --output /output/Client.cs
```

### File Permission Considerations

On Linux and macOS, files created by Docker containers are owned by the root user by default. To create files with your user ownership:

```shell
docker run --rm \
  -v $(pwd):/src \
  -u $(id -u):$(id -g) \
  christianhelle/refitter ./openapi.json --output ./Client.cs
```

## Advanced Usage Examples

### Generate Interface Only

```shell
docker run --rm -v $(pwd):/src christianhelle/refitter ./openapi.json --interface-only --output ./IApiClient.cs
```

### Generate Contracts Only

```shell
docker run --rm -v $(pwd):/src christianhelle/refitter ./openapi.json --contract-only --output ./Contracts.cs
```

### Use API Response Wrappers

```shell
docker run --rm -v $(pwd):/src christianhelle/refitter ./openapi.json --use-api-response --output ./Client.cs
```

### Generate with Cancellation Tokens

```shell
docker run --rm -v $(pwd):/src christianhelle/refitter ./openapi.json --cancellation-tokens --output ./Client.cs
```

### Multiple Interfaces by Endpoint

```shell
docker run --rm -v $(pwd):/src christianhelle/refitter ./openapi.json --multiple-interfaces ByEndpoint --output ./Client.cs
```

### Multiple Interfaces by Tag

```shell
docker run --rm -v $(pwd):/src christianhelle/refitter ./openapi.json --multiple-interfaces ByTag --output ./Client.cs
```

### Filter by Tags

```shell
docker run --rm -v $(pwd):/src christianhelle/refitter ./openapi.json --tag Pet --tag Store --output ./Client.cs
```

### Filter by Path Pattern

```shell
docker run --rm -v $(pwd):/src christianhelle/refitter ./openapi.json --match-path '^/pet/.*' --output ./Client.cs
```

### Internal Type Accessibility

```shell
docker run --rm -v $(pwd):/src christianhelle/refitter ./openapi.json --internal --output ./Client.cs
```

### ISO Date Format

```shell
docker run --rm -v $(pwd):/src christianhelle/refitter ./openapi.json --use-iso-date-format --output ./Client.cs
```

### Additional Namespaces

```shell
docker run --rm -v $(pwd):/src christianhelle/refitter ./openapi.json \
  --additional-namespace "System.Text.Json" \
  --additional-namespace "MyCompany.SharedModels" \
  --output ./Client.cs
```

### Trim Unused Schema

```shell
docker run --rm -v $(pwd):/src christianhelle/refitter ./openapi.json \
  --trim-unused-schema \
  --keep-schema '^Model$' \
  --keep-schema '^Person.+' \
  --output ./Client.cs
```

### Skip Deprecated Operations

```shell
docker run --rm -v $(pwd):/src christianhelle/refitter ./openapi.json --no-deprecated-operations --output ./Client.cs
```

### Custom Operation Name Template

```shell
docker run --rm -v $(pwd):/src christianhelle/refitter ./openapi.json \
  --operation-name-template '{operationName}Async' \
  --output ./Client.cs
```

### Optional Nullable Parameters

```shell
docker run --rm -v $(pwd):/src christianhelle/refitter ./openapi.json --optional-nullable-parameters --output ./Client.cs
```

### Use Apizr Integration

```shell
docker run --rm -v $(pwd):/src christianhelle/refitter ./openapi.json --use-apizr --output ./Client.cs
```

### Dynamic Query String Parameters

```shell
docker run --rm -v $(pwd):/src christianhelle/refitter ./openapi.json --use-dynamic-querystring-parameters --output ./Client.cs
```

### Polymorphic Serialization

```shell
docker run --rm -v $(pwd):/src christianhelle/refitter ./openapi.json --use-polymorphic-serialization --output ./Client.cs
```

### Disposable Clients

```shell
docker run --rm -v $(pwd):/src christianhelle/refitter ./openapi.json --disposable --output ./Client.cs
```

### Immutable Records

```shell
docker run --rm -v $(pwd):/src christianhelle/refitter ./openapi.json --immutable-records --output ./Client.cs
```

### Collection Format

```shell
docker run --rm -v $(pwd):/src christianhelle/refitter ./openapi.json --collection-format Csv --output ./Client.cs
```

### Custom Integer Type

```shell
docker run --rm -v $(pwd):/src christianhelle/refitter ./openapi.json --integer-type long --output ./Client.cs
```

### No Auto-Generated Header

```shell
docker run --rm -v $(pwd):/src christianhelle/refitter ./openapi.json --no-auto-generated-header --output ./Client.cs
```

### No Banner (Quiet Mode)

```shell
docker run --rm -v $(pwd):/src christianhelle/refitter ./openapi.json --no-banner --output ./Client.cs
```

### Multiple Files Output

```shell
docker run --rm -v $(pwd):/src christianhelle/refitter ./openapi.json \
  --multiple-files \
  --output ./generated/ \
  --namespace "MyApi.Client"
```

This generates multiple files:
- `RefitInterfaces.cs`
- `DependencyInjection.cs`
- `Contracts.cs`

### Separate Contracts Namespace and Output

```shell
docker run --rm -v $(pwd):/src christianhelle/refitter ./openapi.json \
  --namespace "MyApi.Client" \
  --contracts-namespace "MyApi.Contracts" \
  --contracts-output ./contracts/ \
  --output ./clients/
```

### Custom Operation Name Generator

```shell
docker run --rm -v $(pwd):/src christianhelle/refitter ./openapi.json \
  --operation-name-generator MultipleClientsFromOperationId \
  --output ./Client.cs
```

Available operation name generators:
- `Default`
- `MultipleClientsFromOperationId`
- `MultipleClientsFromPathSegments`
- `MultipleClientsFromFirstTagAndOperationId`
- `MultipleClientsFromFirstTagAndOperationName`
- `MultipleClientsFromFirstTagAndPathSegments`
- `SingleClientFromOperationId`
- `SingleClientFromPathSegments`

### Combining Multiple Options

```shell
docker run --rm -v $(pwd):/src christianhelle/refitter ./openapi.json \
  --namespace "PetStore.Api.Client" \
  --use-api-response \
  --cancellation-tokens \
  --internal \
  --multiple-interfaces ByTag \
  --tag Pet \
  --tag Store \
  --no-deprecated-operations \
  --operation-name-template '{operationName}Async' \
  --trim-unused-schema \
  --immutable-records \
  --output ./PetStoreClient.cs
```

## Creating Shell Aliases

To avoid typing the full `docker run` command every time, you can create shell aliases.

### Bash/Zsh (Linux/macOS)

Add to your `~/.bashrc`, `~/.zshrc`, or `~/.bash_profile`:

```bash
# Basic alias
alias refitter='docker run --rm -v $(pwd):/src christianhelle/refitter'

# With user permissions (Linux/macOS)
alias refitter='docker run --rm -v $(pwd):/src -u $(id -u):$(id -g) christianhelle/refitter'

# More explicit alias that changes to /src working directory
alias refitter='docker run --rm -v $(pwd):/src -w /src christianhelle/refitter'
```

After adding the alias, reload your shell configuration:

```bash
source ~/.bashrc  # or ~/.zshrc
```

Now you can use Refitter as if it were installed locally:

```bash
refitter ./openapi.json --output ./Client.cs
refitter https://petstore3.swagger.io/api/v3/openapi.yaml --namespace "PetStore" --output ./PetStoreClient.cs
```

### PowerShell (Windows)

Add to your PowerShell profile (`$PROFILE`):

```powershell
# Basic alias
function refitter {
    docker run --rm -v ${PWD}:/src christianhelle/refitter $args
}

# Or as a more explicit function
function Invoke-Refitter {
    [CmdletBinding()]
    param(
        [Parameter(ValueFromRemainingArguments)]
        [string[]]$Arguments
    )

    docker run --rm -v ${PWD}:/src -w /src christianhelle/refitter $Arguments
}

Set-Alias refitter Invoke-Refitter
```

To edit your PowerShell profile:

```powershell
notepad $PROFILE
```

If the profile doesn't exist, create it:

```powershell
New-Item -Path $PROFILE -Type File -Force
```

After saving, reload your profile:

```powershell
. $PROFILE
```

Usage:

```powershell
refitter ./openapi.json --output ./Client.cs
refitter https://petstore3.swagger.io/api/v3/openapi.yaml --namespace "PetStore" --output ./PetStoreClient.cs
```

### Windows CMD

Create a batch file named `refitter.bat` in a directory that's in your PATH (e.g., `C:\Windows` or `C:\bin`):

```batch
@echo off
docker run --rm -v %cd%:/src christianhelle/refitter %*
```

Usage:

```cmd
refitter .\openapi.json --output .\Client.cs
```

### Fish Shell (Linux/macOS)

Add to your `~/.config/fish/config.fish`:

```fish
function refitter
    docker run --rm -v (pwd):/src christianhelle/refitter $argv
end
```

Reload the configuration:

```fish
source ~/.config/fish/config.fish
```

### Advanced Alias with Custom Options

Create a more sophisticated alias that includes common options:

**Bash/Zsh:**

```bash
# Function with default options
refitter() {
    local volume="${REFITTER_VOLUME:-$(pwd)}"
    docker run --rm \
      -v "$volume:/src" \
      -u $(id -u):$(id -g) \
      -w /src \
      christianhelle/refitter "$@"
}

# Custom version selection
refitter-dev() {
    docker run --rm -v $(pwd):/src christianhelle/refitter:dev "$@"
}

# With network access to local services
refitter-local() {
    docker run --rm -v $(pwd):/src --network host christianhelle/refitter "$@"
}
```

**PowerShell:**

```powershell
function refitter {
    $volume = if ($env:REFITTER_VOLUME) { $env:REFITTER_VOLUME } else { $PWD }
    docker run --rm -v "${volume}:/src" -w /src christianhelle/refitter $args
}

function refitter-dev {
    docker run --rm -v ${PWD}:/src christianhelle/refitter:dev $args
}
```

## All CLI Options

Here's a complete reference of all Refitter CLI options:

```text
USAGE:
    refitter [URL or input file] [OPTIONS]

ARGUMENTS:
    [URL or input file]    URL or file path to OpenAPI Specification file

OPTIONS:
                                                DEFAULT
    -h, --help                                                   Prints help information
    -v, --version                                                Prints version information
    -s, --settings-file                                          Path to .refitter settings file. Specifying this will ignore all other settings (except for --output)
    -n, --namespace                             GeneratedCode    Default namespace to use for generated types
        --contracts-namespace                                    Default namespace to use for generated contracts
    -o, --output                                Output.cs        Path to Output file or folder (if multiple files are generated)
        --contracts-output                                       Output path for generated contracts. Enabling this automatically enables generating multiple files
        --no-auto-generated-header                               Don't add <auto-generated> header to output file
        --no-xml-doc-comments                                    Don't generate XML doc comments for interfaces and operations
        --no-accept-headers                                      Don't add <Accept> header to output file
        --interface-only                                         Don't generate contract types
        --contract-only                                          Don't generate clients
        --use-api-response                                       Return Task<IApiResponse<T>> instead of Task<T>
        --use-observable-response                                Return IObservable instead of Task
        --internal                                               Set the accessibility of the generated types to 'internal'
        --cancellation-tokens                                    Use cancellation tokens
        --no-operation-headers                                   Don't generate operation headers
        --ignored-operation-headers                              A collection of headers to omit from operation signatures
        --no-logging                                             Don't log errors or collect telemetry
        --additional-namespace                                   Add additional namespace to generated types
        --exclude-namespace                                      Exclude namespace on generated types
        --use-iso-date-format                                    Explicitly format date query string parameters in ISO 8601 standard date format using delimiters (2023-06-15)
        --multiple-interfaces                                    Generate a Refit interface for each endpoint. May be one of ByEndpoint, ByTag
        --multiple-files                                         Generate multiple files instead of a single large file.
                                                                 The output files can be the following:
                                                                 - RefitInterfaces.cs
                                                                 - DependencyInjection.cs
                                                                 - Contracts.cs
        --match-path                                             Only include Paths that match the provided regular expression. May be set multiple times
        --tag                                                    Only include Endpoints that contain this tag. May be set multiple times and result in OR'ed evaluation
        --skip-validation                                        Skip validation of the OpenAPI specification
        --no-deprecated-operations                               Don't generate deprecated operations
        --operation-name-template                                Generate operation names using pattern. When using --multiple-interfaces ByEndpoint, this is name of the Execute() method in the interface where all instances of the string '{operationName}' is replaced with 'Execute'
        --optional-nullable-parameters                           Generate nullable parameters as optional parameters
        --trim-unused-schema                                     Removes unreferenced components schema to keep the generated output to a minimum
        --keep-schema                                            Force to keep matching schema, uses regular expressions. Use together with "--trim-unused-schema". Can be set multiple times
        --include-inheritance-hierarchy                          Keep all possible inherited types/union types even if they are not directly used
        --no-banner                                              Don't show donation banner
        --skip-default-additional-properties                     Set to true to skip default additional properties
        --simple-output                                          Generate simple, plain-text console output without ASCII art, tables, emojis, or color formatting
        --collection-format                      Multi           Determines the format of collection parameters. May be one of:
                                                                 - Multi (separate parameter instances for each array item)
                                                                 - Csv (comma separated values)
                                                                 - Ssv (space separated values)
                                                                 - Tsv (tab separated values)
                                                                 - Pipes (pipe separated values)
        --operation-name-generator              Default          The NSwag IOperationNameGenerator implementation to use.
                                                                 May be one of:
                                                                 - Default
                                                                 - MultipleClientsFromOperationId
                                                                 - MultipleClientsFromPathSegments
                                                                 - MultipleClientsFromFirstTagAndOperationId
                                                                 - MultipleClientsFromFirstTagAndOperationName
                                                                 - MultipleClientsFromFirstTagAndPathSegments
                                                                 - SingleClientFromOperationId
                                                                 - SingleClientFromPathSegments
                                                                 See https://refitter.github.io/api/Refitter.Core.OperationNameGeneratorTypes.html for more information
        --immutable-records                                      Generate contracts as immutable records instead of classes
        --use-apizr                                              Use Apizr by:
                                                                 - Adding a final IApizrRequestOptions options parameter to all generated methods
                                                                 - Providing cancellation tokens by Apizr request options instead of a dedicated parameter
                                                                 - Using method overloads instead of optional parameters
                                                                 See https://refitter.github.io for more information and https://www.apizr.net to get started with Apizr
        --use-dynamic-querystring-parameters                     Enable wrapping multiple query parameters into a single complex one. Default is no wrapping.
                                                                 See https://github.com/reactiveui/refit?tab=readme-ov-file#dynamic-querystring-parameters for more information
        --use-polymorphic-serialization                          Use System.Text.Json polymorphic serialization.
                                                                 Replaces NSwag JsonInheritanceConverter attributes with System.Text.Json JsonPolymorphicAttributes.
                                                                 To have the native support of inheritance (de)serialization and fallback to base types when
                                                                 payloads with (yet) unknown types are offered by newer versions of an API
                                                                 See https://learn.microsoft.com/en-us/dotnet/standard/serialization/system-text-json/polymorphism for more information
        --disposable                                             Generate refit clients that implement IDisposable
        --no-inline-json-converters                              Don't inline JsonConverter attributes for enum properties. When disabled, enum properties will not have [JsonConverter(typeof(JsonStringEnumConverter))] attributes
        --integer-type                           int             The .NET type to use for OpenAPI integer types without a format specifier. Common values: 'int' (default), 'long'
        --custom-template-directory                              Custom directory with NSwag fluid templates for code generation. Default is null which uses the default NSwag templates. See https://github.com/RicoSuter/NSwag/wiki/Templates
```

## Troubleshooting

### Common Issues and Solutions

#### Issue: "No such file or directory" when reading OpenAPI spec

**Problem:** The container can't access your OpenAPI specification file.

**Solution:** Ensure you've mounted the directory containing the file:

```shell
# Wrong - file not accessible
docker run --rm christianhelle/refitter ./openapi.json

# Correct - mount current directory
docker run --rm -v $(pwd):/src christianhelle/refitter /src/openapi.json
# or
docker run --rm -v $(pwd):/src christianhelle/refitter ./openapi.json
```

#### Issue: Generated files are not visible on host

**Problem:** Files are created inside the container but not on your host machine.

**Solution:** Mount a volume for the output location:

```shell
# Ensure output directory is within mounted volume
docker run --rm -v $(pwd):/src christianhelle/refitter ./openapi.json --output /src/Client.cs
```

#### Issue: Permission denied when writing files (Linux/macOS)

**Problem:** Files created by Docker are owned by root.

**Solution:** Run the container with your user ID:

```shell
docker run --rm -v $(pwd):/src -u $(id -u):$(id -g) christianhelle/refitter ./openapi.json
```

#### Issue: Cannot access URLs from container

**Problem:** The container can't download OpenAPI specs from URLs.

**Solution:** Most URLs should work out of the box. For local URLs (localhost), use host networking:

```shell
docker run --rm --network host -v $(pwd):/src christianhelle/refitter http://localhost:5000/swagger/v1/swagger.json
```

#### Issue: Volume mounting not working on Windows

**Problem:** Docker can't mount Windows paths correctly.

**Solution:** Use the appropriate path syntax for your shell:

```powershell
# PowerShell
docker run --rm -v ${PWD}:/src christianhelle/refitter ./openapi.json

# CMD
docker run --rm -v %cd%:/src christianhelle/refitter ./openapi.json
```

For paths with spaces, use quotes:

```powershell
docker run --rm -v "${PWD}:/src" christianhelle/refitter ./openapi.json
```

#### Issue: "docker: command not found"

**Problem:** Docker is not installed or not in PATH.

**Solution:** Install Docker Desktop from [docker.com](https://www.docker.com/products/docker-desktop) and ensure it's running.

#### Issue: Settings file not found

**Problem:** The `.refitter` settings file can't be located.

**Solution:** Ensure the settings file is in the mounted volume:

```shell
# If settings file is in current directory
docker run --rm -v $(pwd):/src christianhelle/refitter ./openapi.json --settings-file /src/petstore.refitter
```

#### Issue: Custom template directory not accessible

**Problem:** Custom NSwag templates can't be found.

**Solution:** Mount the template directory:

```shell
docker run --rm \
  -v $(pwd):/src \
  -v $(pwd)/templates:/templates:ro \
  christianhelle/refitter ./openapi.json --custom-template-directory /templates
```

### Getting Help

- **GitHub Issues:** [https://github.com/christianhelle/refitter/issues](https://github.com/christianhelle/refitter/issues)
- **Documentation:** [https://refitter.github.io](https://refitter.github.io)
- **Docker Hub:** [https://hub.docker.com/r/christianhelle/refitter](https://hub.docker.com/r/christianhelle/refitter)

### Viewing Container Logs

To see detailed logs if something goes wrong:

```shell
docker run --rm -v $(pwd):/src christianhelle/refitter ./openapi.json --output ./Client.cs
```

The `--no-logging` flag disables telemetry collection but not console output. To see minimal output, use:

```shell
docker run --rm -v $(pwd):/src christianhelle/refitter ./openapi.json --no-banner --output ./Client.cs
```

### Verifying the Image

To check the image details:

```shell
docker inspect christianhelle/refitter
```

To see the image version:

```shell
docker run --rm christianhelle/refitter --version
```

### Updating the Image

To ensure you have the latest version:

```shell
docker pull christianhelle/refitter:latest
```

To clean up old versions:

```shell
docker images | grep refitter
docker rmi christianhelle/refitter:old-version
```

## Additional Resources

- **Refitter Documentation:** [https://refitter.github.io](https://refitter.github.io)
- **Refit Documentation:** [https://github.com/reactiveui/refit](https://github.com/reactiveui/refit)
- **Apizr Documentation:** [https://www.apizr.net](https://www.apizr.net)
- **Docker Documentation:** [https://docs.docker.com](https://docs.docker.com)
- **OpenAPI Specification:** [https://swagger.io/specification/](https://swagger.io/specification/)

## Contributing

If you find issues with the Docker image or have suggestions for improvement, please open an issue or pull request at [https://github.com/christianhelle/refitter](https://github.com/christianhelle/refitter).

## License

Refitter is licensed under the MIT License. See the [LICENSE](https://github.com/christianhelle/refitter/blob/main/LICENSE) file for details.
