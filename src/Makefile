# Default target
all: build

# Build target
build:
	dotnet build --configuration Debug Refitter.sln

# Optional release build target
release:
	dotnet build --configuration Release Refitter.sln

# Test target
test:
	dotnet test --configuration Debug Refitter.sln

# Clean target
clean:
	dotnet clean Refitter.sln
	rm -rf ./Refitter/bin
	rm -rf ./Refitter/obj
	rm -rf ./Refitter.Core/bin
	rm -rf ./Refitter.Core/obj
