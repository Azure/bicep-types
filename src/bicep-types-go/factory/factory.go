package factory

import (
	"fmt"

	"github.com/Azure/bicep-types/src/bicep-types-go/types"
)

// TypeFactory provides methods to create and manage types with caching and indexing
type TypeFactory struct {
	typeList []types.Type
	typeMap  map[types.Type]int
}

// NewTypeFactory creates a new TypeFactory instance
func NewTypeFactory() *TypeFactory {
	return &TypeFactory{
		typeList: make([]types.Type, 0),
		typeMap:  make(map[types.Type]int),
	}
}

// GetReference returns a TypeReference for the given type, caching it if necessary
func (f *TypeFactory) GetReference(t types.Type) types.ITypeReference {
	if idx, exists := f.typeMap[t]; exists {
		return types.TypeReference{Ref: idx}
	}

	idx := len(f.typeList)
	f.typeList = append(f.typeList, t)
	f.typeMap[t] = idx

	return types.TypeReference{Ref: idx}
}

// GetTypes returns all registered types in order
func (f *TypeFactory) GetTypes() []types.Type {
	return f.typeList
}

// GetTypeByIndex returns the type at the given index
func (f *TypeFactory) GetTypeByIndex(index int) (types.Type, error) {
	if index < 0 || index >= len(f.typeList) {
		return nil, fmt.Errorf("index %d out of bounds (0-%d)", index, len(f.typeList)-1)
	}
	return f.typeList[index], nil
}

// GetTypeIndex returns the index of the given type, or -1 if not found
func (f *TypeFactory) GetTypeIndex(t types.Type) int {
	if idx, exists := f.typeMap[t]; exists {
		return idx
	}
	return -1
}

// HasType returns true if the type is already registered
func (f *TypeFactory) HasType(t types.Type) bool {
	_, exists := f.typeMap[t]
	return exists
}

// Reset clears all registered types
func (f *TypeFactory) Reset() {
	f.typeList = make([]types.Type, 0)
	f.typeMap = make(map[types.Type]int)
}

// Count returns the number of registered types
func (f *TypeFactory) Count() int {
	return len(f.typeList)
}

// Primitive type factory methods

// CreateStringType creates a new StringType
func (f *TypeFactory) CreateStringType() *types.StringType {
	return &types.StringType{}
}

// CreateStringTypeWithConstraints creates a StringType with constraints
func (f *TypeFactory) CreateStringTypeWithConstraints(minLength, maxLength *int64, pattern string, sensitive bool) *types.StringType {
	return &types.StringType{
		MinLength: minLength,
		MaxLength: maxLength,
		Pattern:   pattern,
		Sensitive: sensitive,
	}
}

// CreateStringLiteralType creates a new StringLiteralType
func (f *TypeFactory) CreateStringLiteralType(value string) *types.StringLiteralType {
	return &types.StringLiteralType{
		Value: value,
	}
}

// CreateSensitiveStringLiteralType creates a sensitive StringLiteralType
func (f *TypeFactory) CreateSensitiveStringLiteralType(value string) *types.StringLiteralType {
	return &types.StringLiteralType{
		Value:     value,
		Sensitive: true,
	}
}

// CreateIntegerType creates a new IntegerType
func (f *TypeFactory) CreateIntegerType() *types.IntegerType {
	return &types.IntegerType{}
}

// CreateIntegerTypeWithConstraints creates an IntegerType with constraints
func (f *TypeFactory) CreateIntegerTypeWithConstraints(minValue, maxValue *int64) *types.IntegerType {
	return &types.IntegerType{
		MinValue: minValue,
		MaxValue: maxValue,
	}
}

// CreateBooleanType creates a new BooleanType
func (f *TypeFactory) CreateBooleanType() *types.BooleanType {
	return &types.BooleanType{}
}

// CreateAnyType creates a new AnyType
func (f *TypeFactory) CreateAnyType() *types.AnyType {
	return &types.AnyType{}
}

// CreateNullType creates a new NullType
func (f *TypeFactory) CreateNullType() *types.NullType {
	return &types.NullType{}
}

// CreateBuiltInType creates a new BuiltInType
func (f *TypeFactory) CreateBuiltInType(kind string) *types.BuiltInType {
	return &types.BuiltInType{
		Kind: kind,
	}
}

// Complex type factory methods

// CreateArrayType creates a new ArrayType
func (f *TypeFactory) CreateArrayType(itemType types.ITypeReference) *types.ArrayType {
	return &types.ArrayType{
		ItemType: itemType,
	}
}

// CreateArrayTypeWithConstraints creates an ArrayType with constraints
func (f *TypeFactory) CreateArrayTypeWithConstraints(itemType types.ITypeReference, minLength, maxLength *int64) *types.ArrayType {
	return &types.ArrayType{
		ItemType:  itemType,
		MinLength: minLength,
		MaxLength: maxLength,
	}
}

// CreateUnionType creates a new UnionType
func (f *TypeFactory) CreateUnionType(elements []types.ITypeReference) *types.UnionType {
	return &types.UnionType{
		Elements: elements,
	}
}

// CreateObjectType creates a new ObjectType
func (f *TypeFactory) CreateObjectType(name string) *types.ObjectType {
	return &types.ObjectType{
		Name:       name,
		Properties: make(map[string]types.ObjectTypeProperty),
	}
}

// CreateObjectTypeWithProperties creates an ObjectType with properties
func (f *TypeFactory) CreateObjectTypeWithProperties(name string, properties map[string]types.ObjectTypeProperty) *types.ObjectType {
	return &types.ObjectType{
		Name:       name,
		Properties: properties,
	}
}

// CreateDiscriminatedObjectType creates a new DiscriminatedObjectType
func (f *TypeFactory) CreateDiscriminatedObjectType(name, discriminator string, elements map[string]types.ITypeReference) *types.DiscriminatedObjectType {
	return &types.DiscriminatedObjectType{
		Name:          name,
		Discriminator: discriminator,
		Elements:      elements,
	}
}

// Resource type factory methods

// CreateResourceType creates a new ResourceType
func (f *TypeFactory) CreateResourceType(name, resourceTypeID, apiVersion string, body types.ITypeReference) *types.ResourceType {
	return &types.ResourceType{
		Name:           name,
		ResourceTypeID: resourceTypeID,
		APIVersion:     apiVersion,
		Body:           body,
	}
}

// CreateResourceTypeWithDetails creates a ResourceType with all details
func (f *TypeFactory) CreateResourceTypeWithDetails(
	name, resourceTypeID, apiVersion string,
	body types.ITypeReference,
	description string,
	providers []string,
	scopeTypes []types.ScopeType,
	locationRequired, zoneRequired, isSingleton bool,
	metadata map[string]interface{},
) *types.ResourceType {
	return &types.ResourceType{
		Name:             name,
		ResourceTypeID:   resourceTypeID,
		APIVersion:       apiVersion,
		Body:             body,
		Description:      description,
		Providers:        providers,
		ScopeTypes:       scopeTypes,
		LocationRequired: locationRequired,
		ZoneRequired:     zoneRequired,
		IsSingleton:      isSingleton,
		Metadata:         metadata,
	}
}

// Function type factory methods

// CreateFunctionType creates a new FunctionType
func (f *TypeFactory) CreateFunctionType(parameters []types.FunctionParameter, returnType types.ITypeReference) *types.FunctionType {
	return &types.FunctionType{
		Parameters: parameters,
		ReturnType: returnType,
	}
}

// CreateFunctionTypeWithDetails creates a FunctionType with description and metadata
func (f *TypeFactory) CreateFunctionTypeWithDetails(
	parameters []types.FunctionParameter,
	returnType types.ITypeReference,
	description string,
	metadata map[string]interface{},
) *types.FunctionType {
	return &types.FunctionType{
		Parameters:  parameters,
		ReturnType:  returnType,
		Description: description,
		Metadata:    metadata,
	}
}

// CreateResourceFunctionType creates a new ResourceFunctionType
func (f *TypeFactory) CreateResourceFunctionType(
	name, resourceType, apiVersion string,
	returnType types.ITypeReference,
) *types.ResourceFunctionType {
	return &types.ResourceFunctionType{
		Name:         name,
		ResourceType: resourceType,
		APIVersion:   apiVersion,
		ReturnType:   returnType,
	}
}

// CreateResourceFunctionTypeWithDetails creates a ResourceFunctionType with all details
func (f *TypeFactory) CreateResourceFunctionTypeWithDetails(
	name, resourceType, apiVersion string,
	returnType types.ITypeReference,
	description string,
	parameters []types.FunctionParameter,
	metadata map[string]interface{},
) *types.ResourceFunctionType {
	return &types.ResourceFunctionType{
		Name:         name,
		ResourceType: resourceType,
		APIVersion:   apiVersion,
		ReturnType:   returnType,
		Description:  description,
		Parameters:   parameters,
		Metadata:     metadata,
	}
}

// Helper methods for creating commonly used combinations

// CreateRequiredStringProperty creates a required string property
func (f *TypeFactory) CreateRequiredStringProperty(description string) types.ObjectTypeProperty {
	stringType := f.CreateStringType()
	ref := f.GetReference(stringType)

	return types.ObjectTypeProperty{
		Type:        ref,
		Flags:       types.TypePropertyFlagsRequired,
		Description: description,
	}
}

// CreateOptionalStringProperty creates an optional string property
func (f *TypeFactory) CreateOptionalStringProperty(description string) types.ObjectTypeProperty {
	stringType := f.CreateStringType()
	ref := f.GetReference(stringType)

	return types.ObjectTypeProperty{
		Type:        ref,
		Description: description,
	}
}

// CreateReadOnlyProperty creates a read-only property
func (f *TypeFactory) CreateReadOnlyProperty(typeRef types.ITypeReference, description string) types.ObjectTypeProperty {
	return types.ObjectTypeProperty{
		Type:        typeRef,
		Flags:       types.TypePropertyFlagsReadOnly,
		Description: description,
	}
}

// CreateFunctionParameter creates a function parameter
func (f *TypeFactory) CreateFunctionParameter(name string, typeRef types.ITypeReference, description string) types.FunctionParameter {
	return types.FunctionParameter{
		Name:        name,
		Type:        typeRef,
		Description: description,
	}
}

// CreateStringArrayType creates an array of strings
func (f *TypeFactory) CreateStringArrayType() *types.ArrayType {
	stringType := f.CreateStringType()
	stringRef := f.GetReference(stringType)
	return f.CreateArrayType(stringRef)
}

// CreateStringUnionType creates a union of string literals
func (f *TypeFactory) CreateStringUnionType(values []string) *types.UnionType {
	var elements []types.ITypeReference

	for _, value := range values {
		literalType := f.CreateStringLiteralType(value)
		ref := f.GetReference(literalType)
		elements = append(elements, ref)
	}

	return f.CreateUnionType(elements)
}

// Utility methods for common patterns

// GetOrCreateType gets an existing type reference or creates and caches a new one
func (f *TypeFactory) GetOrCreateType(t types.Type) types.ITypeReference {
	return f.GetReference(t)
}

// CreateCrossFileReference creates a cross-file type reference
func (f *TypeFactory) CreateCrossFileReference(relativePath string, ref int) types.CrossFileTypeReference {
	return types.CrossFileTypeReference{
		Ref:          ref,
		RelativePath: relativePath,
	}
}
