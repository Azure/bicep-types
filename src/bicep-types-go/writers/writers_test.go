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
	resourceType := &types.ResourceType{
		Name:           "Microsoft.Test/resources@2023-01-01",
		Body:           types.TypeReference{Ref: 0},
		Functions:      nil,
		ReadableScopes: types.AllExceptExtension,
		WritableScopes: types.AllExceptExtension,
	}

	objectType := &types.ObjectType{
		Name: "TestObject",
		Properties: map[string]types.ObjectTypeProperty{
			"name": {
				Type:        types.TypeReference{Ref: 1},
				Flags:       types.TypePropertyFlagsRequired,
				Description: "The name property",
			},
			"value": {
				Type:  types.TypeReference{Ref: 2},
				Flags: types.TypePropertyFlagsReadOnly,
			},
		},
	}

	testTypes := []types.Type{resourceType, objectType}

	var buf bytes.Buffer
	err := writer.WriteTypes(&buf, testTypes)
	require.NoError(t, err)

	output := buf.String()

	// Should contain markdown headers and content
	assert.Contains(t, output, "# Types")
	assert.Contains(t, output, "## Resource Types")
	assert.Contains(t, output, "**Resource Type ID:** `Microsoft.Test/resources`")
	assert.Contains(t, output, "**API Version:** `2023-01-01`")
	assert.Contains(t, output, "## Object Types")
	assert.Contains(t, output, "### TestObject")
	assert.Contains(t, output, "- `name`: The name property (required)")
	assert.Contains(t, output, "- `value` (read-only)")
}

func TestMarkdownWriter_WriteTypeIndex(t *testing.T) {
	writer := NewMarkdownWriter()

	// Create test index
	idx := index.NewTypeIndex()
	idx.AddResource("Microsoft.Test/resources", "2023-01-01", types.TypeReference{Ref: 0})
	idx.AddResource("Microsoft.Test/resources", "2023-02-01", types.TypeReference{Ref: 1})
	idx.AddResourceFunction("Microsoft.Test/resources", "2023-01-01", "list", types.TypeReference{Ref: 2})

	var buf bytes.Buffer
	err := writer.WriteTypeIndex(&buf, idx)
	require.NoError(t, err)

	output := buf.String()

	// Should contain index structure
	assert.Contains(t, output, "# Type Index")
	assert.Contains(t, output, "## Resources")
	assert.Contains(t, output, "### Microsoft.Test/resources")
	assert.Contains(t, output, "**API Versions:**")
	assert.Contains(t, output, "- `2023-01-01`")
	assert.Contains(t, output, "- `2023-02-01`")
	assert.Contains(t, output, "## Resource Functions")
	assert.Contains(t, output, "- `list`")
}

func TestMarkdownWriter_WithTableOfContents(t *testing.T) {
	writer := NewMarkdownWriter()
	writer.SetIncludeTableOfContents(true)

	testTypes := []types.Type{
		&types.ResourceType{Name: "TestResource"},
		&types.FunctionType{},
		&types.ObjectType{Name: "TestObject"},
	}

	var buf bytes.Buffer
	err := writer.WriteTypes(&buf, testTypes)
	require.NoError(t, err)

	output := buf.String()

	// Should contain table of contents
	assert.Contains(t, output, "## Table of Contents")
	assert.Contains(t, output, "- [Resource Types](#resource-types)")
	assert.Contains(t, output, "- [Function Types](#function-types)")
	assert.Contains(t, output, "- [Object Types](#object-types)")
}

func TestMarkdownWriter_WithoutTableOfContents(t *testing.T) {
	writer := NewMarkdownWriter()
	writer.SetIncludeTableOfContents(false)

	testTypes := []types.Type{
		&types.ResourceType{Name: "TestResource"},
	}

	var buf bytes.Buffer
	err := writer.WriteTypes(&buf, testTypes)
	require.NoError(t, err)

	output := buf.String()

	// Should not contain table of contents
	assert.NotContains(t, output, "## Table of Contents")
	assert.Contains(t, output, "## Resource Types")
}

func TestMarkdownWriter_ResourceFunctionTypes(t *testing.T) {
	writer := NewMarkdownWriter()

	rft := &types.ResourceFunctionType{
		Name:         "listKeys",
		ResourceType: "Microsoft.Storage/storageAccounts",
		ApiVersion:   "2023-01-01",
		Output:       types.TypeReference{Ref: 1},
	}

	testTypes := []types.Type{rft}

	var buf bytes.Buffer
	err := writer.WriteTypes(&buf, testTypes)
	require.NoError(t, err)

	output := buf.String()

	assert.Contains(t, output, "## Resource Function Types")
	assert.Contains(t, output, "### listKeys")
	assert.Contains(t, output, "**Resource Type:** `Microsoft.Storage/storageAccounts`")
	assert.Contains(t, output, "**API Version:** `2023-01-01`")
}

func TestMarkdownWriter_PropertyFlags(t *testing.T) {
	writer := NewMarkdownWriter()

	objectType := &types.ObjectType{
		Name: "TestObject",
		Properties: map[string]types.ObjectTypeProperty{
			"readOnlyProp": {
				Type:  types.TypeReference{Ref: 0},
				Flags: types.TypePropertyFlagsReadOnly,
			},
			"requiredProp": {
				Type:  types.TypeReference{Ref: 1},
				Flags: types.TypePropertyFlagsRequired,
			},
			"identifierProp": {
				Type:  types.TypeReference{Ref: 2},
				Flags: types.TypePropertyFlagsIdentifier,
			},
			"combinedFlags": {
				Type:  types.TypeReference{Ref: 3},
				Flags: types.TypePropertyFlagsRequired | types.TypePropertyFlagsReadOnly,
			},
		},
	}

	testTypes := []types.Type{objectType}

	var buf bytes.Buffer
	err := writer.WriteTypes(&buf, testTypes)
	require.NoError(t, err)

	output := buf.String()

	assert.Contains(t, output, "- `readOnlyProp` (read-only)")
	assert.Contains(t, output, "- `requiredProp` (required)")
	assert.Contains(t, output, "- `identifierProp` (identifier)")
	assert.Contains(t, output, "- `combinedFlags` (required, read-only)")
}

// Helper function
func int64Ptr(v int64) *int64 {
	return &v
}
