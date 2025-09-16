// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
package serialization

import (
	"encoding/json"
	"fmt"
	"path/filepath"
	"strings"

	"github.com/Azure/bicep-types/src/bicep-types-go/types"
)

// TypeReferenceResolver handles resolution of type references during deserialization
type TypeReferenceResolver struct {
	types       []types.Type
	currentPath string
	typesByPath map[string][]types.Type
}

// NewTypeReferenceResolver creates a new resolver
func NewTypeReferenceResolver() *TypeReferenceResolver {
	return &TypeReferenceResolver{
		types:       make([]types.Type, 0),
		typesByPath: make(map[string][]types.Type),
	}
}

// SetCurrentPath sets the current file path for resolving relative references
func (r *TypeReferenceResolver) SetCurrentPath(path string) {
	r.currentPath = path
}

// RegisterTypes registers types for a given path
func (r *TypeReferenceResolver) RegisterTypes(path string, types []types.Type) {
	r.typesByPath[path] = types
}

// ResolveReference resolves a type reference to its actual type
func (r *TypeReferenceResolver) ResolveReference(ref types.ITypeReference) (types.Type, error) {
	switch v := ref.(type) {
	case types.TypeReference:
		if v.Ref < 0 || v.Ref >= len(r.types) {
			return nil, fmt.Errorf("invalid type reference index: %d", v.Ref)
		}
		return r.types[v.Ref], nil

	case types.CrossFileTypeReference:
		targetPath := r.resolvePath(v.RelativePath)
		targetTypes, exists := r.typesByPath[targetPath]
		if !exists {
			return nil, fmt.Errorf("no types found for path: %s", targetPath)
		}
		if v.Ref < 0 || v.Ref >= len(targetTypes) {
			return nil, fmt.Errorf("invalid cross-file type reference index: %d for path %s", v.Ref, targetPath)
		}
		return targetTypes[v.Ref], nil

	default:
		return nil, fmt.Errorf("unknown type reference type: %T", ref)
	}
}

// resolvePath resolves a relative path based on the current path
func (r *TypeReferenceResolver) resolvePath(relativePath string) string {
	if r.currentPath == "" {
		return relativePath
	}

	// Normalize path separators
	relativePath = strings.ReplaceAll(relativePath, "\\", "/")

	// If it's already absolute, return as is
	if filepath.IsAbs(relativePath) {
		return relativePath
	}

	// Resolve relative to current path directory
	dir := filepath.Dir(r.currentPath)
	resolved := filepath.Join(dir, relativePath)

	// Clean and normalize the path
	return filepath.Clean(resolved)
}

// UnmarshalTypeReference deserializes a type reference from JSON
func UnmarshalTypeReference(data []byte) (types.ITypeReference, error) {
	// Check if it's a simple reference or cross-file reference
	var temp map[string]interface{}
	if err := json.Unmarshal(data, &temp); err != nil {
		return nil, err
	}

	if relativePath, hasPath := temp["relativePath"]; hasPath {
		// It's a CrossFileTypeReference
		var ref types.CrossFileTypeReference
		if err := json.Unmarshal(data, &ref); err != nil {
			return nil, err
		}
		// Ensure relativePath is a string
		if pathStr, ok := relativePath.(string); ok {
			ref.RelativePath = pathStr
		}
		return ref, nil
	}

	// It's a regular TypeReference
	var ref types.TypeReference
	if err := json.Unmarshal(data, &ref); err != nil {
		return nil, err
	}
	return ref, nil
}

// TypeSerializer handles serialization and deserialization of types
type TypeSerializer struct {
	resolver *TypeReferenceResolver
}

// NewTypeSerializer creates a new TypeSerializer
func NewTypeSerializer() *TypeSerializer {
	return &TypeSerializer{
		resolver: NewTypeReferenceResolver(),
	}
}

// SerializeTypes serializes a slice of types to JSON
func (s *TypeSerializer) SerializeTypes(types []types.Type) ([]byte, error) {
	return json.MarshalIndent(types, "", "  ")
}

// DeserializeTypes deserializes types from JSON
func (s *TypeSerializer) DeserializeTypes(data []byte) ([]types.Type, error) {
	var rawTypes []json.RawMessage
	if err := json.Unmarshal(data, &rawTypes); err != nil {
		return nil, fmt.Errorf("failed to unmarshal types array: %w", err)
	}

	result := make([]types.Type, 0, len(rawTypes))
	for i, raw := range rawTypes {
		t, err := types.UnmarshalType(raw)
		if err != nil {
			return nil, fmt.Errorf("failed to unmarshal type at index %d: %w", i, err)
		}
		result = append(result, t)
	}

	// Store types for reference resolution
	s.resolver.types = result

	return result, nil
}

// DeserializeTypeWithReferences deserializes a type and resolves all references
func (s *TypeSerializer) DeserializeTypeWithReferences(data []byte, allTypes []types.Type) (types.Type, error) {
	// First deserialize the type
	t, err := types.UnmarshalType(data)
	if err != nil {
		return nil, err
	}

	// Store types for reference resolution
	s.resolver.types = allTypes

	// The type itself may contain references that need resolution
	// This would be handled by a separate pass or lazy resolution

	return t, nil
}

// NormalizePath normalizes a file path for cross-platform compatibility
func NormalizePath(path string) string {
	// Convert backslashes to forward slashes
	normalized := strings.ReplaceAll(path, "\\", "/")

	// Clean the path
	normalized = filepath.Clean(normalized)

	// Ensure forward slashes
	normalized = strings.ReplaceAll(normalized, "\\", "/")

	return normalized
}

// GetRelativePath calculates the relative path from source to target
func GetRelativePath(source, target string) (string, error) {
	// Normalize both paths
	source = NormalizePath(source)
	target = NormalizePath(target)

	// Get relative path
	rel, err := filepath.Rel(filepath.Dir(source), target)
	if err != nil {
		return "", err
	}

	// Ensure forward slashes
	rel = strings.ReplaceAll(rel, "\\", "/")

	return rel, nil
}
