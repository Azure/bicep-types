package integration

import (
	"bytes"
	"encoding/json"
	"os"
	"path/filepath"
	"runtime"
	"testing"

	"github.com/stretchr/testify/assert"
	"github.com/stretchr/testify/require"

	"github.com/Azure/bicep-types/src/bicep-types-go/factory"
	"github.com/Azure/bicep-types/src/bicep-types-go/index"
	"github.com/Azure/bicep-types/src/bicep-types-go/types"
	"github.com/Azure/bicep-types/src/bicep-types-go/writers"
)

func TestIntegrationFoo(t *testing.T) {
	scenario := buildFooScenario(t)
	verifyScenario(t, scenario)
}

func TestIntegrationHTTP(t *testing.T) {
	scenario := buildHTTPScenario(t)
	verifyScenario(t, scenario)
}

type baselineScenario struct {
	name      string
	typeFiles []index.TypeFile
	idx       *index.TypeIndex
}

func buildFooScenario(t *testing.T) *baselineScenario {
	t.Helper()

	fac := factory.NewTypeFactory()

	stringType := fac.CreateStringType()
	stringRef := fac.GetReference(stringType)

	defObject := fac.CreateObjectType("def", map[string]types.ObjectTypeProperty{}, nil, nil)
	defRef := fac.GetReference(defObject)

	boolType := fac.CreateBooleanType()
	boolRef := fac.GetReference(boolType)

	jklObject := fac.CreateObjectType("jkl", map[string]types.ObjectTypeProperty{}, nil, nil)
	jklRef := fac.GetReference(jklObject)

	anyType := fac.CreateAnyType()
	anyRef := fac.GetReference(anyType)

	dictObject := fac.CreateObjectType("dictType", map[string]types.ObjectTypeProperty{}, anyRef, boolPtr(true))
	dictRef := fac.GetReference(dictObject)

	minLen := int64(1)
	maxLen := int64(10)
	arrayType := fac.CreateArrayTypeWithConstraints(anyRef, &minLen, &maxLen)
	arrayRef := fac.GetReference(arrayType)

	props := map[string]types.ObjectTypeProperty{
		"abc": {
			Type:        stringRef,
			Flags:       types.TypePropertyFlagsNone,
			Description: "Abc prop",
		},
		"def": {
			Type:        defRef,
			Flags:       types.TypePropertyFlagsReadOnly,
			Description: "Def prop",
		},
		"ghi": {
			Type:        boolRef,
			Flags:       types.TypePropertyFlagsWriteOnly,
			Description: "Ghi prop",
		},
		"jkl": {
			Type:        jklRef,
			Flags:       types.TypePropertyFlagsRequired | types.TypePropertyFlagsIdentifier,
			Description: "Jkl prop",
		},
		"dictType": {
			Type:        dictRef,
			Flags:       types.TypePropertyFlagsNone,
			Description: "Dictionary of any",
		},
		"arrayType": {
			Type:        arrayRef,
			Flags:       types.TypePropertyFlagsNone,
			Description: "Array of any",
		},
	}

	fooObject := fac.CreateObjectType("foo", props, nil, nil)
	fooRef := fac.GetReference(fooObject)

	funcArgs := []types.FunctionParameter{
		fac.CreateFunctionParameter("arg", stringRef, ""),
		fac.CreateFunctionParameter("arg2", stringRef, ""),
	}

	funcType := fac.CreateFunctionType(funcArgs, boolRef)
	funcRef := fac.GetReference(funcType)

	functions := map[string]types.ResourceTypeFunction{
		"doSomething": {
			Type: funcRef,
		},
	}

	resourceType := fac.CreateResourceType("foo@v1", fooRef, types.ScopeTypeNone, types.ScopeTypeNone, functions)
	fac.GetReference(resourceType)

	configFactory := factory.NewTypeFactory()

	configProps := map[string]types.ObjectTypeProperty{
		"configProp": {
			Type:        stringRef,
			Flags:       types.TypePropertyFlagsRequired,
			Description: "Config property",
		},
	}

	configObject := configFactory.CreateObjectType("config", configProps, nil, nil)
	configRef := configFactory.GetReference(configObject).(types.TypeReference)

	fallbackBodyProps := map[string]types.ObjectTypeProperty{
		"bodyProp": {
			Type:        stringRef,
			Flags:       types.TypePropertyFlagsRequired,
			Description: "Body property",
		},
	}

	fallbackBody := configFactory.CreateObjectType("fallback body", fallbackBodyProps, nil, nil)
	fallbackBodyRef := configFactory.GetReference(fallbackBody)

	fallbackResource := configFactory.CreateResourceType("fallback", fallbackBodyRef, types.ScopeTypeNone, types.ScopeTypeNone, nil)
	fallbackRef := configFactory.GetReference(fallbackResource).(types.TypeReference)

	settings := &index.TypeSettings{
		Name:         "Foo",
		IsSingleton:  true,
		IsPreview:    true,
		IsDeprecated: false,
		Version:      "0.1.2",
		ConfigurationType: types.CrossFileTypeReference{
			RelativePath: "types.json",
			Ref:          configRef.Ref,
		},
	}

	fallbackResourceType := types.CrossFileTypeReference{RelativePath: "types.json", Ref: fallbackRef.Ref}

	typeFiles := []index.TypeFile{
		{RelativePath: "foo/types.json", Types: append([]types.Type(nil), fac.GetTypes()...)},
		{RelativePath: "types.json", Types: append([]types.Type(nil), configFactory.GetTypes()...)},
	}

	idx := index.BuildIndex(typeFiles, nil, settings, fallbackResourceType)

	return &baselineScenario{
		name:      "foo",
		typeFiles: typeFiles,
		idx:       idx,
	}
}

func buildHTTPScenario(t *testing.T) *baselineScenario {
	t.Helper()

	fac := factory.NewTypeFactory()

	rawLiteral := fac.CreateStringLiteralType("raw")
	rawRef := fac.GetReference(rawLiteral)

	jsonLiteral := fac.CreateStringLiteralType("json")
	jsonRef := fac.GetReference(jsonLiteral)

	union := fac.CreateUnionType([]types.ITypeReference{rawRef, jsonRef})
	unionRef := fac.GetReference(union)

	stringType := fac.CreateStringType()
	stringRef := fac.GetReference(stringType)

	minLen := int64(3)
	constrainedString := fac.CreateStringTypeWithConstraints(&minLen, nil, "", false)
	constrainedStringRef := fac.GetReference(constrainedString)

	minVal := int64(100)
	maxVal := int64(599)
	intType := fac.CreateIntegerTypeWithConstraints(&minVal, &maxVal)
	intRef := fac.GetReference(intType)

	anyType := fac.CreateAnyType()
	anyRef := fac.GetReference(anyType)

	props := map[string]types.ObjectTypeProperty{
		"uri": {
			Type:        stringRef,
			Flags:       types.TypePropertyFlagsRequired,
			Description: "The HTTP request URI to submit a GET request to.",
		},
		"format": {
			Type:        unionRef,
			Flags:       types.TypePropertyFlagsNone,
			Description: "How to deserialize the response body.",
		},
		"method": {
			Type:        constrainedStringRef,
			Flags:       types.TypePropertyFlagsNone,
			Description: "The HTTP method to submit request to the given URI.",
		},
		"statusCode": {
			Type:        intRef,
			Flags:       types.TypePropertyFlagsReadOnly,
			Description: "The status code of the HTTP request.",
		},
		"body": {
			Type:        anyRef,
			Flags:       types.TypePropertyFlagsReadOnly,
			Description: "The parsed request body.",
		},
	}

	requestObject := fac.CreateObjectType("request@v1", props, nil, nil)
	requestRef := fac.GetReference(requestObject)

	resource := fac.CreateResourceType("request@v1", requestRef, types.ScopeTypeNone, types.ScopeTypeNone, nil)
	fac.GetReference(resource)

	typeFiles := []index.TypeFile{
		{RelativePath: "http/v1/types.json", Types: append([]types.Type(nil), fac.GetTypes()...)},
	}

	idx := index.BuildIndex(typeFiles, nil, nil, nil)

	return &baselineScenario{
		name:      "http",
		typeFiles: typeFiles,
		idx:       idx,
	}
}

func verifyScenario(t *testing.T, scenario *baselineScenario) {
	t.Helper()

	baseDir := baselineRoot(t)

	jsonWriter := writers.NewJSONWriter()

	for _, file := range scenario.typeFiles {
		jsonOutput, err := jsonWriter.WriteTypesToString(file.Types)
		require.NoError(t, err)

		roundTrip := deserializeTypes(t, jsonOutput)
		assert.Equal(t, file.Types, roundTrip, "types JSON round-trip mismatch for %s", file.RelativePath)

		assertTypesBaseline(t, baseDir, scenario.name, file.RelativePath, file.Types)

		mdWriter := writers.NewMarkdownWriter()
		mdWriter.SetIncludeTableOfContents(false)
		var buf bytes.Buffer
		require.NoError(t, mdWriter.WriteTypes(&buf, file.Types))

		mdPath := replaceExt(file.RelativePath, ".md")
		assertMarkdownBaseline(t, baseDir, scenario.name, mdPath, buf.String())
	}

	indexJSON, err := jsonWriter.WriteTypeIndexToString(scenario.idx)
	require.NoError(t, err)
	roundTripIdx := deserializeIndex(t, indexJSON)
	assert.Equal(t, scenario.idx, roundTripIdx)
	assertIndexBaseline(t, baseDir, scenario.name, "index.json", scenario.idx)

	mdWriter := writers.NewMarkdownWriter()
	mdWriter.SetIncludeTableOfContents(false)
	var idxBuf bytes.Buffer
	require.NoError(t, mdWriter.WriteTypeIndex(&idxBuf, scenario.idx))
	assertMarkdownBaseline(t, baseDir, scenario.name, "index.md", idxBuf.String())
}

func assertTypesBaseline(t *testing.T, baseDir, scenarioName, relativePath string, actual []types.Type) {
	t.Helper()

	path := filepath.Join(baseDir, scenarioName, filepath.FromSlash(relativePath))
	baseline := deserializeTypesFromFile(t, path)
	assert.Equalf(t, baseline, actual, "baseline mismatch for %s", relativePath)
}

func assertIndexBaseline(t *testing.T, baseDir, scenarioName, relativePath string, idx *index.TypeIndex) {
	t.Helper()

	path := filepath.Join(baseDir, scenarioName, filepath.FromSlash(relativePath))
	data, err := os.ReadFile(path)
	require.NoErrorf(t, err, "failed to read baseline %s", path)

	var baseline index.TypeIndex
	require.NoErrorf(t, json.Unmarshal(data, &baseline), "failed to parse baseline index %s", path)
	assert.Equal(t, baseline, *idx)
}

func assertMarkdownBaseline(t *testing.T, baseDir, scenarioName, relativePath, actual string) {
	t.Helper()

	path := filepath.Join(baseDir, scenarioName, filepath.FromSlash(relativePath))
	expected, err := os.ReadFile(path)
	require.NoErrorf(t, err, "failed to read markdown baseline %s", path)

	assert.Equalf(t, string(expected), actual, "markdown baseline mismatch for %s", relativePath)
}

func deserializeTypes(t *testing.T, content string) []types.Type {
	t.Helper()

	var raw []json.RawMessage
	require.NoError(t, json.Unmarshal([]byte(content), &raw))

	result := make([]types.Type, len(raw))
	for i, r := range raw {
		typ, err := types.UnmarshalType(r)
		require.NoError(t, err)
		result[i] = typ
	}

	return result
}

func deserializeTypesFromFile(t *testing.T, path string) []types.Type {
	t.Helper()

	data, err := os.ReadFile(path)
	require.NoErrorf(t, err, "failed to read baseline %s", path)
	return deserializeTypes(t, string(data))
}

func deserializeIndex(t *testing.T, content string) *index.TypeIndex {
	t.Helper()

	var idx index.TypeIndex
	require.NoError(t, json.Unmarshal([]byte(content), &idx))
	return &idx
}

func replaceExt(path, ext string) string {
	return path[:len(path)-len(filepath.Ext(path))] + ext
}

func baselineRoot(t *testing.T) string {
	t.Helper()

	_, file, _, ok := runtime.Caller(0)
	require.True(t, ok)

	dir := filepath.Dir(file)
	return filepath.Clean(filepath.Join(dir, "..", "..", "..", "bicep-types", "test", "integration", "baselines"))
}

func boolPtr(v bool) *bool {
	return &v
}
