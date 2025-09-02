package types

import (
	"encoding/json"
	"fmt"
	"strconv"
	"strings"
)

// ITypeReference represents a reference to a type, either within the same file or across files
type ITypeReference interface {
	isTypeReference()
}

// Type represents the base interface for all types in the Bicep type system
type Type interface {
	// Type returns the discriminator value for JSON serialization
	Type() string
	// MarshalJSON implements custom JSON marshaling
	MarshalJSON() ([]byte, error)
}

// TypeReference represents a reference to a type within the same file
type TypeReference struct {
	Ref int `json:"-"`
}

func (TypeReference) isTypeReference() {}

// CrossFileTypeReference represents a reference to a type in another file
type CrossFileTypeReference struct {
	Ref              int    `json:"-"`
	RelativePath     string `json:"relativePath"`
}

func (CrossFileTypeReference) isTypeReference() {}

// ScopeType represents the scope of a resource
type ScopeType int

const (
	ScopeTypeUnknown ScopeType = iota
	ScopeTypeTenant
	ScopeTypeManagementGroup
	ScopeTypeSubscription
	ScopeTypeResourceGroup
	ScopeTypeExtension
)

// TypePropertyFlags represents flags for type properties
type TypePropertyFlags int

const (
	TypePropertyFlagsNone        TypePropertyFlags = 0
	TypePropertyFlagsRequired    TypePropertyFlags = 1 << 0
	TypePropertyFlagsReadOnly    TypePropertyFlags = 1 << 1
	TypePropertyFlagsWriteOnly   TypePropertyFlags = 1 << 2
	TypePropertyFlagsDeployTime  TypePropertyFlags = 1 << 3
	TypePropertyFlagsConstant    TypePropertyFlags = 1 << 4
	TypePropertyFlagsNested      TypePropertyFlags = 1 << 5
	TypePropertyFlagsIdentifier  TypePropertyFlags = 1 << 6
)

// ObjectTypePropertyFlags represents flags specific to object type properties
type ObjectTypePropertyFlags int

const (
	ObjectTypePropertyFlagsNone         ObjectTypePropertyFlags = 0
	ObjectTypePropertyFlagsSystemProperty ObjectTypePropertyFlags = 1 << 0
)

// TypeFactory provides methods to create and manage types
type TypeFactory struct {
	types []Type
	index map[Type]int
}

// NewTypeFactory creates a new TypeFactory instance
func NewTypeFactory() *TypeFactory {
	return &TypeFactory{
		types: make([]Type, 0),
		index: make(map[Type]int),
	}
}

// GetReference returns a TypeReference for the given type
func (f *TypeFactory) GetReference(t Type) ITypeReference {
	if idx, exists := f.index[t]; exists {
		return TypeReference{Ref: idx}
	}
	
	idx := len(f.types)
	f.types = append(f.types, t)
	f.index[t] = idx
	
	return TypeReference{Ref: idx}
}

// GetTypes returns all registered types
func (f *TypeFactory) GetTypes() []Type {
	return f.types
}

// MarshalJSON implementations for references
func (r TypeReference) MarshalJSON() ([]byte, error) {
	return json.Marshal(map[string]interface{}{
		"$ref": fmt.Sprintf("#/%d", r.Ref),
	})
}

// UnmarshalJSON for TypeReference handles JSON Pointer format
func (r *TypeReference) UnmarshalJSON(data []byte) error {
	var temp struct {
		Ref string `json:"$ref"`
	}
	
	if err := json.Unmarshal(data, &temp); err != nil {
		return err
	}
	
	// Parse JSON Pointer format "#/0" -> 0
	if strings.HasPrefix(temp.Ref, "#/") {
		refStr := strings.TrimPrefix(temp.Ref, "#/")
		refInt, err := strconv.Atoi(refStr)
		if err != nil {
			return fmt.Errorf("invalid type reference format: %s", temp.Ref)
		}
		r.Ref = refInt
		return nil
	}
	
	// Also support direct integer format for backward compatibility
	refInt, err := strconv.Atoi(temp.Ref)
	if err != nil {
		return fmt.Errorf("invalid type reference format: %s", temp.Ref)
	}
	r.Ref = refInt
	return nil
}

func (r CrossFileTypeReference) MarshalJSON() ([]byte, error) {
	// Use the format expected by rad-bicep: "filename#/index"
	refString := fmt.Sprintf("%s#/%d", r.RelativePath, r.Ref)
	return json.Marshal(map[string]interface{}{
		"$ref": refString,
	})
}

// UnmarshalJSON for CrossFileTypeReference handles JSON Pointer format
func (r *CrossFileTypeReference) UnmarshalJSON(data []byte) error {
	var temp struct {
		Ref          string `json:"$ref"`
		RelativePath string `json:"relativePath"`
	}
	
	if err := json.Unmarshal(data, &temp); err != nil {
		return err
	}
	
	// Parse different formats:
	// 1. "filename.json#/0" -> extract filename and index
	// 2. "#/0" -> extract index only
	// 3. "0" -> direct integer
	
	if strings.Contains(temp.Ref, "#/") {
		parts := strings.Split(temp.Ref, "#/")
		if len(parts) != 2 {
			return fmt.Errorf("invalid cross-file type reference format: %s", temp.Ref)
		}
		
		// If there's a filename before #/, use it as relativePath if not already set
		if parts[0] != "" && temp.RelativePath == "" {
			r.RelativePath = parts[0]
		} else {
			r.RelativePath = temp.RelativePath
		}
		
		refInt, err := strconv.Atoi(parts[1])
		if err != nil {
			return fmt.Errorf("invalid cross-file type reference index: %s", parts[1])
		}
		r.Ref = refInt
	} else {
		// Direct integer format
		refInt, err := strconv.Atoi(temp.Ref)
		if err != nil {
			return fmt.Errorf("invalid cross-file type reference format: %s", temp.Ref)
		}
		r.Ref = refInt
		r.RelativePath = temp.RelativePath
	}
	
	return nil
}

// Helper function for marshaling types with proper discrimination
func marshalTypeWithDiscriminator(typeName string, v interface{}) ([]byte, error) {
	// Create a map to hold all fields
	m := make(map[string]interface{})
	
	// Marshal the struct to get its fields
	temp, err := json.Marshal(v)
	if err != nil {
		return nil, err
	}
	
	// Unmarshal into the map
	if err := json.Unmarshal(temp, &m); err != nil {
		return nil, err
	}
	
	// Add the $type discriminator
	m["$type"] = typeName
	
	// Marshal the final result
	return json.Marshal(m)
}

// UnmarshalType deserializes a Type from JSON with polymorphic handling
func UnmarshalType(data []byte) (Type, error) {
	var discriminator struct {
		Type string `json:"$type"`
	}
	
	if err := json.Unmarshal(data, &discriminator); err != nil {
		return nil, fmt.Errorf("failed to unmarshal type discriminator: %w", err)
	}
	
	switch discriminator.Type {
	case "StringType":
		var t StringType
		if err := json.Unmarshal(data, &t); err != nil {
			return nil, err
		}
		return &t, nil
	case "IntegerType":
		var t IntegerType
		if err := json.Unmarshal(data, &t); err != nil {
			return nil, err
		}
		return &t, nil
	case "BooleanType":
		var t BooleanType
		if err := json.Unmarshal(data, &t); err != nil {
			return nil, err
		}
		return &t, nil
	case "NullType":
		var t NullType
		if err := json.Unmarshal(data, &t); err != nil {
			return nil, err
		}
		return &t, nil
	case "AnyType":
		var t AnyType
		if err := json.Unmarshal(data, &t); err != nil {
			return nil, err
		}
		return &t, nil
	case "StringLiteralType":
		var t StringLiteralType
		if err := json.Unmarshal(data, &t); err != nil {
			return nil, err
		}
		return &t, nil
	case "BuiltInType":
		var t BuiltInType
		if err := json.Unmarshal(data, &t); err != nil {
			return nil, err
		}
		return &t, nil
	case "ArrayType":
		var t ArrayType
		if err := json.Unmarshal(data, &t); err != nil {
			return nil, err
		}
		return &t, nil
	case "UnionType":
		var t UnionType
		if err := json.Unmarshal(data, &t); err != nil {
			return nil, err
		}
		return &t, nil
	case "ObjectType":
		var t ObjectType
		if err := json.Unmarshal(data, &t); err != nil {
			return nil, err
		}
		return &t, nil
	case "DiscriminatedObjectType":
		var t DiscriminatedObjectType
		if err := json.Unmarshal(data, &t); err != nil {
			return nil, err
		}
		return &t, nil
	case "ResourceType":
		var t ResourceType
		if err := json.Unmarshal(data, &t); err != nil {
			return nil, err
		}
		return &t, nil
	case "FunctionType":
		var t FunctionType
		if err := json.Unmarshal(data, &t); err != nil {
			return nil, err
		}
		return &t, nil
	case "ResourceFunctionType":
		var t ResourceFunctionType
		if err := json.Unmarshal(data, &t); err != nil {
			return nil, err
		}
		return &t, nil
	default:
		return nil, fmt.Errorf("unknown type discriminator: %s", discriminator.Type)
	}
}

// unmarshalTypeReference is a helper to unmarshal type references
func unmarshalTypeReference(data []byte) (ITypeReference, error) {
	// Check if it's a simple reference or cross-file reference
	var temp map[string]interface{}
	if err := json.Unmarshal(data, &temp); err != nil {
		return nil, err
	}
	
	// Check for explicit relativePath field
	if _, hasPath := temp["relativePath"]; hasPath {
		var ref CrossFileTypeReference
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
					var ref CrossFileTypeReference
					if err := json.Unmarshal(data, &ref); err != nil {
						return nil, err
					}
					return ref, nil
				}
			}
		}
	}
	
	// It's a regular TypeReference
	var ref TypeReference
	if err := json.Unmarshal(data, &ref); err != nil {
		return nil, err
	}
	return ref, nil
}