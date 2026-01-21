// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
package types

import (
	"encoding/json"
	"fmt"
)

// ObjectTypeProperty represents a property of an object type
type ObjectTypeProperty struct {
	Type        ITypeReference    `json:"type"`
	Flags       TypePropertyFlags `json:"flags"`
	Description string            `json:"description,omitempty"`
}

// ObjectType represents an object type with properties
type ObjectType struct {
	Name                 string                        `json:"name,omitempty"`
	Properties           map[string]ObjectTypeProperty `json:"properties"`
	AdditionalProperties ITypeReference                `json:"additionalProperties,omitempty"`
	Sensitive            *bool                         `json:"sensitive,omitempty"`
}

func (*ObjectType) Type() string { return "ObjectType" }

func (t *ObjectType) MarshalJSON() ([]byte, error) {
	return marshalTypeWithDiscriminator(t.Type(), struct {
		Name                 string                        `json:"name,omitempty"`
		Properties           map[string]ObjectTypeProperty `json:"properties"`
		AdditionalProperties ITypeReference                `json:"additionalProperties,omitempty"`
		Sensitive            *bool                         `json:"sensitive,omitempty"`
	}{
		Name:                 t.Name,
		Properties:           t.Properties,
		AdditionalProperties: t.AdditionalProperties,
		Sensitive:            t.Sensitive,
	})
}

// DiscriminatedObjectType represents an object type with a discriminator property
type DiscriminatedObjectType struct {
	Name           string                        `json:"name,omitempty"`
	Discriminator  string                        `json:"discriminator"`
	BaseProperties map[string]ObjectTypeProperty `json:"baseProperties"`
	Elements       map[string]ITypeReference     `json:"elements"`
}

func (*DiscriminatedObjectType) Type() string { return "DiscriminatedObjectType" }

func (t *DiscriminatedObjectType) MarshalJSON() ([]byte, error) {
	return marshalTypeWithDiscriminator(t.Type(), struct {
		Name           string                        `json:"name,omitempty"`
		Discriminator  string                        `json:"discriminator"`
		BaseProperties map[string]ObjectTypeProperty `json:"baseProperties"`
		Elements       map[string]ITypeReference     `json:"elements"`
	}{
		Name:           t.Name,
		Discriminator:  t.Discriminator,
		BaseProperties: t.BaseProperties,
		Elements:       t.Elements,
	})
}

// ResourceType represents an Azure resource type
type ResourceType struct {
	Name           string                          `json:"name"`
	Body           ITypeReference                  `json:"body"`
	Functions      map[string]ResourceTypeFunction `json:"functions,omitempty"`
	ReadableScopes ScopeType                       `json:"readableScopes"`
	WritableScopes ScopeType                       `json:"writableScopes"`
}

// Add the ResourceTypeFunction struct:
type ResourceTypeFunction struct {
	Type        ITypeReference `json:"type"`
	Description string         `json:"description,omitempty"`
}

func (*ResourceType) Type() string { return "ResourceType" }

func (t *ResourceType) MarshalJSON() ([]byte, error) {
	return marshalTypeWithDiscriminator(t.Type(), struct {
		Name           string                          `json:"name"`
		Body           ITypeReference                  `json:"body"`
		Functions      map[string]ResourceTypeFunction `json:"functions,omitempty"`
		ReadableScopes ScopeType                       `json:"readableScopes"`
		WritableScopes ScopeType                       `json:"writableScopes"`
	}{
		Name:           t.Name,
		Body:           t.Body,
		Functions:      t.Functions,
		ReadableScopes: t.ReadableScopes,
		WritableScopes: t.WritableScopes,
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
	Parameters []FunctionParameter `json:"parameters"`
	Output     ITypeReference      `json:"output"`
}

func (*FunctionType) Type() string { return "FunctionType" }

func (t *FunctionType) MarshalJSON() ([]byte, error) {
	return marshalTypeWithDiscriminator(t.Type(), struct {
		Parameters []FunctionParameter `json:"parameters"`
		Output     ITypeReference      `json:"output"`
	}{
		Parameters: t.Parameters,
		Output:     t.Output,
	})
}

// ResourceFunctionType represents a resource function type (standalone)
type ResourceFunctionType struct {
	Name         string         `json:"name"`
	ResourceType string         `json:"resourceType"`
	ApiVersion   string         `json:"apiVersion"`
	Output       ITypeReference `json:"output"`
	Input        ITypeReference `json:"input,omitempty"`
}

func (*ResourceFunctionType) Type() string { return "ResourceFunctionType" }

func (t *ResourceFunctionType) MarshalJSON() ([]byte, error) {
	return marshalTypeWithDiscriminator(t.Type(), struct {
		Name         string         `json:"name"`
		ResourceType string         `json:"resourceType"`
		ApiVersion   string         `json:"apiVersion"`
		Output       ITypeReference `json:"output"`
		Input        ITypeReference `json:"input,omitempty"`
	}{
		Name:         t.Name,
		ResourceType: t.ResourceType,
		ApiVersion:   t.ApiVersion,
		Output:       t.Output,
		Input:        t.Input,
	})
}

// NamespaceFunctionParameter represents a parameter of a namespace function
type NamespaceFunctionParameter struct {
	Name        string                          `json:"name"`
	Type        ITypeReference                  `json:"type"`
	Description string                          `json:"description,omitempty"`
	Flags       NamespaceFunctionParameterFlags `json:"flags"`
}

// NamespaceFunctionType represents a namespace function type
type NamespaceFunctionType struct {
	Name                        string                       `json:"name"`
	Description                 string                       `json:"description,omitempty"`
	EvaluatedLanguageExpression string                       `json:"evaluatedLanguageExpression,omitempty"`
	Parameters                  []NamespaceFunctionParameter `json:"parameters"`
	OutputType                  ITypeReference               `json:"outputType"`
	VisibleInFileKind           *BicepSourceFileKind         `json:"visibleInFileKind,omitempty"`
}

func (*NamespaceFunctionType) Type() string { return "NamespaceFunctionType" }

func (t *NamespaceFunctionType) MarshalJSON() ([]byte, error) {
	return marshalTypeWithDiscriminator(t.Type(), struct {
		Name                        string                       `json:"name"`
		Description                 string                       `json:"description,omitempty"`
		EvaluatedLanguageExpression string                       `json:"evaluatedLanguageExpression,omitempty"`
		Parameters                  []NamespaceFunctionParameter `json:"parameters"`
		OutputType                  ITypeReference               `json:"outputType"`
		VisibleInFileKind           *BicepSourceFileKind         `json:"visibleInFileKind,omitempty"`
	}{
		Name:                        t.Name,
		Description:                 t.Description,
		EvaluatedLanguageExpression: t.EvaluatedLanguageExpression,
		Parameters:                  t.Parameters,
		OutputType:                  t.OutputType,
		VisibleInFileKind:           t.VisibleInFileKind,
	})
}

// UnmarshalJSON for NamespaceFunctionParameter
func (p *NamespaceFunctionParameter) UnmarshalJSON(data []byte) error {
	var temp struct {
		Name        string                          `json:"name"`
		Type        json.RawMessage                 `json:"type"`
		Description string                          `json:"description,omitempty"`
		Flags       NamespaceFunctionParameterFlags `json:"flags"`
	}

	if err := json.Unmarshal(data, &temp); err != nil {
		return err
	}

	// Unmarshal type reference
	ref, err := unmarshalTypeReference(temp.Type)
	if err != nil {
		return fmt.Errorf("failed to unmarshal namespace function parameter type: %w", err)
	}

	p.Name = temp.Name
	p.Type = ref
	p.Description = temp.Description
	p.Flags = temp.Flags

	return nil
}

// UnmarshalJSON for NamespaceFunctionType
func (t *NamespaceFunctionType) UnmarshalJSON(data []byte) error {
	var temp struct {
		Name                        string               `json:"name"`
		Description                 string               `json:"description,omitempty"`
		EvaluatedLanguageExpression string               `json:"evaluatedLanguageExpression,omitempty"`
		Parameters                  []json.RawMessage    `json:"parameters"`
		OutputType                  json.RawMessage      `json:"outputType"`
		VisibleInFileKind           *BicepSourceFileKind `json:"visibleInFileKind,omitempty"`
	}

	if err := json.Unmarshal(data, &temp); err != nil {
		return err
	}

	// Unmarshal parameters
	params := make([]NamespaceFunctionParameter, len(temp.Parameters))
	for i, paramData := range temp.Parameters {
		if err := json.Unmarshal(paramData, &params[i]); err != nil {
			return fmt.Errorf("failed to unmarshal namespace function parameter %d: %w", i, err)
		}
	}

	// Unmarshal output type reference
	outputRef, err := unmarshalTypeReference(temp.OutputType)
	if err != nil {
		return fmt.Errorf("failed to unmarshal namespace function output type: %w", err)
	}

	t.Name = temp.Name
	t.Description = temp.Description
	t.EvaluatedLanguageExpression = temp.EvaluatedLanguageExpression
	t.Parameters = params
	t.OutputType = outputRef
	t.VisibleInFileKind = temp.VisibleInFileKind

	return nil
}

// UnmarshalJSON for ObjectTypeProperty
func (p *ObjectTypeProperty) UnmarshalJSON(data []byte) error {
	var temp struct {
		Type        json.RawMessage   `json:"type"`
		Flags       TypePropertyFlags `json:"flags,omitempty"`
		Description string            `json:"description,omitempty"`
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

	return nil
}

// UnmarshalJSON for ObjectType
func (t *ObjectType) UnmarshalJSON(data []byte) error {
	var temp struct {
		Name                 string                     `json:"name,omitempty"`
		Properties           map[string]json.RawMessage `json:"properties,omitempty"`
		AdditionalProperties json.RawMessage            `json:"additionalProperties,omitempty"`
		Sensitive            *bool                      `json:"sensitive,omitempty"`
	}

	if err := json.Unmarshal(data, &temp); err != nil {
		return err
	}

	t.Name = temp.Name
	t.Sensitive = temp.Sensitive

	t.Properties = make(map[string]ObjectTypeProperty)
	// Unmarshal properties
	if temp.Properties != nil {
		//t.Properties = make(map[string]ObjectTypeProperty)
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
		Name           string                     `json:"name,omitempty"`
		Discriminator  string                     `json:"discriminator"`
		BaseProperties map[string]json.RawMessage `json:"baseProperties"`
		Elements       map[string]json.RawMessage `json:"elements"`
	}

	if err := json.Unmarshal(data, &temp); err != nil {
		return err
	}

	t.Name = temp.Name
	t.Discriminator = temp.Discriminator

	// Unmarshal base properties
	if temp.BaseProperties != nil {
		t.BaseProperties = make(map[string]ObjectTypeProperty)
		for key, propData := range temp.BaseProperties {
			var prop ObjectTypeProperty
			if err := json.Unmarshal(propData, &prop); err != nil {
				return fmt.Errorf("failed to unmarshal base property %s: %w", key, err)
			}
			t.BaseProperties[key] = prop
		}
	}

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
		Name           string                     `json:"name"`
		Body           json.RawMessage            `json:"body"`
		Functions      map[string]json.RawMessage `json:"functions,omitempty"`
		ReadableScopes ScopeType                  `json:"readableScopes"`
		WritableScopes ScopeType                  `json:"writableScopes"`
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
	t.Body = ref
	t.ReadableScopes = temp.ReadableScopes
	t.WritableScopes = temp.WritableScopes

	// Unmarshal functions separately
	if temp.Functions != nil {
		t.Functions = make(map[string]ResourceTypeFunction)
		for funcName, funcData := range temp.Functions {
			var rtf ResourceTypeFunction
			if err := json.Unmarshal(funcData, &rtf); err != nil {
				return fmt.Errorf("failed to unmarshal function %s: %w", funcName, err)
			}
			t.Functions[funcName] = rtf
		}
	}

	return nil
}

// UnmarshalJSON for ResourceTypeFunction
func (rtf *ResourceTypeFunction) UnmarshalJSON(data []byte) error {
	var temp struct {
		Type        json.RawMessage `json:"type"`
		Description string          `json:"description,omitempty"`
	}

	if err := json.Unmarshal(data, &temp); err != nil {
		return err
	}

	// Unmarshal the type reference
	typeRef, err := unmarshalTypeReference(temp.Type)
	if err != nil {
		return fmt.Errorf("failed to unmarshal function type reference: %w", err)
	}

	rtf.Type = typeRef
	rtf.Description = temp.Description

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
		Parameters []json.RawMessage `json:"parameters"`
		Output     json.RawMessage   `json:"output"`
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
	ref, err := unmarshalTypeReference(temp.Output)
	if err != nil {
		return fmt.Errorf("failed to unmarshal return type: %w", err)
	}

	t.Output = ref

	return nil
}

// UnmarshalJSON for ResourceFunctionType
func (t *ResourceFunctionType) UnmarshalJSON(data []byte) error {
	var temp struct {
		Name         string          `json:"name"`
		ResourceType string          `json:"resourceType"`
		ApiVersion   string          `json:"apiVersion"`
		Output       json.RawMessage `json:"output"`
		Input        json.RawMessage `json:"input,omitempty"`
	}

	if err := json.Unmarshal(data, &temp); err != nil {
		return err
	}

	// Unmarshal return type reference
	outputRef, err := unmarshalTypeReference(temp.Output)
	if err != nil {
		return fmt.Errorf("failed to unmarshal output type: %w", err)
	}

	// Unmarshal input type reference if present
	var inputRef ITypeReference
	if temp.Input != nil {
		inputRef, err = unmarshalTypeReference(temp.Input)
		if err != nil {
			return fmt.Errorf("failed to unmarshal input type: %w", err)
		}
	}

	t.Name = temp.Name
	t.ResourceType = temp.ResourceType
	t.ApiVersion = temp.ApiVersion // Changed from t.APIVersion
	t.Output = outputRef           // Changed from t.ReturnType
	t.Input = inputRef

	return nil
}
