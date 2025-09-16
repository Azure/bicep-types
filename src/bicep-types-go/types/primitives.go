// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
package types

import (
	"encoding/json"
	"fmt"
)

// AnyType represents a type that can be any value
type AnyType struct{}

func (*AnyType) Type() string { return "AnyType" }

func (t *AnyType) MarshalJSON() ([]byte, error) {
	return marshalTypeWithDiscriminator(t.Type(), struct{}{})
}

// NullType represents a null value type
type NullType struct{}

func (*NullType) Type() string { return "NullType" }

func (t *NullType) MarshalJSON() ([]byte, error) {
	return marshalTypeWithDiscriminator(t.Type(), struct{}{})
}

// BooleanType represents a boolean value type
type BooleanType struct{}

func (*BooleanType) Type() string { return "BooleanType" }

func (t *BooleanType) MarshalJSON() ([]byte, error) {
	return marshalTypeWithDiscriminator(t.Type(), struct{}{})
}

// IntegerType represents an integer value type
type IntegerType struct {
	MinValue *int64 `json:"minValue,omitempty"`
	MaxValue *int64 `json:"maxValue,omitempty"`
}

func (*IntegerType) Type() string { return "IntegerType" }

func (t *IntegerType) MarshalJSON() ([]byte, error) {
	return marshalTypeWithDiscriminator(t.Type(), struct {
		MinValue *int64 `json:"minValue,omitempty"`
		MaxValue *int64 `json:"maxValue,omitempty"`
	}{
		MinValue: t.MinValue,
		MaxValue: t.MaxValue,
	})
}

// StringType represents a string value type
type StringType struct {
	MinLength *int64 `json:"minLength,omitempty"`
	MaxLength *int64 `json:"maxLength,omitempty"`
	Pattern   string `json:"pattern,omitempty"`
	Sensitive bool   `json:"sensitive,omitempty"`
}

func (*StringType) Type() string { return "StringType" }

func (t *StringType) MarshalJSON() ([]byte, error) {
	return marshalTypeWithDiscriminator(t.Type(), struct {
		MinLength *int64 `json:"minLength,omitempty"`
		MaxLength *int64 `json:"maxLength,omitempty"`
		Pattern   string `json:"pattern,omitempty"`
		Sensitive bool   `json:"sensitive,omitempty"`
	}{
		MinLength: t.MinLength,
		MaxLength: t.MaxLength,
		Pattern:   t.Pattern,
		Sensitive: t.Sensitive,
	})
}

// StringLiteralType represents a string literal with a specific value
type StringLiteralType struct {
	Value     string `json:"value"`
	Sensitive bool   `json:"sensitive,omitempty"`
}

func (*StringLiteralType) Type() string { return "StringLiteralType" }

func (t *StringLiteralType) MarshalJSON() ([]byte, error) {
	return marshalTypeWithDiscriminator(t.Type(), struct {
		Value     string `json:"value"`
		Sensitive bool   `json:"sensitive,omitempty"`
	}{
		Value:     t.Value,
		Sensitive: t.Sensitive,
	})
}

// BuiltInType represents a built-in type by name
type BuiltInType struct {
	Kind string `json:"kind"`
}

func (*BuiltInType) Type() string { return "BuiltInType" }

func (t *BuiltInType) MarshalJSON() ([]byte, error) {
	return marshalTypeWithDiscriminator(t.Type(), struct {
		Kind string `json:"kind"`
	}{
		Kind: t.Kind,
	})
}

// ArrayType represents an array type
type ArrayType struct {
	ItemType  ITypeReference `json:"itemType"`
	MinLength *int64         `json:"minLength,omitempty"`
	MaxLength *int64         `json:"maxLength,omitempty"`
}

func (*ArrayType) Type() string { return "ArrayType" }

func (t *ArrayType) MarshalJSON() ([]byte, error) {
	return marshalTypeWithDiscriminator(t.Type(), struct {
		ItemType  ITypeReference `json:"itemType"`
		MinLength *int64         `json:"minLength,omitempty"`
		MaxLength *int64         `json:"maxLength,omitempty"`
	}{
		ItemType:  t.ItemType,
		MinLength: t.MinLength,
		MaxLength: t.MaxLength,
	})
}

// UnionType represents a union of multiple types
type UnionType struct {
	Elements []ITypeReference `json:"elements"`
}

func (*UnionType) Type() string { return "UnionType" }

func (t *UnionType) MarshalJSON() ([]byte, error) {
	return marshalTypeWithDiscriminator(t.Type(), struct {
		Elements []ITypeReference `json:"elements"`
	}{
		Elements: t.Elements,
	})
}

// UnmarshalJSON for ArrayType
func (t *ArrayType) UnmarshalJSON(data []byte) error {
	var temp struct {
		Type      string          `json:"$type"`
		ItemType  json.RawMessage `json:"itemType"`
		MinLength *int64          `json:"minLength,omitempty"`
		MaxLength *int64          `json:"maxLength,omitempty"`
	}

	if err := json.Unmarshal(data, &temp); err != nil {
		return err
	}

	// Unmarshal the item type reference
	ref, err := unmarshalTypeReference(temp.ItemType)
	if err != nil {
		return fmt.Errorf("failed to unmarshal item type reference: %w", err)
	}

	t.ItemType = ref
	t.MinLength = temp.MinLength
	t.MaxLength = temp.MaxLength

	return nil
}

// UnmarshalJSON for UnionType
func (t *UnionType) UnmarshalJSON(data []byte) error {
	var temp struct {
		Type     string            `json:"$type"`
		Elements []json.RawMessage `json:"elements"`
	}

	if err := json.Unmarshal(data, &temp); err != nil {
		return err
	}

	// Unmarshal each element reference
	t.Elements = make([]ITypeReference, 0, len(temp.Elements))
	for i, elem := range temp.Elements {
		ref, err := unmarshalTypeReference(elem)
		if err != nil {
			return fmt.Errorf("failed to unmarshal element %d: %w", i, err)
		}
		t.Elements = append(t.Elements, ref)
	}

	return nil
}
