# Go parameters
GOCMD=go
GOBUILD=$(GOCMD) build
GOCLEAN=$(GOCMD) clean
GOTEST=$(GOCMD) test
GOGET=$(GOCMD) get
GOMOD=$(GOCMD) mod
GOFMT=$(GOCMD) fmt

# Build parameters
BINARY_NAME=bicep-types
BINARY_PATH=./cmd/bicep-types
BUILD_DIR=./bin

# Test parameters
TEST_PACKAGES=./...
TEST_FLAGS=-v -race -cover

.PHONY: all build test clean fmt vet tidy deps help

# Default target
all: clean fmt vet test build

# Build the binary
build:
	@echo "Building $(BINARY_NAME)..."
	@mkdir -p $(BUILD_DIR)
	$(GOBUILD) -o $(BUILD_DIR)/$(BINARY_NAME) $(BINARY_PATH)
	@echo "Built $(BUILD_DIR)/$(BINARY_NAME)"

# Run all tests
test:
	@echo "Running tests..."
	$(GOTEST) $(TEST_FLAGS) $(TEST_PACKAGES)

# Run tests with coverage report
test-coverage:
	@echo "Running tests with coverage..."
	$(GOTEST) -v -race -coverprofile=coverage.out $(TEST_PACKAGES)
	$(GOCMD) tool cover -html=coverage.out -o coverage.html
	@echo "Coverage report generated: coverage.html"

# Run specific test
test-verbose:
	@echo "Running tests with verbose output..."
	$(GOTEST) -v $(TEST_PACKAGES)

# Clean build artifacts
clean:
	@echo "Cleaning..."
	$(GOCLEAN)
	@rm -rf $(BUILD_DIR)
	@rm -f coverage.out coverage.html

# Format Go code
fmt:
	@echo "Formatting code..."
	$(GOFMT) ./...

# Run linter (requires golangci-lint)
lint:
	@echo "Running linter..."
	@which golangci-lint > /dev/null || (echo "golangci-lint not found, install with: go install github.com/golangci/golangci-lint/cmd/golangci-lint@latest" && exit 1)
	golangci-lint run

# Install the binary
install:
	@echo "Installing $(BINARY_NAME)..."
	$(GOCMD) install $(BINARY_PATH)

# Generate code (if you have code generation)
generate:
	@echo "Generating code..."
	$(GOCMD) generate $(TEST_PACKAGES)


# Run all quality checks
check: fmt vet lint test

# Help
help:
	@echo "Available targets:"
	@echo "  all           - Run clean, fmt, vet, test, and build"
	@echo "  build         - Build the binary"
	@echo "  test          - Run all tests"
	@echo "  test-coverage - Run tests with coverage report"
	@echo "  test-verbose  - Run tests with verbose output"
	@echo "  clean         - Clean build artifacts"
	@echo "  fmt           - Format Go code"
	@echo "  vet           - Run go vet"
	@echo "  tidy          - Tidy dependencies"
	@echo "  deps          - Download dependencies"
	@echo "  lint          - Run linter (requires golangci-lint)"
	@echo "  install       - Install the binary"
	@echo "  dev           - Start development server (requires air)"
	@echo "  generate      - Generate code"
	@echo "  bench         - Run benchmark tests"
	@echo "  check         - Run all quality checks"
	@echo "  help          - Show this help"