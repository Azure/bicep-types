package types

import (
	"encoding/json"
	"fmt"
)

// ObjectTypeProperty represents a property of an object type
type ObjectTypeProperty struct {
	Type        ITypeReference           `json:"type"`
	Flags       TypePropertyFlags        `json:"flags,omitempty"`
	Description string                   `json:"description,omitempty"`
	Metadata    map[string]interface{}   `json:"metadata,omitempty"`
}

// ObjectType represents an object type with properties
type ObjectType struct {
	Name                 string                          `json:"name,omitempty"`
	Properties           map[string]ObjectTypeProperty   `json:"properties,omitempty"`
	AdditionalProperties ITypeReference                  `json:"additionalProperties,omitempty"`
	Flags                ObjectTypePropertyFlags         `json:"flags,omitempty"`
}

func (*ObjectType) Type() string { return "ObjectType" }

func (t *ObjectType) MarshalJSON() ([]byte, error) {
	return marshalTypeWithDiscriminator(t.Type(), struct {
		Name                 string                          `json:"name,omitempty"`
		Properties           map[string]ObjectTypeProperty   `json:"properties,omitempty"`
		AdditionalProperties ITypeReference                  `json:"additionalProperties,omitempty"`
		Flags                ObjectTypePropertyFlags         `json:"flags,omitempty"`
	}{
		Name:                 t.Name,
		Properties:           t.Properties,
		AdditionalProperties: t.AdditionalProperties,
		Flags:                t.Flags,
	})
}

// DiscriminatedObjectType represents an object type with a discriminator property
type DiscriminatedObjectType struct {
	Name          string                             `json:"name,omitempty"`
	Discriminator string                             `json:"discriminator"`
	Elements      map[string]ITypeReference          `json:"elements"`
}

func (*DiscriminatedObjectType) Type() string { return "DiscriminatedObjectType" }

func (t *DiscriminatedObjectType) MarshalJSON() ([]byte, error) {
	return marshalTypeWithDiscriminator(t.Type(), struct {
		Name          string                    `json:"name,omitempty"`
		Discriminator string                    `json:"discriminator"`
		Elements      map[string]ITypeReference `json:"elements"`
	}{
		Name:          t.Name,
		Discriminator: t.Discriminator,
		Elements:      t.Elements,
	})
}

// ResourceType represents an Azure resource type
type ResourceType struct {
	Name           string                          `json:"name"`
	ResourceTypeID string                          `json:"resourceTypeId"`
	APIVersion     string                          `json:"apiVersion"`
	ReadOnlyAPIVersion bool                       `json:"readOnlyApiVersion,omitempty"`
	Providers      []string                        `json:"providers,omitempty"`
	LocationRequired bool                         `json:"locationRequired,omitempty"`
	ZoneRequired   bool                            `json:"zoneRequired,omitempty"`
	Body           ITypeReference                  `json:"body"`
	Metadata       map[string]interface{}          `json:"metadata,omitempty"`
	Description    string                          `json:"description,omitempty"`
	IsSingleton    bool                            `json:"isSingleton,omitempty"`
	ScopeTypes     []ScopeType                     `json:"scopeTypes,omitempty"`
}

func (*ResourceType) Type() string { return "ResourceType" }

func (t *ResourceType) MarshalJSON() ([]byte, error) {
	return marshalTypeWithDiscriminator(t.Type(), struct {
		Name               string                 `json:"name"`
		ResourceTypeID     string                 `json:"resourceTypeId"`
		APIVersion         string                 `json:"apiVersion"`
		ReadOnlyAPIVersion bool                   `json:"readOnlyApiVersion,omitempty"`
		Providers          []string               `json:"providers,omitempty"`
		LocationRequired   bool                   `json:"locationRequired,omitempty"`
		ZoneRequired       bool                   `json:"zoneRequired,omitempty"`
		Body               ITypeReference         `json:"body"`
		Metadata           map[string]interface{} `json:"metadata,omitempty"`
		Description        string                 `json:"description,omitempty"`
		IsSingleton        bool                   `json:"isSingleton,omitempty"`
		ScopeTypes         []ScopeType            `json:"scopeTypes,omitempty"`
	}{
		Name:               t.Name,
		ResourceTypeID:     t.ResourceTypeID,
		APIVersion:         t.APIVersion,
		ReadOnlyAPIVersion: t.ReadOnlyAPIVersion,
		Providers:          t.Providers,
		LocationRequired:   t.LocationRequired,
		ZoneRequired:       t.ZoneRequired,
		Body:               t.Body,
		Metadata:           t.Metadata,
		Description:        t.Description,
		IsSingleton:        t.IsSingleton,
		ScopeTypes:         t.ScopeTypes,
	})
}

// FunctionParameter represents a parameter of a function
type FunctionParameter struct {
	Name        string         `json:"name"`
	Type        ITypeReference `json:"type"`
	Description string         `json:"description,omitempty"`
}

// FunctionType represents a function type
type FunctionType struct {
	Parameters   []FunctionParameter `json:"parameters"`
	ReturnType   ITypeReference      `json:"output"`
	Description  string              `json:"description,omitempty"`
	Metadata     map[string]interface{} `json:"metadata,omitempty"`
}

func (*FunctionType) Type() string { return "FunctionType" }

func (t *FunctionType) MarshalJSON() ([]byte, error) {
	return marshalTypeWithDiscriminator(t.Type(), struct {
		Parameters  []FunctionParameter    `json:"parameters"`
		ReturnType  ITypeReference         `json:"output"`
		Description string                 `json:"description,omitempty"`
		Metadata    map[string]interface{} `json:"metadata,omitempty"`
	}{
		Parameters:  t.Parameters,
		ReturnType:  t.ReturnType,
		Description: t.Description,
		Metadata:    t.Metadata,
	})
}

// ResourceFunctionType represents a resource function type
type ResourceFunctionType struct {
	Name            string                 `json:"name"`
	ResourceType    string                 `json:"resourceType"`
	APIVersion      string                 `json:"apiVersion"`
	ReturnType      ITypeReference         `json:"returnType"`
	Description     string                 `json:"description,omitempty"`
	Parameters      []FunctionParameter    `json:"parameters,omitempty"`
	Metadata        map[string]interface{} `json:"metadata,omitempty"`
}

func (*ResourceFunctionType) Type() string { return "ResourceFunctionType" }

func (t *ResourceFunctionType) MarshalJSON() ([]byte, error) {
	return marshalTypeWithDiscriminator(t.Type(), struct {
		Name         string                 `json:"name"`
		ResourceType string                 `json:"resourceType"`
		APIVersion   string                 `json:"apiVersion"`
		ReturnType   ITypeReference         `json:"returnType"`
		Description  string                 `json:"description,omitempty"`
		Parameters   []FunctionParameter    `json:"parameters,omitempty"`
		Metadata     map[string]interface{} `json:"metadata,omitempty"`
	}{
		Name:         t.Name,
		ResourceType: t.ResourceType,
		APIVersion:   t.APIVersion,
		ReturnType:   t.ReturnType,
		Description:  t.Description,
		Parameters:   t.Parameters,
		Metadata:     t.Metadata,
	})
}

// UnmarshalJSON for ObjectTypeProperty
func (p *ObjectTypeProperty) UnmarshalJSON(data []byte) error {
	var temp struct {
		Type        json.RawMessage        `json:"type"`
		Flags       TypePropertyFlags      `json:"flags,omitempty"`
		Description string                 `json:"description,omitempty"`
		Metadata    map[string]interface{} `json:"metadata,omitempty"`
	}
	
	if err := json.Unmarshal(data, &temp); err != nil {
		return err
	}
	
	// Unmarshal the type reference
	ref, err := unmarshalTypeReference(temp.Type)
	if err != nil {
		return fmt.Errorf("failed to unmarshal property type reference: %w", err)
	}
	
	p.Type = ref
	p.Flags = temp.Flags
	p.Description = temp.Description
	p.Metadata = temp.Metadata
	
	return nil
}

// UnmarshalJSON for ObjectType
func (t *ObjectType) UnmarshalJSON(data []byte) error {
	var temp struct {
		Type                 string                        `json:"$type"`
		Name                 string                        `json:"name,omitempty"`
		Properties           map[string]json.RawMessage    `json:"properties,omitempty"`
		AdditionalProperties json.RawMessage               `json:"additionalProperties,omitempty"`
		Flags                ObjectTypePropertyFlags       `json:"flags,omitempty"`
	}
	
	if err := json.Unmarshal(data, &temp); err != nil {
		return err
	}
	
	t.Name = temp.Name
	t.Flags = temp.Flags
	
	// Unmarshal properties
	if temp.Properties != nil {
		t.Properties = make(map[string]ObjectTypeProperty)
		for key, propData := range temp.Properties {
			var prop ObjectTypeProperty
			if err := json.Unmarshal(propData, &prop); err != nil {
				return fmt.Errorf("failed to unmarshal property %s: %w", key, err)
			}
			t.Properties[key] = prop
		}
	}
	
	// Unmarshal additional properties if present
	if temp.AdditionalProperties != nil {
		ref, err := unmarshalTypeReference(temp.AdditionalProperties)
		if err != nil {
			return fmt.Errorf("failed to unmarshal additional properties: %w", err)
		}
		t.AdditionalProperties = ref
	}
	
	return nil
}

// UnmarshalJSON for DiscriminatedObjectType
func (t *DiscriminatedObjectType) UnmarshalJSON(data []byte) error {
	var temp struct {
		Type          string                     `json:"$type"`
		Name          string                     `json:"name,omitempty"`
		Discriminator string                     `json:"discriminator"`
		Elements      map[string]json.RawMessage `json:"elements"`
	}
	
	if err := json.Unmarshal(data, &temp); err != nil {
		return err
	}
	
	t.Name = temp.Name
	t.Discriminator = temp.Discriminator
	
	// Unmarshal element references
	t.Elements = make(map[string]ITypeReference)
	for key, elemData := range temp.Elements {
		ref, err := unmarshalTypeReference(elemData)
		if err != nil {
			return fmt.Errorf("failed to unmarshal element %s: %w", key, err)
		}
		t.Elements[key] = ref
	}
	
	return nil
}

// UnmarshalJSON for ResourceType
func (t *ResourceType) UnmarshalJSON(data []byte) error {
	var temp struct {
		Type               string                 `json:"$type"`
		Name               string                 `json:"name"`
		ResourceTypeID     string                 `json:"resourceTypeId"`
		APIVersion         string                 `json:"apiVersion"`
		ReadOnlyAPIVersion bool                   `json:"readOnlyApiVersion,omitempty"`
		Providers          []string               `json:"providers,omitempty"`
		LocationRequired   bool                   `json:"locationRequired,omitempty"`
		ZoneRequired       bool                   `json:"zoneRequired,omitempty"`
		Body               json.RawMessage        `json:"body"`
		Metadata           map[string]interface{} `json:"metadata,omitempty"`
		Description        string                 `json:"description,omitempty"`
		IsSingleton        bool                   `json:"isSingleton,omitempty"`
		ScopeTypes         []ScopeType            `json:"scopeTypes,omitempty"`
	}
	
	if err := json.Unmarshal(data, &temp); err != nil {
		return err
	}
	
	// Unmarshal body reference
	ref, err := unmarshalTypeReference(temp.Body)
	if err != nil {
		return fmt.Errorf("failed to unmarshal resource body: %w", err)
	}
	
	t.Name = temp.Name
	t.ResourceTypeID = temp.ResourceTypeID
	t.APIVersion = temp.APIVersion
	t.ReadOnlyAPIVersion = temp.ReadOnlyAPIVersion
	t.Providers = temp.Providers
	t.LocationRequired = temp.LocationRequired
	t.ZoneRequired = temp.ZoneRequired
	t.Body = ref
	t.Metadata = temp.Metadata
	t.Description = temp.Description
	t.IsSingleton = temp.IsSingleton
	t.ScopeTypes = temp.ScopeTypes
	
	return nil
}

// UnmarshalJSON for FunctionParameter
func (p *FunctionParameter) UnmarshalJSON(data []byte) error {
	var temp struct {
		Name        string          `json:"name"`
		Type        json.RawMessage `json:"type"`
		Description string          `json:"description,omitempty"`
	}
	
	if err := json.Unmarshal(data, &temp); err != nil {
		return err
	}
	
	// Unmarshal type reference
	ref, err := unmarshalTypeReference(temp.Type)
	if err != nil {
		return fmt.Errorf("failed to unmarshal parameter type: %w", err)
	}
	
	p.Name = temp.Name
	p.Type = ref
	p.Description = temp.Description
	
	return nil
}

// UnmarshalJSON for FunctionType
func (t *FunctionType) UnmarshalJSON(data []byte) error {
	var temp struct {
		Type        string                 `json:"$type"`
		Parameters  []json.RawMessage      `json:"parameters"`
		ReturnType  json.RawMessage        `json:"output"`
		Description string                 `json:"description,omitempty"`
		Metadata    map[string]interface{} `json:"metadata,omitempty"`
	}
	
	if err := json.Unmarshal(data, &temp); err != nil {
		return err
	}
	
	// Unmarshal parameters
	t.Parameters = make([]FunctionParameter, 0, len(temp.Parameters))
	for i, paramData := range temp.Parameters {
		var param FunctionParameter
		if err := json.Unmarshal(paramData, &param); err != nil {
			return fmt.Errorf("failed to unmarshal parameter %d: %w", i, err)
		}
		t.Parameters = append(t.Parameters, param)
	}
	
	// Unmarshal return type reference
	ref, err := unmarshalTypeReference(temp.ReturnType)
	if err != nil {
		return fmt.Errorf("failed to unmarshal return type: %w", err)
	}
	
	t.ReturnType = ref
	t.Description = temp.Description
	t.Metadata = temp.Metadata
	
	return nil
}

// UnmarshalJSON for ResourceFunctionType
func (t *ResourceFunctionType) UnmarshalJSON(data []byte) error {
	var temp struct {
		Type         string                 `json:"$type"`
		Name         string                 `json:"name"`
		ResourceType string                 `json:"resourceType"`
		APIVersion   string                 `json:"apiVersion"`
		ReturnType   json.RawMessage        `json:"returnType"`
		Description  string                 `json:"description,omitempty"`
		Parameters   []json.RawMessage      `json:"parameters,omitempty"`
		Metadata     map[string]interface{} `json:"metadata,omitempty"`
	}
	
	if err := json.Unmarshal(data, &temp); err != nil {
		return err
	}
	
	// Unmarshal return type reference
	ref, err := unmarshalTypeReference(temp.ReturnType)
	if err != nil {
		return fmt.Errorf("failed to unmarshal return type: %w", err)
	}
	
	// Unmarshal parameters if present
	if temp.Parameters != nil {
		t.Parameters = make([]FunctionParameter, 0, len(temp.Parameters))
		for i, paramData := range temp.Parameters {
			var param FunctionParameter
			if err := json.Unmarshal(paramData, &param); err != nil {
				return fmt.Errorf("failed to unmarshal parameter %d: %w", i, err)
			}
			t.Parameters = append(t.Parameters, param)
		}
	}
	
	t.Name = temp.Name
	t.ResourceType = temp.ResourceType
	t.APIVersion = temp.APIVersion
	t.ReturnType = ref
	t.Description = temp.Description
	t.Metadata = temp.Metadata
	
	return nil
}