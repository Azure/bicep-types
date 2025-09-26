// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
package writers

import (
	"bytes"
	"strings"
	"testing"

	"github.com/stretchr/testify/assert"
	"github.com/stretchr/testify/require"

	"github.com/Azure/bicep-types/src/bicep-types-go/index"
	"github.com/Azure/bicep-types/src/bicep-types-go/types"
)

func TestJSONWriter_WriteTypes(t *testing.T) {
	writer := NewJSONWriter()

	// Create test types
	testTypes := []types.Type{
		&types.StringType{},
		&types.IntegerType{MinValue: int64Ptr(0), MaxValue: int64Ptr(100)},
		&types.BooleanType{},
	}

	var buf bytes.Buffer
	err := writer.WriteTypes(&buf, testTypes)
	require.NoError(t, err)

	output := buf.String()

	// Should be valid JSON
	assert.Contains(t, output, `"$type": "StringType"`)
	assert.Contains(t, output, `"$type": "IntegerType"`)
	assert.Contains(t, output, `"$type": "BooleanType"`)
	assert.Contains(t, output, `"minValue": 0`)
	assert.Contains(t, output, `"maxValue": 100`)
}

func TestJSONWriter_WriteTypeIndex(t *testing.T) {
	writer := NewJSONWriter()

	// Create test index
	idx := index.NewTypeIndex()
	idx.AddResource("Microsoft.Test/resources", "2023-01-01", types.TypeReference{Ref: 0})
	idx.AddResourceFunction("Microsoft.Test/resources", "2023-01-01", "list", types.TypeReference{Ref: 1})

	var buf bytes.Buffer
	err := writer.WriteTypeIndex(&buf, idx)
	require.NoError(t, err)

	output := buf.String()

	// Should contain resource and function mappings
	assert.Contains(t, output, "Microsoft.Test/resources")
	assert.Contains(t, output, "2023-01-01")
	assert.Contains(t, output, "list")
	assert.Contains(t, output, `"$ref"`)
}

func TestJSONWriter_WriteTypesToString(t *testing.T) {
	writer := NewJSONWriter()

	testTypes := []types.Type{
		&types.StringType{Pattern: "^[a-z]+$"},
		&types.AnyType{},
	}

	output, err := writer.WriteTypesToString(testTypes)
	require.NoError(t, err)

	assert.Contains(t, output, `"$type": "StringType"`)
	assert.Contains(t, output, `"$type": "AnyType"`)
	assert.Contains(t, output, `"pattern": "^[a-z]+$"`)
}

func TestJSONWriter_WithIndentation(t *testing.T) {
	writer := NewJSONWriterWithIndent(4)

	testTypes := []types.Type{
		&types.StringType{},
	}

	output, err := writer.WriteTypesToString(testTypes)
	require.NoError(t, err)

	// Should have 4-space indentation
	lines := strings.Split(output, "\n")
	assert.True(t, strings.HasPrefix(lines[1], "    "), "Should have 4-space indentation")
}

func TestMarkdownWriter_WriteTypes(t *testing.T) {
	writer := NewMarkdownWriter()

	// Create test types
	stringType := &types.StringType{}
	boolType := &types.BooleanType{}
	resourceType := &types.ResourceType{
		Name:           "Microsoft.Test/resources@2023-01-01",
		Body:           types.TypeReference{Ref: 1},
		Functions:      nil,
		ReadableScopes: types.AllExceptExtension,
		WritableScopes: types.AllExceptExtension,
	}

	objectType := &types.ObjectType{
		Name: "TestObject",
		Properties: map[string]types.ObjectTypeProperty{
			"name": {
				Type:        types.TypeReference{Ref: 2},
				Flags:       types.TypePropertyFlagsRequired,
				Description: "The name property",
			},
			"value": {
				Type:  types.TypeReference{Ref: 3},
				Flags: types.TypePropertyFlagsReadOnly,
			},
		},
	}

	testTypes := []types.Type{resourceType, objectType, stringType, boolType}

	var buf bytes.Buffer
	err := writer.WriteTypes(&buf, testTypes)
	require.NoError(t, err)

	output := buf.String()

	// Should contain TypeScript-aligned markdown output
	assert.Contains(t, output, "# Bicep Types")
	assert.Contains(t, output, "## Resource Microsoft.Test/resources@2023-01-01")
	assert.Contains(t, output, "* **Readable Scope(s)**: Tenant, ManagementGroup, Subscription, ResourceGroup")
	assert.Contains(t, output, "* **Writable Scope(s)**: Tenant, ManagementGroup, Subscription, ResourceGroup")
	assert.Contains(t, output, "### Properties")
	assert.Contains(t, output, "* **name**: string (Required): The name property")
	assert.Contains(t, output, "* **value**: bool (ReadOnly)")
}

func TestMarkdownWriter_WriteTypeIndex(t *testing.T) {
	writer := NewMarkdownWriter()

	// Create test index
	idx := index.NewTypeIndex()
	idx.AddResource("Microsoft.Test/resources", "2023-01-01", types.CrossFileTypeReference{RelativePath: "providers/Microsoft.Test/resources.json", Ref: 0})
	idx.AddResource("Microsoft.Test/resources", "2023-02-01", types.CrossFileTypeReference{RelativePath: "providers/Microsoft.Test/resources.json", Ref: 1})

	var buf bytes.Buffer
	err := writer.WriteTypeIndex(&buf, idx)
	require.NoError(t, err)

	output := buf.String()

	// Should contain flattened index structure matching TypeScript writer
	assert.Contains(t, output, "# Bicep Types")
	assert.Contains(t, output, "## microsoft.test")
	assert.Contains(t, output, "### microsoft.test/resources")
	assert.Contains(t, output, "* **Link**: [2023-01-01](providers/Microsoft.Test/resources.md#resource-microsofttestresources2023-01-01)")
	assert.Contains(t, output, "* **Link**: [2023-02-01](providers/Microsoft.Test/resources.md#resource-microsofttestresources2023-02-01)")
}

func TestMarkdownWriter_ResourceFunctionTypes(t *testing.T) {
	writer := NewMarkdownWriter()

	rft := &types.ResourceFunctionType{
		Name:         "listKeys",
		ResourceType: "Microsoft.Storage/storageAccounts",
		ApiVersion:   "2023-01-01",
		Output:       types.TypeReference{Ref: 1},
	}

	stringType := &types.StringType{}

	testTypes := []types.Type{rft, stringType}

	var buf bytes.Buffer
	err := writer.WriteTypes(&buf, testTypes)
	require.NoError(t, err)

	output := buf.String()

	assert.Contains(t, output, "## Function listKeys (Microsoft.Storage/storageAccounts@2023-01-01)")
	assert.Contains(t, output, "* **Resource**: Microsoft.Storage/storageAccounts")
	assert.Contains(t, output, "* **ApiVersion**: 2023-01-01")
	assert.Contains(t, output, "* **Output**: string")
}

func TestMarkdownWriter_PropertyFlags(t *testing.T) {
	writer := NewMarkdownWriter()

	stringType := &types.StringType{}
	boolType := &types.BooleanType{}
	anotherString := &types.StringType{}

	objectType := &types.ObjectType{
		Name: "TestObject",
		Properties: map[string]types.ObjectTypeProperty{
			"readOnlyProp": {
				Type:  types.TypeReference{Ref: 2},
				Flags: types.TypePropertyFlagsReadOnly,
			},
			"requiredProp": {
				Type:  types.TypeReference{Ref: 2},
				Flags: types.TypePropertyFlagsRequired,
			},
			"identifierProp": {
				Type:  types.TypeReference{Ref: 3},
				Flags: types.TypePropertyFlagsIdentifier,
			},
			"combinedFlags": {
				Type:  types.TypeReference{Ref: 4},
				Flags: types.TypePropertyFlagsRequired | types.TypePropertyFlagsReadOnly,
			},
		},
	}

	resourceType := &types.ResourceType{
		Name: "Test/resource@2023-01-01",
		Body: types.TypeReference{Ref: 1},
	}

	testTypes := []types.Type{resourceType, objectType, stringType, boolType, anotherString}

	var buf bytes.Buffer
	err := writer.WriteTypes(&buf, testTypes)
	require.NoError(t, err)

	output := buf.String()

	assert.Contains(t, output, "* **readOnlyProp**: string (ReadOnly)")
	assert.Contains(t, output, "* **requiredProp**: string (Required)")
	assert.Contains(t, output, "* **identifierProp**: bool (Identifier)")
	assert.Contains(t, output, "* **combinedFlags**: string (Required, ReadOnly)")
}

// Helper function
func int64Ptr(v int64) *int64 {
	return &v
}
