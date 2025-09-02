package factory

import (
	"testing"

	"github.com/stretchr/testify/assert"
	"github.com/stretchr/testify/require"

	"github.com/Azure/bicep-types/src/bicep-types-go/types"
)

func TestTypeFactory_Basic(t *testing.T) {
	factory := NewTypeFactory()

	// Should start empty
	assert.Equal(t, 0, factory.Count())
	assert.Empty(t, factory.GetTypes())
}

func TestTypeFactory_GetReference(t *testing.T) {
	factory := NewTypeFactory()

	stringType := factory.CreateStringType()

	// First reference should get index 0
	ref1 := factory.GetReference(stringType)
	assert.IsType(t, types.TypeReference{}, ref1)
	assert.Equal(t, 0, ref1.(types.TypeReference).Ref)
	assert.Equal(t, 1, factory.Count())

	// Second reference to same type should return same index
	ref2 := factory.GetReference(stringType)
	assert.Equal(t, ref1, ref2)
	assert.Equal(t, 1, factory.Count()) // Still only one type

	// Different type should get new index
	intType := factory.CreateIntegerType()
	ref3 := factory.GetReference(intType)
	assert.Equal(t, 1, ref3.(types.TypeReference).Ref)
	assert.Equal(t, 2, factory.Count())
}

func TestTypeFactory_GetTypeByIndex(t *testing.T) {
	factory := NewTypeFactory()

	stringType := factory.CreateStringType()
	intType := factory.CreateIntegerType()

	factory.GetReference(stringType)
	factory.GetReference(intType)

	// Should be able to retrieve by index
	retrieved0, err := factory.GetTypeByIndex(0)
	require.NoError(t, err)
	assert.Equal(t, stringType, retrieved0)

	retrieved1, err := factory.GetTypeByIndex(1)
	require.NoError(t, err)
	assert.Equal(t, intType, retrieved1)

	// Out of bounds should error
	_, err = factory.GetTypeByIndex(2)
	assert.Error(t, err)

	_, err = factory.GetTypeByIndex(-1)
	assert.Error(t, err)
}

func TestTypeFactory_HasType(t *testing.T) {
	factory := NewTypeFactory()

	stringType := factory.CreateStringType()
	intType := factory.CreateIntegerType()

	// Initially should not have any types
	assert.False(t, factory.HasType(stringType))
	assert.False(t, factory.HasType(intType))

	// After getting reference, should have the type
	factory.GetReference(stringType)
	assert.True(t, factory.HasType(stringType))
	assert.False(t, factory.HasType(intType))
}

func TestTypeFactory_Reset(t *testing.T) {
	factory := NewTypeFactory()

	stringType := factory.CreateStringType()
	factory.GetReference(stringType)

	assert.Equal(t, 1, factory.Count())
	assert.True(t, factory.HasType(stringType))

	factory.Reset()

	assert.Equal(t, 0, factory.Count())
	assert.False(t, factory.HasType(stringType))
	assert.Empty(t, factory.GetTypes())
}

func TestTypeFactory_CreatePrimitiveTypes(t *testing.T) {
	factory := NewTypeFactory()

	// Test string type creation
	stringType := factory.CreateStringType()
	assert.NotNil(t, stringType)
	assert.Equal(t, "StringType", stringType.Type())

	// Test string type with constraints
	minLen := int64(5)
	maxLen := int64(50)
	constrainedString := factory.CreateStringTypeWithConstraints(&minLen, &maxLen, "^[a-z]+$", true)
	assert.Equal(t, &minLen, constrainedString.MinLength)
	assert.Equal(t, &maxLen, constrainedString.MaxLength)
	assert.Equal(t, "^[a-z]+$", constrainedString.Pattern)
	assert.True(t, constrainedString.Sensitive)

	// Test string literal
	literal := factory.CreateStringLiteralType("test")
	assert.Equal(t, "test", literal.Value)
	assert.False(t, literal.Sensitive)

	sensitiveLiteral := factory.CreateSensitiveStringLiteralType("secret")
	assert.Equal(t, "secret", sensitiveLiteral.Value)
	assert.True(t, sensitiveLiteral.Sensitive)

	// Test integer type
	intType := factory.CreateIntegerType()
	assert.NotNil(t, intType)
	assert.Equal(t, "IntegerType", intType.Type())

	// Test integer with constraints
	minVal := int64(0)
	maxVal := int64(100)
	constrainedInt := factory.CreateIntegerTypeWithConstraints(&minVal, &maxVal)
	assert.Equal(t, &minVal, constrainedInt.MinValue)
	assert.Equal(t, &maxVal, constrainedInt.MaxValue)

	// Test boolean type
	boolType := factory.CreateBooleanType()
	assert.NotNil(t, boolType)
	assert.Equal(t, "BooleanType", boolType.Type())

	// Test any type
	anyType := factory.CreateAnyType()
	assert.NotNil(t, anyType)
	assert.Equal(t, "AnyType", anyType.Type())

	// Test null type
	nullType := factory.CreateNullType()
	assert.NotNil(t, nullType)
	assert.Equal(t, "NullType", nullType.Type())

	// Test built-in type
	builtInType := factory.CreateBuiltInType("string")
	assert.Equal(t, "string", builtInType.Kind)
	assert.Equal(t, "BuiltInType", builtInType.Type())
}

func TestTypeFactory_CreateComplexTypes(t *testing.T) {
	factory := NewTypeFactory()

	// Create item type for array
	stringType := factory.CreateStringType()
	stringRef := factory.GetReference(stringType)

	// Test array type
	arrayType := factory.CreateArrayType(stringRef)
	assert.Equal(t, stringRef, arrayType.ItemType)
	assert.Equal(t, "ArrayType", arrayType.Type())

	// Test array with constraints
	minLen := int64(1)
	maxLen := int64(10)
	constrainedArray := factory.CreateArrayTypeWithConstraints(stringRef, &minLen, &maxLen)
	assert.Equal(t, &minLen, constrainedArray.MinLength)
	assert.Equal(t, &maxLen, constrainedArray.MaxLength)

	// Test union type
	intType := factory.CreateIntegerType()
	intRef := factory.GetReference(intType)
	elements := []types.ITypeReference{stringRef, intRef}
	unionType := factory.CreateUnionType(elements)
	assert.Equal(t, elements, unionType.Elements)
	assert.Equal(t, "UnionType", unionType.Type())

	// Test object type
	objectType := factory.CreateObjectType("TestObject")
	assert.Equal(t, "TestObject", objectType.Name)
	assert.NotNil(t, objectType.Properties)
	assert.Equal(t, "ObjectType", objectType.Type())

	// Test object type with properties
	properties := map[string]types.ObjectTypeProperty{
		"name": factory.CreateRequiredStringProperty("The name"),
	}
	objectWithProps := factory.CreateObjectTypeWithProperties("TestObject", properties)
	assert.Equal(t, properties, objectWithProps.Properties)

	// Test discriminated object type
	elementsMap := map[string]types.ITypeReference{
		"option1": stringRef,
		"option2": intRef,
	}
	discriminatedType := factory.CreateDiscriminatedObjectType("TestDiscriminated", "type", elementsMap)
	assert.Equal(t, "TestDiscriminated", discriminatedType.Name)
	assert.Equal(t, "type", discriminatedType.Discriminator)
	assert.Equal(t, elementsMap, discriminatedType.Elements)
}

func TestTypeFactory_CreateResourceTypes(t *testing.T) {
	factory := NewTypeFactory()

	// Create body type
	objectType := factory.CreateObjectType("ResourceBody")
	bodyRef := factory.GetReference(objectType)

	// Test basic resource type
	resourceType := factory.CreateResourceType(
		"Test Resource",
		"Microsoft.Test/resources",
		"2023-01-01",
		bodyRef,
	)

	assert.Equal(t, "Test Resource", resourceType.Name)
	assert.Equal(t, "Microsoft.Test/resources", resourceType.ResourceTypeID)
	assert.Equal(t, "2023-01-01", resourceType.APIVersion)
	assert.Equal(t, bodyRef, resourceType.Body)
	assert.Equal(t, "ResourceType", resourceType.Type())

	// Test resource type with details
	providers := []string{"Microsoft.Test"}
	scopeTypes := []types.ScopeType{types.ScopeTypeResourceGroup}
	metadata := map[string]interface{}{"key": "value"}

	detailedResource := factory.CreateResourceTypeWithDetails(
		"Detailed Resource",
		"Microsoft.Test/detailed",
		"2023-01-01",
		bodyRef,
		"A detailed resource",
		providers,
		scopeTypes,
		true,  // locationRequired
		false, // zoneRequired
		true,  // isSingleton
		metadata,
	)

	assert.Equal(t, "A detailed resource", detailedResource.Description)
	assert.Equal(t, providers, detailedResource.Providers)
	assert.Equal(t, scopeTypes, detailedResource.ScopeTypes)
	assert.True(t, detailedResource.LocationRequired)
	assert.False(t, detailedResource.ZoneRequired)
	assert.True(t, detailedResource.IsSingleton)
	assert.Equal(t, metadata, detailedResource.Metadata)
}

func TestTypeFactory_CreateFunctionTypes(t *testing.T) {
	factory := NewTypeFactory()

	// Create parameter and return types
	stringType := factory.CreateStringType()
	stringRef := factory.GetReference(stringType)

	// Create function parameter
	param := factory.CreateFunctionParameter("name", stringRef, "The name parameter")
	assert.Equal(t, "name", param.Name)
	assert.Equal(t, stringRef, param.Type)
	assert.Equal(t, "The name parameter", param.Description)

	// Test basic function type
	parameters := []types.FunctionParameter{param}
	functionType := factory.CreateFunctionType(parameters, stringRef)

	assert.Equal(t, parameters, functionType.Parameters)
	assert.Equal(t, stringRef, functionType.ReturnType)
	assert.Equal(t, "FunctionType", functionType.Type())

	// Test function type with details
	metadata := map[string]interface{}{"key": "value"}
	detailedFunction := factory.CreateFunctionTypeWithDetails(
		parameters,
		stringRef,
		"A test function",
		metadata,
	)

	assert.Equal(t, "A test function", detailedFunction.Description)
	assert.Equal(t, metadata, detailedFunction.Metadata)

	// Test resource function type
	resourceFunction := factory.CreateResourceFunctionType(
		"listKeys",
		"Microsoft.Storage/storageAccounts",
		"2023-01-01",
		stringRef,
	)

	assert.Equal(t, "listKeys", resourceFunction.Name)
	assert.Equal(t, "Microsoft.Storage/storageAccounts", resourceFunction.ResourceType)
	assert.Equal(t, "2023-01-01", resourceFunction.APIVersion)
	assert.Equal(t, stringRef, resourceFunction.ReturnType)
	assert.Equal(t, "ResourceFunctionType", resourceFunction.Type())
}

func TestTypeFactory_HelperMethods(t *testing.T) {
	factory := NewTypeFactory()

	// Test required string property
	requiredProp := factory.CreateRequiredStringProperty("Required field")
	assert.Equal(t, types.TypePropertyFlagsRequired, requiredProp.Flags)
	assert.Equal(t, "Required field", requiredProp.Description)

	// Test optional string property
	optionalProp := factory.CreateOptionalStringProperty("Optional field")
	assert.Equal(t, types.TypePropertyFlagsNone, optionalProp.Flags)
	assert.Equal(t, "Optional field", optionalProp.Description)

	// Test read-only property
	stringType := factory.CreateStringType()
	stringRef := factory.GetReference(stringType)
	readOnlyProp := factory.CreateReadOnlyProperty(stringRef, "Read-only field")
	assert.Equal(t, types.TypePropertyFlagsReadOnly, readOnlyProp.Flags)
	assert.Equal(t, "Read-only field", readOnlyProp.Description)
	assert.Equal(t, stringRef, readOnlyProp.Type)

	// Test string array type
	stringArrayType := factory.CreateStringArrayType()
	assert.Equal(t, "ArrayType", stringArrayType.Type())

	// Test string union type
	values := []string{"option1", "option2", "option3"}
	unionType := factory.CreateStringUnionType(values)
	assert.Equal(t, "UnionType", unionType.Type())
	assert.Equal(t, len(values), len(unionType.Elements))

	// Test cross-file reference
	crossRef := factory.CreateCrossFileReference("../other/types.json", 5)
	assert.Equal(t, 5, crossRef.Ref)
	assert.Equal(t, "../other/types.json", crossRef.RelativePath)
}

func TestTypeFactory_GetOrCreateType(t *testing.T) {
	factory := NewTypeFactory()

	stringType := factory.CreateStringType()

	// First call should create
	ref1 := factory.GetOrCreateType(stringType)
	assert.Equal(t, 1, factory.Count())

	// Second call should reuse
	ref2 := factory.GetOrCreateType(stringType)
	assert.Equal(t, ref1, ref2)
	assert.Equal(t, 1, factory.Count())
}

func BenchmarkTypeFactory_GetReference(b *testing.B) {
	factory := NewTypeFactory()
	stringType := factory.CreateStringType()

	b.ResetTimer()
	for i := 0; i < b.N; i++ {
		factory.GetReference(stringType)
	}
}

func BenchmarkTypeFactory_CreateStringType(b *testing.B) {
	factory := NewTypeFactory()

	b.ResetTimer()
	for i := 0; i < b.N; i++ {
		factory.CreateStringType()
	}
}
