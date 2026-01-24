// Copyright (c) Microsoft Corporation.
// Licensed under the MIT License.
using System.Text.Json.Serialization;
using Azure.Bicep.Types.Concrete;
using Azure.Bicep.Types.Index;

namespace Azure.Bicep.Types.Serialization;

[JsonSerializable(typeof(TypeBase[]))]
[JsonSerializable(typeof(TypeIndex))]
[JsonSerializable(typeof(ArrayType))]
[JsonSerializable(typeof(BuiltInType))]
[JsonSerializable(typeof(DiscriminatedObjectType))]
[JsonSerializable(typeof(ObjectType))]
[JsonSerializable(typeof(FunctionType))]
[JsonSerializable(typeof(ResourceFunctionType))]
[JsonSerializable(typeof(NamespaceFunctionType))]
[JsonSerializable(typeof(ResourceType))]
[JsonSerializable(typeof(StringLiteralType))]
[JsonSerializable(typeof(UnionType))]
[JsonSerializable(typeof(AnyType))]
[JsonSerializable(typeof(NullType))]
[JsonSerializable(typeof(BooleanType))]
[JsonSerializable(typeof(IntegerType))]
[JsonSerializable(typeof(StringType))]
[JsonSourceGenerationOptions(DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingDefault)]
internal partial class TypeJsonContext : JsonSerializerContext
{
}
