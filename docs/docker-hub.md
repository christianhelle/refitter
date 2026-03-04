# Refitter Docker CLI

Generate C# REST API Clients from OpenAPI specifications using [Refit](https://github.com/reactiveui/refit) - no .NET installation required!

## Quick Start

```bash
# Pull the image
docker pull christianhelle/refitter

# Generate from local OpenAPI spec
docker run --rm -v $(pwd):/src christianhelle/refitter ./openapi.json

# Generate from URL
docker run --rm -v $(pwd):/src christianhelle/refitter \
  https://petstore3.swagger.io/api/v3/openapi.yaml \
  --output /src/PetStoreClient.cs
```

## Installation

```bash
# Latest version
docker pull christianhelle/refitter

# Specific version
docker pull christianhelle/refitter:1.0.0

# Verify installation
docker images | grep refitter
```

## Understanding Docker Arguments

**Basic Structure:**
```bash
docker run [DOCKER_OPTIONS] christianhelle/refitter [REFITTER_ARGUMENTS]
```

**Common Docker Options:**
- `--rm` - Auto-remove container after execution (recommended)
- `-v $(pwd):/src` - Mount current directory (Linux/macOS)
- `-v ${PWD}:/src` - Mount current directory (Windows PowerShell)
- `-v %cd%:/src` - Mount current directory (Windows CMD)
- `-u $(id -u):$(id -g)` - Run as current user (Linux/macOS, fixes file permissions)
- `-w /src` - Set working directory inside container
- `--network host` - Access localhost services

## Usage Examples

### Basic Generation

```bash
# Simple generation
docker run --rm -v $(pwd):/src christianhelle/refitter ./openapi.json

# Custom namespace and output
docker run --rm -v $(pwd):/src christianhelle/refitter \
  ./openapi.json \
  --namespace "MyCompany.Api.Client" \
  --output ./ApiClient.cs

# Using settings file
docker run --rm -v $(pwd):/src christianhelle/refitter \
  ./openapi.json \
  --settings-file ./petstore.refitter
```

### Advanced Features

```bash
# Interface only
docker run --rm -v $(pwd):/src christianhelle/refitter \
  ./openapi.json --interface-only --output ./IApiClient.cs

# Contracts only
docker run --rm -v $(pwd):/src christianhelle/refitter \
  ./openapi.json --contract-only --output ./Contracts.cs

# With API response wrappers
docker run --rm -v $(pwd):/src christianhelle/refitter \
  ./openapi.json --use-api-response

# Multiple interfaces by endpoint
docker run --rm -v $(pwd):/src christianhelle/refitter \
  ./openapi.json --multiple-interfaces ByEndpoint

# Multiple interfaces by tag
docker run --rm -v $(pwd):/src christianhelle/refitter \
  ./openapi.json --multiple-interfaces ByTag

# Filter by tags
docker run --rm -v $(pwd):/src christianhelle/refitter \
  ./openapi.json --tag Pet --tag Store

# With cancellation tokens
docker run --rm -v $(pwd):/src christianhelle/refitter \
  ./openapi.json --cancellation-tokens

# Immutable records
docker run --rm -v $(pwd):/src christianhelle/refitter \
  ./openapi.json --immutable-records

# Apizr integration
docker run --rm -v $(pwd):/src christianhelle/refitter \
  ./openapi.json --use-apizr

# Internal visibility
docker run --rm -v $(pwd):/src christianhelle/refitter \
  ./openapi.json --internal

# Custom operation names
docker run --rm -v $(pwd):/src christianhelle/refitter \
  ./openapi.json --operation-name-template '{operationName}Async'
```

## Shell Aliases

Make usage easier by creating aliases:

### Bash/Zsh (Linux/macOS)
```bash
# Add to ~/.bashrc or ~/.zshrc
alias refitter='docker run --rm -v $(pwd):/src -u $(id -u):$(id -g) christianhelle/refitter'

# Reload
source ~/.bashrc

# Use it
refitter ./openapi.json --output ./Client.cs
```

### PowerShell (Windows)
```powershell
# Add to $PROFILE
function refitter {
    docker run --rm -v ${PWD}:/src christianhelle/refitter $args
}

# Reload
. $PROFILE

# Use it
refitter ./openapi.json --output ./Client.cs
```

### CMD (Windows)
Create `refitter.bat` in your PATH:
```batch
@echo off
docker run --rm -v %cd%:/src christianhelle/refitter %*
```

## Key CLI Options

```
-n, --namespace                  Default namespace (default: GeneratedCode)
-o, --output                     Output file path (default: Output.cs)
-s, --settings-file              Path to .refitter settings file
    --contracts-namespace        Namespace for contracts
    --contracts-output           Separate output for contracts
    --interface-only             Generate only interfaces
    --contract-only              Generate only contracts
    --use-api-response           Return IApiResponse<T>
    --use-observable-response    Return IObservable
    --internal                   Internal type accessibility
    --cancellation-tokens        Add cancellation token parameters
    --multiple-interfaces        ByEndpoint or ByTag
    --multiple-files             Generate separate files
    --tag                        Filter by OpenAPI tags
    --match-path                 Filter paths with regex
    --trim-unused-schema         Remove unreferenced schemas
    --no-deprecated-operations   Skip deprecated operations
    --immutable-records          Generate records instead of classes
    --use-apizr                  Apizr integration
    --use-dynamic-querystring-parameters  Dynamic query params
    --use-polymorphic-serialization      System.Text.Json polymorphism
    --disposable                 Generate IDisposable clients
    --collection-format          Multi/Csv/Ssv/Tsv/Pipes
    --integer-type               int or long
    --operation-name-generator   Naming strategy
    --no-banner                  Hide banner
    --simple-output              Plain text output
```

## Volume Mounting

Mount your project directory to access files:

```bash
# Current directory
docker run --rm -v $(pwd):/src christianhelle/refitter /src/openapi.json

# Specific directory
docker run --rm -v /path/to/project:/src christianhelle/refitter /src/specs/api.json

# Multiple mounts
docker run --rm \
  -v $(pwd)/specs:/specs:ro \
  -v $(pwd)/output:/output \
  christianhelle/refitter /specs/api.json --output /output/Client.cs

# Read-only mount (security)
docker run --rm -v $(pwd)/specs:/specs:ro \
  -v $(pwd)/output:/output \
  christianhelle/refitter /specs/api.json --output /output/Client.cs
```

## File Permissions (Linux/macOS)

Files created in Docker containers are owned by root by default. Use `-u` flag:

```bash
docker run --rm \
  -v $(pwd):/src \
  -u $(id -u):$(id -g) \
  christianhelle/refitter ./openapi.json
```

## Troubleshooting

**"No such file or directory"**
- Mount the directory containing your OpenAPI spec: `-v $(pwd):/src`
- Use correct path inside container: `/src/openapi.json` or `./openapi.json`

**Generated files not visible**
- Ensure output path is within mounted volume
- Example: `--output /src/Client.cs` or `--output ./Client.cs`

**Permission denied (Linux/macOS)**
- Use `-u $(id -u):$(id -g)` flag

**Cannot access localhost URLs**
- Use `--network host` flag
- Example: `docker run --rm --network host -v $(pwd):/src christianhelle/refitter http://localhost:5000/swagger/v1/swagger.json`

**Volume mounting on Windows**
- PowerShell: Use `${PWD}` instead of `$(pwd)`
- CMD: Use `%cd%` instead of `$(pwd)`
- Quote paths with spaces: `-v "${PWD}:/src"`

## Complete Example

```bash
# Generate client with all common options
docker run --rm -v $(pwd):/src christianhelle/refitter ./openapi.json \
  --namespace "PetStore.Api.Client" \
  --use-api-response \
  --cancellation-tokens \
  --multiple-interfaces ByTag \
  --tag Pet \
  --tag Store \
  --no-deprecated-operations \
  --operation-name-template '{operationName}Async' \
  --trim-unused-schema \
  --immutable-records \
  --output ./PetStoreClient.cs
```

## Resources

- **Documentation:** [refitter.github.io](https://refitter.github.io)
- **GitHub:** [github.com/christianhelle/refitter](https://github.com/christianhelle/refitter)
- **Refit:** [github.com/reactiveui/refit](https://github.com/reactiveui/refit)
- **Apizr:** [apizr.net](https://www.apizr.net)

## License

MIT License - See [LICENSE](https://github.com/christianhelle/refitter/blob/main/LICENSE)
