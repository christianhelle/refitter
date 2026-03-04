# Default target
all: build

# Build target
build:
	dotnet build --configuration Debug Refitter.slnx

# Optional release build target
release:
	dotnet build --configuration Release Refitter.slnx

# Test target
test:
	dotnet test --configuration Debug --solution Refitter.slnx

# Clean target
clean:
	dotnet clean Refitter.slnx
	rm -rf ./Refitter/bin
	rm -rf ./Refitter/obj
	rm -rf ./Refitter.Core/bin
	rm -rf ./Refitter.Core/obj
