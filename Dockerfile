# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY src/Refitter.slnx .
COPY src/Directory.Build.props .
COPY src/Refitter/Refitter.csproj ./Refitter/
COPY src/Refitter.Core/Refitter.Core.csproj ./Refitter.Core/

# Restore dependencies
RUN dotnet restore Refitter/Refitter.csproj

# Copy the rest of the source code
COPY src/Refitter/ ./Refitter/
COPY src/Refitter.Core/ ./Refitter.Core/

# Build and publish the application targeting .NET 9.0
RUN dotnet publish Refitter/Refitter.csproj \
    -c Release \
    -f net9.0 \
    -o /app/publish \
    --no-restore

# Runtime stage
FROM mcr.microsoft.com/dotnet/runtime:9.0 AS runtime
WORKDIR /app

# Copy the published application from the build stage
COPY --from=build /app/publish .

# Set the entrypoint to the refitter CLI
ENTRYPOINT ["dotnet", "refitter.dll"]
