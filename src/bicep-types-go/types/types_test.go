package types

import (
	"encoding/json"
	"testing"

	"github.com/stretchr/testify/assert"
	"github.com/stretchr/testify/require"
)

func TestTypeReference_UnmarshalJSON(t *testing.T) {
	// Test regular type reference
	data := []byte(`{"$ref": "#/5"}`)
	var ref TypeReference
	err := json.Unmarshal(data, &ref)
	require.NoError(t, err)
	assert.Equal(t, 5, ref.Ref)

	// Test direct integer (backward compatibility)
	data = []byte(`{"$ref": "10"}`)
	var ref2 TypeReference
	err = json.Unmarshal(data, &ref2)
	require.NoError(t, err)
	assert.Equal(t, 10, ref2.Ref)
}

func TestCrossFileTypeReference_UnmarshalJSON(t *testing.T) {
	// Test with explicit relativePath
	data := []byte(`{"$ref": "#/5", "relativePath": "other.json"}`)
	var ref CrossFileTypeReference
	err := json.Unmarshal(data, &ref)
	require.NoError(t, err)
	assert.Equal(t, 5, ref.Ref)
	assert.Equal(t, "other.json", ref.RelativePath)

	// Test with filename in $ref
	data = []byte(`{"$ref": "types.json#/0"}`)
	var ref2 CrossFileTypeReference
	err = json.Unmarshal(data, &ref2)
	require.NoError(t, err)
	assert.Equal(t, 0, ref2.Ref)
	assert.Equal(t, "types.json", ref2.RelativePath)
}

func TestUnmarshalTypeReference(t *testing.T) {
	// Test regular type reference
	data := []byte(`{"$ref": "#/5"}`)
	ref, err := unmarshalTypeReference(data)
	require.NoError(t, err)
	typeRef, ok := ref.(TypeReference)
	assert.True(t, ok)
	assert.Equal(t, 5, typeRef.Ref)

	// Test cross-file reference with explicit relativePath
	data = []byte(`{"$ref": "#/5", "relativePath": "other.json"}`)
	ref, err = unmarshalTypeReference(data)
	require.NoError(t, err)
	crossRef, ok := ref.(CrossFileTypeReference)
	assert.True(t, ok)
	assert.Equal(t, 5, crossRef.Ref)
	assert.Equal(t, "other.json", crossRef.RelativePath)

	// Test cross-file reference with filename in $ref
	data = []byte(`{"$ref": "types.json#/0"}`)
	ref, err = unmarshalTypeReference(data)
	require.NoError(t, err)
	crossRef2, ok := ref.(CrossFileTypeReference)
	assert.True(t, ok)
	assert.Equal(t, 0, crossRef2.Ref)
	assert.Equal(t, "types.json", crossRef2.RelativePath)
}
