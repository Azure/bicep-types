// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
package index

import (
	"encoding/json"
	"fmt"
	"strings"

	"github.com/Azure/bicep-types/src/bicep-types-go/types"
)

// TypeSettings represents settings for a type index
type TypeSettings struct {
	Name              string               `json:"name,omitempty"`
	IsSingleton       bool                 `json:"isSingleton,omitempty"`
	IsPreview         bool                 `json:"isPreview,omitempty"`
	IsDeprecated      bool                 `json:"isDeprecated,omitempty"`
	Version           string               `json:"version,omitempty"`
	ConfigurationType types.ITypeReference `json:"configurationType,omitempty"`
}

// UnmarshalJSON for TypeSettings handles the configurationType field
func (s *TypeSettings) UnmarshalJSON(data []byte) error {
	var temp struct {
		Name              string          `json:"name,omitempty"`
		IsSingleton       bool            `json:"isSingleton,omitempty"`
		IsPreview         bool            `json:"isPreview,omitempty"`
		IsDeprecated      bool            `json:"isDeprecated,omitempty"`
		Version           string          `json:"version,omitempty"`
		ConfigurationType json.RawMessage `json:"configurationType,omitempty"`
	}

	if err := json.Unmarshal(data, &temp); err != nil {
		return err
	}

	s.Name = temp.Name
	s.IsSingleton = temp.IsSingleton
	s.IsPreview = temp.IsPreview
	s.IsDeprecated = temp.IsDeprecated
	s.Version = temp.Version

	// Unmarshal configurationType if present
	if temp.ConfigurationType != nil {
		ref, err := unmarshalTypeReference(temp.ConfigurationType)
		if err != nil {
			return fmt.Errorf("failed to unmarshal configurationType: %w", err)
		}
		s.ConfigurationType = ref
	}

	return nil
}

// ResourceVersionMap maps API versions to type references
type ResourceVersionMap map[string]types.ITypeReference

// ResourceFunctionMap maps function names to type references
type ResourceFunctionMap map[string]types.ITypeReference

// ResourceFunctionVersionMap maps API versions to function maps
type ResourceFunctionVersionMap map[string]ResourceFunctionMap

// TypeIndex represents an index of types organized by resource type
type TypeIndex struct {
	Resources         map[string]ResourceVersionMap         `json:"resources,omitempty"`
	ResourceFunctions map[string]ResourceFunctionVersionMap `json:"resourceFunctions,omitempty"`
	FallbackResource  types.ITypeReference                  `json:"fallbackResourceType,omitempty"`
	Settings          *TypeSettings                         `json:"settings,omitempty"`
}

// NewTypeIndex creates a new empty TypeIndex
func NewTypeIndex() *TypeIndex {
	return &TypeIndex{
		Resources:         make(map[string]ResourceVersionMap),
		ResourceFunctions: make(map[string]ResourceFunctionVersionMap),
	}
}

// AddResource adds a resource type to the index
func (idx *TypeIndex) AddResource(resourceType string, apiVersion string, typeRef types.ITypeReference) {
	if idx.Resources == nil {
		idx.Resources = make(map[string]ResourceVersionMap)
	}

	if _, exists := idx.Resources[resourceType]; !exists {
		idx.Resources[resourceType] = make(ResourceVersionMap)
	}

	idx.Resources[resourceType][apiVersion] = typeRef
}

// AddResourceFunction adds a resource function to the index
func (idx *TypeIndex) AddResourceFunction(resourceType string, apiVersion string, functionName string, typeRef types.ITypeReference) {
	if idx.ResourceFunctions == nil {
		idx.ResourceFunctions = make(map[string]ResourceFunctionVersionMap)
	}

	if _, exists := idx.ResourceFunctions[resourceType]; !exists {
		idx.ResourceFunctions[resourceType] = make(ResourceFunctionVersionMap)
	}

	if _, exists := idx.ResourceFunctions[resourceType][apiVersion]; !exists {
		idx.ResourceFunctions[resourceType][apiVersion] = make(ResourceFunctionMap)
	}

	idx.ResourceFunctions[resourceType][apiVersion][functionName] = typeRef
}

// GetResource retrieves a resource type reference by type and API version
func (idx *TypeIndex) GetResource(resourceType string, apiVersion string) (types.ITypeReference, bool) {
	if idx.Resources == nil {
		return nil, false
	}

	versionMap, exists := idx.Resources[resourceType]
	if !exists {
		return nil, false
	}

	ref, exists := versionMap[apiVersion]
	return ref, exists
}

// GetResourceFunction retrieves a resource function reference
func (idx *TypeIndex) GetResourceFunction(resourceType string, apiVersion string, functionName string) (types.ITypeReference, bool) {
	if idx.ResourceFunctions == nil {
		return nil, false
	}

	versionMap, exists := idx.ResourceFunctions[resourceType]
	if !exists {
		return nil, false
	}

	functionMap, exists := versionMap[apiVersion]
	if !exists {
		return nil, false
	}

	ref, exists := functionMap[functionName]
	return ref, exists
}

// MarshalJSON implements custom JSON marshaling for TypeIndex
func (idx *TypeIndex) MarshalJSON() ([]byte, error) {
	// Flatten resources to "resourceType@version" format for external compatibility
	flattenedResources := make(map[string]types.ITypeReference)
	if idx.Resources != nil {
		for resourceType, versionMap := range idx.Resources {
			for version, ref := range versionMap {
				key := resourceType + "@" + version
				flattenedResources[key] = ref
			}
		}
	}

	// Create a temporary struct that matches the expected JSON structure
	temp := struct {
		Resources         map[string]types.ITypeReference       `json:"resources,omitempty"`
		ResourceFunctions map[string]ResourceFunctionVersionMap `json:"resourceFunctions"`
		FallbackResource  types.ITypeReference                  `json:"fallbackResourceType,omitempty"`
		Settings          *TypeSettings                         `json:"settings,omitempty"`
	}{
		Resources:         flattenedResources,
		ResourceFunctions: idx.ResourceFunctions,
		FallbackResource:  idx.FallbackResource,
		Settings:          idx.Settings,
	}

	return json.MarshalIndent(temp, "", "  ")
}

// UnmarshalJSON implements custom JSON unmarshaling for TypeIndex
func (idx *TypeIndex) UnmarshalJSON(data []byte) error {
	var temp struct {
		Resources         map[string]json.RawMessage                       `json:"resources,omitempty"`
		ResourceFunctions map[string]map[string]map[string]json.RawMessage `json:"resourceFunctions,omitempty"`
		FallbackResource  json.RawMessage                                  `json:"fallbackResourceType,omitempty"`
		Settings          *TypeSettings                                    `json:"settings,omitempty"`
	}

	if err := json.Unmarshal(data, &temp); err != nil {
		return err
	}

	// Initialize the TypeIndex
	idx.Resources = make(map[string]ResourceVersionMap)
	idx.ResourceFunctions = make(map[string]ResourceFunctionVersionMap)
	idx.Settings = temp.Settings

	// Unmarshal resources
	if temp.Resources != nil {
		for resourceTypeAndVersion, refData := range temp.Resources {
			// Parse "resourceType@version" format
			parts := strings.Split(resourceTypeAndVersion, "@")
			if len(parts) != 2 {
				return fmt.Errorf("invalid resource key format: %s", resourceTypeAndVersion)
			}

			resourceType := parts[0]
			version := parts[1]

			ref, err := unmarshalTypeReference(refData)
			if err != nil {
				return fmt.Errorf("failed to unmarshal resource reference for %s: %w", resourceTypeAndVersion, err)
			}

			if idx.Resources[resourceType] == nil {
				idx.Resources[resourceType] = make(ResourceVersionMap)
			}
			idx.Resources[resourceType][version] = ref
		}
	}

	// Unmarshal resource functions
	if temp.ResourceFunctions != nil {
		for resourceType, versionMap := range temp.ResourceFunctions {
			if versionMap != nil {
				idx.ResourceFunctions[resourceType] = make(ResourceFunctionVersionMap)
				for version, functionMap := range versionMap {
					if functionMap != nil {
						idx.ResourceFunctions[resourceType][version] = make(ResourceFunctionMap)
						for functionName, refData := range functionMap {
							ref, err := unmarshalTypeReference(refData)
							if err != nil {
								return err
							}
							idx.ResourceFunctions[resourceType][version][functionName] = ref
						}
					}
				}
			}
		}
	}

	// Unmarshal fallback resource if present
	if temp.FallbackResource != nil {
		ref, err := unmarshalTypeReference(temp.FallbackResource)
		if err != nil {
			return err
		}
		idx.FallbackResource = ref
	}

	return nil
}

// unmarshalTypeReference is a helper to unmarshal type references
func unmarshalTypeReference(data []byte) (types.ITypeReference, error) {
	// Check if it's a simple reference or cross-file reference
	var temp map[string]interface{}
	if err := json.Unmarshal(data, &temp); err != nil {
		return nil, err
	}

	// Check for explicit relativePath field
	if _, hasPath := temp["relativePath"]; hasPath {
		var ref types.CrossFileTypeReference
		if err := json.Unmarshal(data, &ref); err != nil {
			return nil, err
		}
		return ref, nil
	}

	// Check if $ref contains a filename (cross-file reference)
	if refValue, hasRef := temp["$ref"]; hasRef {
		if refStr, ok := refValue.(string); ok {
			// If it contains a filename before #/ it's a cross-file reference
			if strings.Contains(refStr, "#/") {
				parts := strings.Split(refStr, "#/")
				if len(parts) == 2 && parts[0] != "" {
					// It's a cross-file reference with filename
					var ref types.CrossFileTypeReference
					if err := json.Unmarshal(data, &ref); err != nil {
						return nil, err
					}
					return ref, nil
				}
			}
		}
	}

	// It's a regular TypeReference
	var ref types.TypeReference
	if err := json.Unmarshal(data, &ref); err != nil {
		return nil, err
	}
	return ref, nil
}
