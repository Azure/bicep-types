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
