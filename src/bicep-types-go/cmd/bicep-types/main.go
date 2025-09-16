// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
package main

import (
	"encoding/json"
	"flag"
	"fmt"
	"os"

	"github.com/Azure/bicep-types/src/bicep-types-go/factory"
	"github.com/Azure/bicep-types/src/bicep-types-go/loader"
	"github.com/Azure/bicep-types/src/bicep-types-go/types"
	"github.com/Azure/bicep-types/src/bicep-types-go/writers"
)

func main() {
	var (
		inputFile    = flag.String("input", "", "Input types JSON file")
		outputFile   = flag.String("output", "", "Output file (defaults to stdout)")
		outputFormat = flag.String("format", "json", "Output format: json or markdown")
		indentSize   = flag.Int("indent", 2, "JSON indentation size")
		showHelp     = flag.Bool("help", false, "Show help message")
	)
	flag.Parse()

	if *showHelp || *inputFile == "" {
		printUsage()
		return
	}

	// Load types from input file
	typeLoader := loader.NewTypeLoader()
	types, err := typeLoader.LoadTypesFromFile(*inputFile)
	if err != nil {
		fmt.Fprintf(os.Stderr, "Error loading types: %v\n", err)
		os.Exit(1)
	}

	// Determine output writer
	var output *os.File
	if *outputFile == "" {
		output = os.Stdout
	} else {
		output, err = os.Create(*outputFile)
		if err != nil {
			fmt.Fprintf(os.Stderr, "Error creating output file: %v\n", err)
			os.Exit(1)
		}
		defer func() {
			if closeErr := output.Close(); closeErr != nil {
				fmt.Fprintf(os.Stderr, "Error closing output file: %v\n", closeErr)
			}
		}()
	}

	// Generate output based on format
	switch *outputFormat {
	case "json":
		jsonWriter := writers.NewJSONWriterWithIndent(*indentSize)
		if err := jsonWriter.WriteTypes(output, types); err != nil {
			fmt.Fprintf(os.Stderr, "Error writing JSON: %v\n", err)
			os.Exit(1)
		}

	case "markdown", "md":
		markdownWriter := writers.NewMarkdownWriter()
		if err := markdownWriter.WriteTypes(output, types); err != nil {
			fmt.Fprintf(os.Stderr, "Error writing Markdown: %v\n", err)
			os.Exit(1)
		}

	default:
		fmt.Fprintf(os.Stderr, "Unsupported output format: %s\n", *outputFormat)
		os.Exit(1)
	}

	// Print summary to stderr if not writing to stdout
	if *outputFile != "" {
		fmt.Fprintf(os.Stderr, "Successfully processed %d types\n", len(types))
	}
}

func printUsage() {
	fmt.Println("bicep-types - Bicep type system tool")
	fmt.Println()
	fmt.Println("Usage:")
	fmt.Println("  bicep-types -input <file> [options]")
	fmt.Println()
	fmt.Println("Options:")
	fmt.Println("  -input <file>     Input types JSON file (required)")
	fmt.Println("  -output <file>    Output file (default: stdout)")
	fmt.Println("  -format <format>  Output format: json or markdown (default: json)")
	fmt.Println("  -indent <size>    JSON indentation size (default: 2)")
	fmt.Println("  -help             Show this help message")
	fmt.Println()
	fmt.Println("Examples:")
	fmt.Println("  bicep-types -input types.json")
	fmt.Println("  bicep-types -input types.json -format markdown -output types.md")
	fmt.Println("  bicep-types -input types.json -format json -output formatted.json -indent 4")
}

// Additional subcommands for more advanced functionality
func init() {
	// Check for subcommands
	if len(os.Args) > 1 {
		switch os.Args[1] {
		case "validate":
			validateCommand()
			return
		case "generate":
			generateCommand()
			return
		case "version":
			fmt.Println("bicep-types version 1.0.0")
			return
		}
	}
}

func validateCommand() {
	if len(os.Args) < 3 {
		fmt.Fprintf(os.Stderr, "Usage: bicep-types validate <file>\n")
		os.Exit(1)
	}

	filename := os.Args[2]
	typeLoader := loader.NewTypeLoader()

	types, err := typeLoader.LoadTypesFromFile(filename)
	if err != nil {
		fmt.Fprintf(os.Stderr, "Validation failed: %v\n", err)
		os.Exit(1)
	}

	fmt.Printf("✓ File %s is valid\n", filename)
	fmt.Printf("✓ Loaded %d types successfully\n", len(types))

	// Validate each type can be marshaled back to JSON
	for i, t := range types {
		_, err := json.Marshal(t)
		if err != nil {
			fmt.Fprintf(os.Stderr, "Type at index %d failed JSON marshaling: %v\n", i, err)
			os.Exit(1)
		}
	}

	fmt.Println("✓ All types can be serialized back to JSON")
}

func generateCommand() {
	fmt.Println("Generate command - creates sample types for testing")

	// Create a sample type factory
	f := factory.NewTypeFactory()

	// Create some sample types
	stringType := f.CreateStringType()
	intType := f.CreateIntegerTypeWithConstraints(int64Ptr(0), int64Ptr(100))
	boolType := f.CreateBooleanType()

	// Create an object type
	objectType := f.CreateObjectType("SampleObject", nil, nil, nil)
	objectType.Properties = map[string]types.ObjectTypeProperty{
		"name": f.CreateRequiredStringProperty("The name of the object"),
		"count": {
			Type:        f.GetReference(intType),
			Description: "The count value",
		},
		"enabled": {
			Type:        f.GetReference(boolType),
			Description: "Whether the object is enabled",
		},
	}

	// Create a resource type
	resourceType := f.CreateResourceType(
		"Microsoft.Sample/resources@2023-01-01",     // Full resource name with API version
		f.GetReference(objectType),                  // Body type reference
		types.AllExceptExtension,                    // ReadableScopes
		types.AllExceptExtension,                    // WritableScopes
		make(map[string]types.ResourceTypeFunction), // Functions (empty map)
	)

	// Get all types
	f.GetReference(stringType)
	f.GetReference(intType)
	f.GetReference(boolType)
	f.GetReference(objectType)
	f.GetReference(resourceType)

	allTypes := f.GetTypes()

	// Output as JSON
	jsonWriter := writers.NewJSONWriter()
	if err := jsonWriter.WriteTypes(os.Stdout, allTypes); err != nil {
		fmt.Fprintf(os.Stderr, "Error generating sample types: %v\n", err)
		os.Exit(1)
	}
}

func int64Ptr(v int64) *int64 {
	return &v
}
