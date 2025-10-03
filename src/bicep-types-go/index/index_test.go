// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
package index

import (
	"encoding/json"
	"testing"

	"github.com/stretchr/testify/assert"
	"github.com/stretchr/testify/require"

	"github.com/Azure/bicep-types/src/bicep-types-go/types"
)

func TestTypeIndex_UnmarshalJSON(t *testing.T) {
	// Test basic index structure
	jsonData := `{
		"resources": {
			"foo@v1": {
				"$ref": "#/0"
			}
		},
		"resourceFunctions": {}
	}`

	var idx TypeIndex
	err := json.Unmarshal([]byte(jsonData), &idx)
	require.NoError(t, err)

	assert.NotNil(t, idx.Resources)
	assert.Contains(t, idx.Resources, "foo")
	assert.Contains(t, idx.Resources["foo"], "v1")
}

func TestTypeIndex_UnmarshalJSON_WithCrossFileRef(t *testing.T) {
	// Test cross-file reference
	jsonData := `{
		"resources": {
			"request@v1": {
				"$ref": "http/v1/types.json#/8"
			}
		},
		"resourceFunctions": {}
	}`

	var idx TypeIndex
	err := json.Unmarshal([]byte(jsonData), &idx)
	require.NoError(t, err)

	assert.NotNil(t, idx.Resources)
	assert.Contains(t, idx.Resources, "request")
	assert.Contains(t, idx.Resources["request"], "v1")

	ref := idx.Resources["request"]["v1"]
	crossRef, ok := ref.(types.CrossFileTypeReference)
	require.True(t, ok)
	assert.Equal(t, "http/v1/types.json", crossRef.RelativePath)
	assert.Equal(t, 8, crossRef.Ref)
}

func TestTypeSettings_UnmarshalJSON(t *testing.T) {
	// Test TypeSettings with cross-file configurationType
	jsonData := `{
		"name": "Foo",
		"isSingleton": true,
		"configurationType": {
			"$ref": "types.json#/0"
		}
	}`

	var settings TypeSettings
	err := json.Unmarshal([]byte(jsonData), &settings)
	require.NoError(t, err)

	assert.Equal(t, "Foo", settings.Name)
	assert.True(t, settings.IsSingleton)
	assert.NotNil(t, settings.ConfigurationType)

	crossRef, ok := settings.ConfigurationType.(types.CrossFileTypeReference)
	require.True(t, ok)
	assert.Equal(t, "types.json", crossRef.RelativePath)
	assert.Equal(t, 0, crossRef.Ref)
}

func TestBuildIndex_Basic(t *testing.T) {
	stringType := &types.StringType{}
	resourceType := &types.ResourceType{
		Name:           "Microsoft.Test/resources@2023-01-01",
		Body:           types.TypeReference{Ref: 0},
		ReadableScopes: types.AllExceptExtension,
		WritableScopes: types.AllExceptExtension,
	}
	functionType := &types.ResourceFunctionType{
		Name:         "list",
		ResourceType: "Microsoft.Test/resources",
		ApiVersion:   "2023-01-01",
		Output:       types.TypeReference{Ref: 0},
	}

	typeFile := TypeFile{
		RelativePath: "foo/types.json",
		Types: []types.Type{
			stringType,
			resourceType,
			functionType,
		},
	}

	settings := &TypeSettings{Name: "Test"}
	fallback := types.CrossFileTypeReference{RelativePath: "config/types.json", Ref: 1}

	idx := BuildIndex([]TypeFile{typeFile}, nil, settings, fallback)

	assert.Equal(t, settings, idx.Settings)
	assert.Equal(t, fallback, idx.FallbackResource)

	resMap, exists := idx.Resources["Microsoft.Test/resources"]
	require.True(t, exists)

	resRef, exists := resMap["2023-01-01"]
	require.True(t, exists)
	cross, ok := resRef.(types.CrossFileTypeReference)
	require.True(t, ok)
	assert.Equal(t, "foo/types.json", cross.RelativePath)
	assert.Equal(t, 1, cross.Ref)

	funcVersions, exists := idx.ResourceFunctions["Microsoft.Test/resources"]
	require.True(t, exists)
	funcs, exists := funcVersions["2023-01-01"]
	require.True(t, exists)
	funcRef, exists := funcs["list"]
	require.True(t, exists)
	crossFunc, ok := funcRef.(types.CrossFileTypeReference)
	require.True(t, ok)
	assert.Equal(t, "foo/types.json", crossFunc.RelativePath)
	assert.Equal(t, 2, crossFunc.Ref)
}

func TestBuildIndex_DuplicatesAndWarnings(t *testing.T) {
	warnings := []string{}
	logger := func(msg string) {
		warnings = append(warnings, msg)
	}

	resource := &types.ResourceType{
		Name:           "dup@v1",
		Body:           types.TypeReference{Ref: 0},
		ReadableScopes: types.AllExceptExtension,
		WritableScopes: types.AllExceptExtension,
	}

	typeFiles := []TypeFile{
		{
			RelativePath: "a/types.json",
			Types:        []types.Type{&types.StringType{}, resource},
		},
		{
			RelativePath: "b/types.json",
			Types:        []types.Type{&types.StringType{}, resource},
		},
	}

	idx := BuildIndex(typeFiles, logger, nil, nil)

	assert.NotEmpty(t, warnings)
	resMap, exists := idx.Resources["dup"]
	require.True(t, exists)
	assert.Len(t, resMap, 1)

	_, exists = resMap["v1"]
	assert.True(t, exists)
}

func TestSplitResourceName(t *testing.T) {
	resource, version, ok := splitResourceName("foo/bar@2020-01-01")
	assert.True(t, ok)
	assert.Equal(t, "foo/bar", resource)
	assert.Equal(t, "2020-01-01", version)

	_, _, ok = splitResourceName("noVersion")
	assert.False(t, ok)
}
