// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
package baseline

import (
	"encoding/json"
	"fmt"
	"io/fs"
	"os"
	"path/filepath"
	"strings"

	"github.com/Azure/bicep-types/src/bicep-types-go/index"
	"github.com/Azure/bicep-types/src/bicep-types-go/loader"
	"github.com/Azure/bicep-types/src/bicep-types-go/types"
)

// BaselineTypeLoader provides functionality to load baseline test data
type BaselineTypeLoader struct {
	loader        *loader.TypeLoaderWithResolver
	baselinesPath string
}

// NewBaselineTypeLoader creates a new baseline type loader
func NewBaselineTypeLoader() *BaselineTypeLoader {
	// Default to the bicep-types baseline path
	baselinesPath := "../../../bicep-types/test/integration/baselines"
	return &BaselineTypeLoader{
		loader:        loader.NewTypeLoaderWithResolver(),
		baselinesPath: baselinesPath,
	}
}

// NewBaselineTypeLoaderWithPath creates a new baseline type loader with custom path
func NewBaselineTypeLoaderWithPath(baselinesPath string) *BaselineTypeLoader {
	return &BaselineTypeLoader{
		loader:        loader.NewTypeLoaderWithResolver(),
		baselinesPath: baselinesPath,
	}
}

// GetBaselineNames returns all available baseline test names
func (b *BaselineTypeLoader) GetBaselineNames() ([]string, error) {
	entries, err := os.ReadDir(b.baselinesPath)
	if err != nil {
		return nil, fmt.Errorf("failed to read baselines directory %s: %w", b.baselinesPath, err)
	}

	var names []string
	for _, entry := range entries {
		if entry.IsDir() {
			names = append(names, entry.Name())
		}
	}

	return names, nil
}

// LoadBaselineTypes loads types from a baseline test
func (b *BaselineTypeLoader) LoadBaselineTypes(baselineName string) ([]types.Type, error) {
	typesPath := filepath.Join(b.baselinesPath, baselineName, "types.json")

	data, err := os.ReadFile(typesPath)
	if err != nil {
		// If direct types.json doesn't exist, return empty slice
		// This handles cases like http baseline that only has nested types
		return []types.Type{}, nil
	}

	var rawTypes []json.RawMessage
	if err := json.Unmarshal(data, &rawTypes); err != nil {
		return nil, fmt.Errorf("failed to unmarshal types array: %w", err)
	}

	result := make([]types.Type, 0, len(rawTypes))
	for i, raw := range rawTypes {
		t, err := types.UnmarshalType(raw)
		if err != nil {
			return nil, fmt.Errorf("failed to unmarshal type at index %d in baseline %s: %w", i, baselineName, err)
		}
		result = append(result, t)
	}

	return result, nil
}

// LoadBaselineIndex loads a type index from a baseline test
func (b *BaselineTypeLoader) LoadBaselineIndex(baselineName string) (*index.TypeIndex, error) {
	indexPath := filepath.Join(b.baselinesPath, baselineName, "index.json")

	data, err := os.ReadFile(indexPath)
	if err != nil {
		return nil, fmt.Errorf("failed to read baseline index file %s: %w", indexPath, err)
	}

	var idx index.TypeIndex
	if err := json.Unmarshal(data, &idx); err != nil {
		return nil, fmt.Errorf("failed to unmarshal type index: %w", err)
	}

	return &idx, nil
}

// LoadBaselineMarkdown loads markdown content from a baseline test
func (b *BaselineTypeLoader) LoadBaselineMarkdown(baselineName string, fileName string) (string, error) {
	markdownPath := filepath.Join(b.baselinesPath, baselineName, fileName)

	data, err := os.ReadFile(markdownPath)
	if err != nil {
		return "", fmt.Errorf("failed to read baseline markdown file %s: %w", markdownPath, err)
	}

	return string(data), nil
}

// GetAllBaselineFiles returns all files in a baseline directory
func (b *BaselineTypeLoader) GetAllBaselineFiles(baselineName string) ([]string, error) {
	baselineDir := filepath.Join(b.baselinesPath, baselineName)

	var files []string
	err := filepath.WalkDir(baselineDir, func(path string, d fs.DirEntry, err error) error {
		if err != nil {
			return err
		}

		if !d.IsDir() {
			// Get relative path from baseline directory
			relativePath, relErr := filepath.Rel(baselineDir, path)
			if relErr != nil {
				return relErr
			}
			files = append(files, relativePath)
		}

		return nil
	})

	if err != nil {
		return nil, fmt.Errorf("failed to walk baseline directory %s: %w", baselineDir, err)
	}

	return files, nil
}

// ValidateBaseline validates that a baseline can be loaded successfully
func (b *BaselineTypeLoader) ValidateBaseline(baselineName string) error {
	// Try to load types
	_, err := b.LoadBaselineTypes(baselineName)
	if err != nil {
		return fmt.Errorf("failed to load baseline types: %w", err)
	}

	// Try to load index
	_, err = b.LoadBaselineIndex(baselineName)
	if err != nil {
		return fmt.Errorf("failed to load baseline index: %w", err)
	}

	return nil
}

// LoadBaselineWithCrossFileReferences loads a baseline and resolves cross-file references
func (b *BaselineTypeLoader) LoadBaselineWithCrossFileReferences(baselineName string) ([]types.Type, *index.TypeIndex, error) {
	// Load the main types
	mainTypes, err := b.LoadBaselineTypes(baselineName)
	if err != nil {
		return nil, nil, err
	}

	// Load the index
	idx, err := b.LoadBaselineIndex(baselineName)
	if err != nil {
		return nil, nil, err
	}

	// Load any additional types files that might be referenced
	additionalTypes := make(map[string][]types.Type)

	// Check for nested directories with types.json
	baselineDir := filepath.Join(b.baselinesPath, baselineName)
	err = filepath.WalkDir(baselineDir, func(path string, d fs.DirEntry, err error) error {
		if err != nil {
			return err
		}

		if !d.IsDir() && d.Name() == "types.json" && path != filepath.Join(baselineDir, "types.json") {
			// This is an additional types file
			data, readErr := os.ReadFile(path)
			if readErr != nil {
				return readErr
			}

			var rawTypes []json.RawMessage
			if unmarshalErr := json.Unmarshal(data, &rawTypes); unmarshalErr != nil {
				return unmarshalErr
			}

			var typesList []types.Type
			for i, raw := range rawTypes {
				t, typeErr := types.UnmarshalType(raw)
				if typeErr != nil {
					return fmt.Errorf("failed to unmarshal type at index %d in file %s: %w", i, path, typeErr)
				}
				typesList = append(typesList, t)
			}

			// Store with relative path from baseline directory
			relativePath, relErr := filepath.Rel(baselineDir, path)
			if relErr != nil {
				return relErr
			}
			additionalTypes[relativePath] = typesList
		}

		return nil
	})

	if err != nil {
		return nil, nil, fmt.Errorf("failed to load cross-file references for baseline %s: %w", baselineName, err)
	}

	// Register all types with the resolver
	// Note: Type registration would need to be implemented when resolver is ready
	_ = mainTypes       // Avoid unused variable error
	_ = additionalTypes // Avoid unused variable error

	return mainTypes, idx, nil
}

// BaselineTestCase represents a single baseline test case
type BaselineTestCase struct {
	Name  string
	Types []types.Type
	Index *index.TypeIndex
	Files map[string]string
}

// GetAllBaselineTestCases returns all baseline test cases
func (b *BaselineTypeLoader) GetAllBaselineTestCases() ([]BaselineTestCase, error) {
	names, err := b.GetBaselineNames()
	if err != nil {
		return nil, err
	}

	var testCases []BaselineTestCase
	for _, name := range names {
		types, idx, err := b.LoadBaselineWithCrossFileReferences(name)
		if err != nil {
			return nil, fmt.Errorf("failed to load baseline %s: %w", name, err)
		}

		// Load all files for this baseline
		files := make(map[string]string)
		fileNames, err := b.GetAllBaselineFiles(name)
		if err != nil {
			return nil, fmt.Errorf("failed to get files for baseline %s: %w", name, err)
		}

		for _, fileName := range fileNames {
			if strings.HasSuffix(fileName, ".md") {
				content, err := b.LoadBaselineMarkdown(name, fileName)
				if err != nil {
					return nil, fmt.Errorf("failed to load markdown file %s for baseline %s: %w", fileName, name, err)
				}
				files[fileName] = content
			}
		}

		testCases = append(testCases, BaselineTestCase{
			Name:  name,
			Types: types,
			Index: idx,
			Files: files,
		})
	}

	return testCases, nil
}
