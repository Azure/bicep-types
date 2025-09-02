package loader

import (
	"encoding/json"
	"fmt"
	"io"
	"os"
	"path/filepath"

	"github.com/Azure/bicep-types/src/bicep-types-go/index"
	"github.com/Azure/bicep-types/src/bicep-types-go/serialization"
	"github.com/Azure/bicep-types/src/bicep-types-go/types"
)

// ITypeLoader defines the interface for loading types and type indices
type ITypeLoader interface {
	LoadTypes(source io.Reader) ([]types.Type, error)
	LoadTypesFromFile(path string) ([]types.Type, error)
	LoadTypeIndex(source io.Reader) (*index.TypeIndex, error)
	LoadTypeIndexFromFile(path string) (*index.TypeIndex, error)
}

// TypeLoader implements the ITypeLoader interface
type TypeLoader struct {
	serializer *serialization.TypeSerializer
	basePath   string
}

// NewTypeLoader creates a new TypeLoader instance
func NewTypeLoader() *TypeLoader {
	return &TypeLoader{
		serializer: serialization.NewTypeSerializer(),
	}
}

// NewTypeLoaderWithBasePath creates a TypeLoader with a base path for resolving relative references
func NewTypeLoaderWithBasePath(basePath string) *TypeLoader {
	return &TypeLoader{
		serializer: serialization.NewTypeSerializer(),
		basePath:   basePath,
	}
}

// LoadTypes loads types from a reader
func (l *TypeLoader) LoadTypes(source io.Reader) ([]types.Type, error) {
	data, err := io.ReadAll(source)
	if err != nil {
		return nil, fmt.Errorf("failed to read types data: %w", err)
	}

	return l.serializer.DeserializeTypes(data)
}

// LoadTypesFromFile loads types from a file
func (l *TypeLoader) LoadTypesFromFile(path string) ([]types.Type, error) {
	file, err := os.Open(path)
	if err != nil {
		return nil, fmt.Errorf("failed to open types file: %w", err)
	}
	defer file.Close()

	// Set the current path for cross-file reference resolution
	// Note: This functionality would need to be implemented in TypeSerializer

	return l.LoadTypes(file)
}

// LoadTypeIndex loads a type index from a reader
func (l *TypeLoader) LoadTypeIndex(source io.Reader) (*index.TypeIndex, error) {
	data, err := io.ReadAll(source)
	if err != nil {
		return nil, fmt.Errorf("failed to read type index data: %w", err)
	}

	var idx index.TypeIndex
	if err := json.Unmarshal(data, &idx); err != nil {
		return nil, fmt.Errorf("failed to unmarshal type index: %w", err)
	}

	return &idx, nil
}

// LoadTypeIndexFromFile loads a type index from a file
func (l *TypeLoader) LoadTypeIndexFromFile(path string) (*index.TypeIndex, error) {
	file, err := os.Open(path)
	if err != nil {
		return nil, fmt.Errorf("failed to open type index file: %w", err)
	}
	defer file.Close()

	return l.LoadTypeIndex(file)
}

// TypeLoaderWithResolver extends TypeLoader with cross-file resolution capabilities
type TypeLoaderWithResolver struct {
	*TypeLoader
	loadedTypes map[string][]types.Type
}

// NewTypeLoaderWithResolver creates a new TypeLoaderWithResolver
func NewTypeLoaderWithResolver() *TypeLoaderWithResolver {
	return &TypeLoaderWithResolver{
		TypeLoader:  NewTypeLoader(),
		loadedTypes: make(map[string][]types.Type),
	}
}

// LoadTypesWithReferences loads types and resolves all cross-file references
func (l *TypeLoaderWithResolver) LoadTypesWithReferences(path string) ([]types.Type, error) {
	// Normalize the path
	path = filepath.Clean(path)

	// Check if already loaded
	if loadedTypes, exists := l.loadedTypes[path]; exists {
		return loadedTypes, nil
	}

	// Load the types
	loadedTypes, err := l.LoadTypesFromFile(path)
	if err != nil {
		return nil, err
	}

	// Store loaded types
	l.loadedTypes[path] = loadedTypes
	// Note: Type registration would need to be implemented

	// Find and load any cross-file references
	if err := l.loadCrossFileReferences(loadedTypes, path); err != nil {
		return nil, err
	}

	return loadedTypes, nil
}

// loadCrossFileReferences recursively loads types from cross-file references
func (l *TypeLoaderWithResolver) loadCrossFileReferences(typesList []types.Type, currentPath string) error {
	// Track unique paths to load
	pathsToLoad := make(map[string]bool)

	// Find all cross-file references
	for _, t := range typesList {
		l.findCrossFileReferencesInType(t, pathsToLoad)
	}

	// Load each referenced file
	currentDir := filepath.Dir(currentPath)
	for relativePath := range pathsToLoad {
		absolutePath := filepath.Join(currentDir, relativePath)
		absolutePath = filepath.Clean(absolutePath)

		// Skip if already loaded
		if _, exists := l.loadedTypes[absolutePath]; exists {
			continue
		}

		// Load the referenced types
		if _, err := l.LoadTypesWithReferences(absolutePath); err != nil {
			return fmt.Errorf("failed to load cross-file reference %s: %w", relativePath, err)
		}
	}

	return nil
}

// findCrossFileReferencesInType finds cross-file references in a type
func (l *TypeLoaderWithResolver) findCrossFileReferencesInType(t types.Type, paths map[string]bool) {
	// This would need to be implemented with type inspection
	// For now, we'll handle the most common cases

	switch v := t.(type) {
	case *types.ArrayType:
		if ref, ok := v.ItemType.(types.CrossFileTypeReference); ok {
			paths[ref.RelativePath] = true
		}
	case *types.UnionType:
		for _, elem := range v.Elements {
			if ref, ok := elem.(types.CrossFileTypeReference); ok {
				paths[ref.RelativePath] = true
			}
		}
	case *types.ObjectType:
		for _, prop := range v.Properties {
			if ref, ok := prop.Type.(types.CrossFileTypeReference); ok {
				paths[ref.RelativePath] = true
			}
		}
		if v.AdditionalProperties != nil {
			if ref, ok := v.AdditionalProperties.(types.CrossFileTypeReference); ok {
				paths[ref.RelativePath] = true
			}
		}
	case *types.DiscriminatedObjectType:
		for _, elem := range v.Elements {
			if ref, ok := elem.(types.CrossFileTypeReference); ok {
				paths[ref.RelativePath] = true
			}
		}
	case *types.ResourceType:
		if ref, ok := v.Body.(types.CrossFileTypeReference); ok {
			paths[ref.RelativePath] = true
		}
	case *types.FunctionType:
		if ref, ok := v.ReturnType.(types.CrossFileTypeReference); ok {
			paths[ref.RelativePath] = true
		}
		for _, param := range v.Parameters {
			if ref, ok := param.Type.(types.CrossFileTypeReference); ok {
				paths[ref.RelativePath] = true
			}
		}
	case *types.ResourceFunctionType:
		if ref, ok := v.ReturnType.(types.CrossFileTypeReference); ok {
			paths[ref.RelativePath] = true
		}
		for _, param := range v.Parameters {
			if ref, ok := param.Type.(types.CrossFileTypeReference); ok {
				paths[ref.RelativePath] = true
			}
		}
	}
}

// ResolveTypeReference resolves a type reference to its actual type
func (l *TypeLoaderWithResolver) ResolveTypeReference(ref types.ITypeReference) (types.Type, error) {
	// Note: This would need a proper implementation
	return nil, fmt.Errorf("type reference resolution not implemented")
}

// GetSerializerResolver returns the underlying serializer resolver for advanced usage
// Note: This is a placeholder - proper implementation needed
func (l *TypeLoaderWithResolver) GetSerializerResolver() interface{} {
	return nil
}
